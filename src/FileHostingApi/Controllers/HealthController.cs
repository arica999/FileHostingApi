using Microsoft.AspNetCore.Mvc;

namespace FileHostingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("API działa poprawnie");
    }
}
