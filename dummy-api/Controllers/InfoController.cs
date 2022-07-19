using Microsoft.AspNetCore.Mvc;

namespace dummy_api.Controllers;

public class Info
{
    public string Version { get; set; }
}

[ApiController]
[Route("[controller]")]
public class InfoController : ControllerBase
{
    [HttpGet(Name = "GetInfo")]
    public ActionResult<string> Get()
    {
        return Ok(new Info
        {
            Version = "1.2.3.4"
        });
    }
}