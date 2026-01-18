using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using FamilyGameServer.Hubs;
using FamilyGameServer.Services;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Allow running as a Windows Service (e.g., on a dedicated LAN machine).
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapHub<GameHub>("/gameHub");

app.MapGet("/", () => Results.Redirect("/host"));
app.MapGet("/host", () => Results.Redirect("/host/"));
app.MapGet("/play", () => Results.Redirect("/play/"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();