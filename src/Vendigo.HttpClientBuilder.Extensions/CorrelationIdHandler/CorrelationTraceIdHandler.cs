using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Vendigo.HttpClientBuilder.CorrelationIdHandler
{
    public class CorrelationTraceIdHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _correlationHeaderName;

        public CorrelationTraceIdHandler(string correlationHeaderName, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _correlationHeaderName = correlationHeaderName ?? throw new ArgumentNullException(nameof(correlationHeaderName));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var traceId = _httpContextAccessor.HttpContext.TraceIdentifier;
            request.Headers.Add(_correlationHeaderName, traceId);

            return base.SendAsync(request, cancellationToken);
        }

    }
}
