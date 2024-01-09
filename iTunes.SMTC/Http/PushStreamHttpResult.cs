using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace iTunes.SMTC.Http
{
    public class PushStreamHttpResult : IActionResult
    {
        private readonly Func<HttpContext, Task> _onClientConnected;
        private readonly string _contentType;

        public PushStreamHttpResult(Func<HttpContext, Task> onClientConnected, string contentType)
        {
            _onClientConnected = onClientConnected;
            _contentType = contentType;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.GetTypedHeaders().ContentType = new MediaTypeHeaderValue(_contentType);
            await _onClientConnected(context.HttpContext);
        }
    }
}
