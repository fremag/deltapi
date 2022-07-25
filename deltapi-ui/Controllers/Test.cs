using Microsoft.AspNetCore.Mvc;

namespace deltapi_ui.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new {A=1, B="toto"});
    }
}

