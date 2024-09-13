using iTunes.SMTC.AppleMusic.Model;
using iTunes.SMTC.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace iTunes.SMTC.AppleMusic
{
    [ApiController]
    [Route("am-remote/[action]")]
    public class AMRemoteController : ControllerBase, IDisposable
    {
        private static readonly ConcurrentDictionary<string, TextWriter> sClients = new();
        private static readonly Lazy<JsonSerializerOptions> sSerializerOptions = new(() =>
        {
            var opts = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            opts.Converters.Add(new JsonStringEnumConverter());
            return opts;
        }, true);

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

        [HttpPost]
        [ActionName("volume")]
        public IActionResult PostSetVolume([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] float volume)
        {
            if (volume >= 0 && volume <= 1)
            {
                AMController.UpdateVolume(volume);
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [ActionName("mute")]
        public IActionResult PostSetMute([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] bool mute)
        {
            AMController.Mute(isMuted: mute);
            return Ok();
        }

        [HttpPost]
        [ActionName("playbackPosition")]
        public IActionResult PostSetPlaybackPosition([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] double timeInSeconds)
        {
            if (double.IsFinite(timeInSeconds) && timeInSeconds >= 0)
            {
                AMController.UpdateAMPlayerPlaybackPosition(timeInSeconds);
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
            return Task.Factory.StartNew(() =>
            {
                var waitHndl = context.RequestAborted.WaitHandle;

                var client = TextWriter.Synchronized(new StreamWriter(context.Response.Body));
                sClients.TryAdd(context.TraceIdentifier, client);

                waitHndl.WaitOne();

                sClients.TryRemove(context.TraceIdentifier, out var _);
            }, context.RequestAborted, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void SubscribeToAMEvents()
        {
            AMController.TrackChanged += AMController_TrackChanged;
            AMController.PlayerStateChanged += AMController_PlayerStateChanged;
            AMController.ArtworkChanged += AMController_ArtworkChanged;
            AMController.VolumeStateChanged += AMController_VolumeChanged;
            AMController.PlaybackPositionChanged += AMController_PlaybackPositionChanged;
        }

        private void AMController_TrackChanged(object sender, PlayerStateModel e)
        {
            PublishEvent(new EventMessage(EventType.TrackChange, e));
        }

        private void AMController_PlayerStateChanged(object sender, PlayerStateModel e)
        {
            PublishEvent(new EventMessage(EventType.PlayerStateChanged, e));
        }

        private void AMController_ArtworkChanged(object sender, object artwork)
        {
            PublishEvent(new EventMessage(EventType.ArtworkChanged, artwork));
        }

        private void AMController_VolumeChanged(object sender, VolumeState e)
        {
            PublishEvent(new EventMessage(EventType.VolumeChanged, e));
        }

        private void AMController_PlaybackPositionChanged(object sender, TrackModel e)
        {
            PublishEvent(new EventMessage(EventType.PlaybackPositionChanged, e));
        }

        private static void PublishEvent(EventMessage @event)
        {
            Task.Factory.StartNew(async () =>
            {
                foreach (var item in sClients)
                {
                    var client = item.Value;

                    var data = JsonSerializer.Serialize(@event, sSerializerOptions.Value);

                    await client?.WriteLineAsync($"data: {data}");
                    client?.WriteLine(); // data terminator (required)
                    await client?.FlushAsync();
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Dispose()
        {
            AMController.TrackChanged -= AMController_TrackChanged;
            AMController.PlayerStateChanged -= AMController_PlayerStateChanged;
            AMController.ArtworkChanged -= AMController_ArtworkChanged;
            AMController.VolumeStateChanged -= AMController_VolumeChanged;
            AMController.PlaybackPositionChanged -= AMController_PlaybackPositionChanged;
        }
    }
}
