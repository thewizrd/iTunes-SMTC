using iTunes.SMTC.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Concurrent;

namespace iTunes.SMTC.AppleMusic
{
    [ApiController]
    [Route("am-remote/[action]")]
    public class AMRemoteController : ControllerBase, IDisposable
    {
        private static readonly ConcurrentDictionary<string, StreamWriter> sClients = new();

        private readonly AppleMusicController AMController;

        public AMRemoteController(AppleMusicController musicController)
        {
            AMController = musicController;
            SubscribeToAMEvents();
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

        [HttpGet]
        [ActionName("subscribe")]
        [Produces("text/event-stream")]
        [ResponseCache(NoStore = true)]
        public IActionResult GetSubscription()
        {
            Response.StatusCode = 200;
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers.ContentType = "text/event-stream";

            return new PushStreamHttpResult(OnClientConnected, "text/event-stream");
        }

        private Task OnClientConnected(HttpContext context)
        {
            var waitHndl = context.RequestAborted.WaitHandle;

            var client = new StreamWriter(context.Response.Body);
            sClients.TryAdd(context.TraceIdentifier, client);

            waitHndl.WaitOne();

            sClients.TryRemove(context.TraceIdentifier, out var _);

            return Task.CompletedTask;
        }

        private void SubscribeToAMEvents()
        {
            AMController.TrackChanged += AMController_PlayerStateUpdated;
            AMController.PlayerStateChanged += AMController_PlayerStateUpdated;
        }

        private void AMController_PlayerStateUpdated(object sender, Model.PlayerStateModel e)
        {
            Task.Run(async () =>
            {
                foreach (var item in sClients)
                {
                    var client = item.Value;

                    await client?.WriteLineAsync($"data: {System.Text.Json.JsonSerializer.Serialize(e)}");
                    client?.WriteLine(); // data terminator (required)
                    await client?.FlushAsync();
                }
            });
        }

        public void Dispose()
        {
            AMController.TrackChanged -= AMController_PlayerStateUpdated;
            AMController.PlayerStateChanged -= AMController_PlayerStateUpdated;
        }
    }
}
