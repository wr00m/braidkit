using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BraidKit.Network.Pages;

public class IndexModel(Server server) : PageModel
{
    public List<PlayerSummary> Players { get; private set; } = [];
    public void OnGet() => Players = server.GetPlayers();
}