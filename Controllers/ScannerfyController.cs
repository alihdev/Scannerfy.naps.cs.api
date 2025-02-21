using Microsoft.AspNetCore.Mvc;

namespace Scannerfy.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ScannerfyController : ControllerBase
{
    private readonly ILogger<ScannerfyController> _logger;

    public ScannerfyController(ILogger<ScannerfyController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "GetScanners")]
    public object Get()
    {
        return "Hello world";
    }
}
