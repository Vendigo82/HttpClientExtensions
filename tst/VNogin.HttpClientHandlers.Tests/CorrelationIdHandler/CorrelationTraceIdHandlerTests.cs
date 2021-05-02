using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VNogin.HttpClientHandlers.Handlers;
using Xunit;

namespace VNogin.HttpClientHandlers.CorrelationIdHandler.Tests
{
    public class CorrelationTraceIdHandlerTests : HttpClientHandlerBaseTests
    {
        public CorrelationTraceIdHandlerTests()
        {
        }

        [Theory, AutoData]
        public async Task AddHeaderTest(string header, string traceId)
        {
            // setup
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.SetupGet(f => f.TraceIdentifier).Returns(traceId);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.SetupGet(f => f.HttpContext).Returns(httpContextMock.Object);

            var handler = new CorrelationTraceIdHandler(header, httpContextAccessorMock.Object);
            Init(handler);

            // action
            await httpClient.SendAsync(new HttpRequestMessage());

            // asserts
            IEnumerable<string> values;
            handlerMock.Protected().Verify<Task<HttpResponseMessage>>("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Headers.TryGetValues(header, out values) && values.Count() == 1 && values.Single() == traceId),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
