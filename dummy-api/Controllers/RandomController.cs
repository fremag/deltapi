using deltapi_utils;
using dummy_api.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace dummy_api.Controllers;

[ApiController]
[Route("[controller]")]
public class RandomController : ControllerBase
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public RandomService RandomService { get; }

    public RandomController(RandomService randomService)
    {
        RandomService = randomService;
    }

    [HttpGet]
    public ActionResult<string> Get()
    {
        Logger.ExtInfo();
        var value = new { Value = RandomService.Next(100) };
        Logger.ExtInfo(value);

        return Ok(value);
    }
    
    [HttpPost("{seed:int}")]
    public ActionResult Post(int seed)
    {
        Logger.ExtInfo(new {seed});
        RandomService.Reset(seed);
        return Ok();
    }
    
    [HttpDelete]
    public ActionResult Delete()
    {
        var newSeed = (int)DateTime.Now.Ticks; 
        Logger.ExtInfo(new {newSeed});
        RandomService.Reset(newSeed);
        return Ok(new {newSeed});
    }
}