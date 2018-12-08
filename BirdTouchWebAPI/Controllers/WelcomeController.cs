using Microsoft.AspNetCore.Mvc;

namespace BirdTouchWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WelcomeController : ControllerBase
    {
        // GET api/welcome
        [HttpGet]
        public ActionResult Get()
        {
            return Ok("BirdtouchAPI is running.");
        }
    }
}
