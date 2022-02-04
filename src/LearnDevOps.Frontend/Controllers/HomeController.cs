using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LearnDevOps.Frontend.Models;

namespace LearnDevOps.Frontend.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IESAgent _esAgent;

    public HomeController(ILogger<HomeController> logger, IESAgent esAgent)
    {
        _logger = logger;
        _esAgent = esAgent;
        _logger.LogInformation($"{nameof(HomeController)} inited");
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Put(int id, string title, string content) {
        _esAgent.Put(new Doc(id, title, content));
        return Ok("put succeeded");
    }

    public IActionResult Query(int id) {
        var doc = _esAgent.QueryDocById(id);
        return Ok($"query succeeded, doc = {doc?.ToString() ?? "null"}");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
