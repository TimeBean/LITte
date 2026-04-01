namespace Litee.Engine.Common;

public class Result
{
    public bool Success { get; }
    public string? Error { get; }

    private Result(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}