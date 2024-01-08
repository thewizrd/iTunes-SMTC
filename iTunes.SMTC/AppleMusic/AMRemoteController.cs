using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace iTunes.SMTC.AppleMusic
{
    [ApiController]
    [Route("am-remote/[action]")]
    public class AMRemoteController : ControllerBase
    {
        private readonly AppleMusicController AMController;

        public AMRemoteController(AppleMusicController musicController)
        {
            AMController = musicController;
        }

        [HttpGet]
        [ActionName("playerState")]
        public async Task<IActionResult> GetPlayerState([FromQuery] bool includeArtwork = false)
        {
            var state = await AMController.GetPlayerState(includeArtwork);

            return Ok(state);
        }

        [HttpGet]
        [ActionName("artwork")]
        public async Task<IActionResult> GetArtwork()
        {
            var artworkBytes = await AMController.GetArtwork();

            return Ok(new { artwork = artworkBytes });
        }

        [HttpPost]
        [ActionName("command")]
        public IActionResult PostAMCommand([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] string command)
        {
            if (Enum.TryParse<AppleMusicController.AppleMusicControlButtons>(command, true, out var buttonCommand))
            {
                AMController.SendMediaCommand(buttonCommand);
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [ActionName("ping")]
        public IActionResult GetPing()
        {
            return Ok();
        }
    }
}
