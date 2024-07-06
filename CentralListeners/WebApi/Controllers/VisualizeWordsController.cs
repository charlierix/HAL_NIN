using CentralListeners.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CentralListeners.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VisualizeWordsController : ControllerBase
    {
        [HttpPost]
        public ActionResult<string> Post([FromBody] Word[] words)
        {

            // TODO: create a singleton wpf window with transparent background
            // Use a config for size, position, colors, etc
            // As words come in, pass them to the window, which will show them for a bit and auto clear


            return Ok("success");
        }
    }
}
