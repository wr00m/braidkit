using BraidKit.Core.Network;

namespace BraidKit.ServerApp;

public static class WebDashboard
{
    public static async Task RunWebDashboard(Server server, int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRazorPages();
        builder.Services.AddSingleton(server);

        var app = builder.Build();
        app.UseStaticFiles();
        app.MapRazorPages();

        await app.RunAsync($"http://0.0.0.0:{port}");
    }
}
