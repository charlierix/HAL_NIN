using CentralListeners.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApi.Models;

namespace CentralListeners.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VisualizeWordsController : ControllerBase
    {
        public VisualizeWordsController(IOptions<MarqueeText> marquee_options)
        {
            if (marquee_options != null)
            {
                double scale = marquee_options.Value.Scale;

                Core.WordMarquee.WordMarqueeManager.StoreSettings(new Core.WordMarquee.Settings()
                {
                    FontSize_Min = marquee_options.Value.FontSize_Min * scale,
                    FontSize_Max = marquee_options.Value.FontSize_Max * scale,

                    Blur_Min = marquee_options.Value.Blur_Min * scale,
                    Blur_Max = marquee_options.Value.Blur_Max * scale,

                    Vertical_Padding = marquee_options.Value.Vertical_Padding * scale,

                    Speed = marquee_options.Value.Speed * scale,

                    Screen_Bottom_Margin = marquee_options.Value.Screen_Bottom_Margin * scale,
                });

                foreach (var marquee in marquee_options.Value.SpeechLanes)
                    Core.WordMarquee.WordMarqueeManager.AddLane(new Core.WordMarquee.Lane()       // this function ignores dupe calls
                    {
                        Name = marquee.Name,
                        Color = marquee.Color,
                        SortOrder = marquee.SortOrder,
                    });
            }
        }

        [HttpPost]
        public ActionResult<string> Post([FromBody] Word[] words)
        {
            foreach (var word in words.OrderBy(o => o.Start))
                Core.WordMarquee.WordMarqueeManager.AddWord(new Core.WordMarquee.Word()
                {
                    LaneName = word.SpeakerName,        // if there is no match, an unmatched lane is created at the bottom with forecolor of magenta
                    Probability = word.Probability,
                    Text = word.Text,
                });

            return Ok("success");
        }
    }
}
