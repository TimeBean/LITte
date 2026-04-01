using Litee.Engine.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPageRepository, DatabasePageRepository>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new DatabasePageRepository(connectionString!);
});

builder.Services.AddRazorPages();

var app = builder.Build();

/*if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}*/

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();