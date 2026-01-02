using BraidKit.Core.Network;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BraidKit.ServerApp.Pages;

public class IndexModel(Server server) : PageModel
{
    public List<PlayerSummary> Players { get; private set; } = [];
    public int Port { get; private set; }

    public void OnGet()
    {
        Players = server.GetPlayers();
        Port = server.Port;
    }
}