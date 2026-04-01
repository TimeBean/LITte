using Litee.Engine.Common;
using Litee.Engine.Dto;
using Litee.Engine.Model;
using Microsoft.VisualBasic.CompilerServices;
using Npgsql;
using Dapper;

namespace Litee.Engine.Service;

using Npgsql;

public class DatabasePageRepository : IPageRepository
{
    private readonly string _connectionString;

    public DatabasePageRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Result AddPage(PageDto dto)
    {
        const string sql = @"INSERT INTO alex_schema.pages (url, content, keywords)
                             VALUES (@Url, @Content, @Keywords);";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(sql, connection);

            if (dto.Url == null)
            {
                throw  new ArgumentNullException(nameof(dto.Url));
            }
            command.Parameters.AddWithValue("Url", dto.Url);
            command.Parameters.AddWithValue("Content", (object?)dto.Content ?? DBNull.Value);
            command.Parameters.AddWithValue("Keywords", (object?)dto.Keywords ?? DBNull.Value);

            connection.Open();
            var affectedRows = command.ExecuteNonQuery();

            return affectedRows == 1
                ? Result.Ok()
                : Result.Fail("Failed to add page.");
        }
        catch (PostgresException ex)
        {
            return Result.Fail($"Database error while adding page: {ex.Message}");
        }
    }

    public Result UpdatePage(UpdatePageDto dto)
    {
        const string sql = @"UPDATE alex_schema.pages
                             SET url = @Url, content = @Content, keywords = @Keywords
                             WHERE id = @Id;";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("Id", dto.Id);
            command.Parameters.AddWithValue("Url", dto.Url);
            command.Parameters.AddWithValue("Content", (object?)dto.Content ?? DBNull.Value);
            command.Parameters.AddWithValue("Keywords", (object?)dto.Keywords ?? DBNull.Value);

            connection.Open();
            var affectedRows = command.ExecuteNonQuery();

            return affectedRows == 1
                ? Result.Ok()
                : Result.Fail("Page with specified Id not found.");
        }
        catch (PostgresException ex)
        {
            return Result.Fail($"Database error while updating page: {ex.Message}");
        }
    }

    public Result<Page> GetPageById(Guid guid)
    {
        const string sql = @"SELECT id, url, content, keywords
                            FROM alex_schema.pages
                            WHERE id = @Id;";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("Id", guid);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var page = new Page(
                    reader.GetGuid(0),
                    new Url(reader.GetString(1)), // assuming Url is a value object
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                );
                return Result<Page>.Ok(page);
            }
            
            return Result<Page>.Fail("Page with specified Id not found.");
        }
        catch (PostgresException ex)
        {
            return Result<Page>.Fail($"Database error while retrieving page: {ex.Message}");
        }
    }

    public Result RemovePageById(Guid guid)
    {
        const string sql = @"DELETE FROM alex_schema.pages 
                             WHERE id = @Id;";
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("Id", guid);

            connection.Open();
            var affectedRows = command.ExecuteNonQuery();

            return affectedRows == 1
                ? Result.Ok()
                : Result.Fail("Page with specified Id not found.");
        }
        catch (PostgresException ex)
        {
            return Result.Fail($"Database error while deleting page: {ex.Message}");
        }
    }

    public Result<Page[]> FindPages(string keywords)
    {
        // 1. Проверяем входную строку на пустоту
        if (string.IsNullOrWhiteSpace(keywords))
        {
            // Возвращаем пустой массив (синтаксис зависит от вашей реализации Result)
            return Result<Page[]>.Ok(Array.Empty<Page>()); 
        }

        // 2. Разбиваем строку на массив уникальных слов
        // "door food table" -> ["door", "food", "table"]
        var keywordsArray = keywords.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();

        // 3. Формируем SQL-запрос.
        // Функция string_to_array разбивает строку из БД в массив.
        // Оператор && проверяет пересечение массивов (есть ли хотя бы одно совпадение).
        const string sql = @"SELECT id, url, content
                            FROM alex_schema.pages
                            WHERE EXISTS (
                                SELECT 1
                                FROM unnest(@KeywordsArray) AS kw
                                WHERE pages.keywords ILIKE '%' || kw || '%'
                            );";

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
        
            // Передаем массив слов как параметр @KeywordsArray
            var pages = connection.Query(sql, new { KeywordsArray = keywordsArray })
                .Select(row => new Page(
                    id: (Guid)row.id,
                    url: new Url((string)row.url),
                    content: (string)row.content
                ))
                .ToArray();

            return Result<Page[]>.Ok(pages);
        }
        catch (Exception ex)
        {
            // Логируем ошибку при необходимости
            return Result<Page[]>.Fail($"Ошибка при поиске страниц: {ex.Message}");
        }
    }
}