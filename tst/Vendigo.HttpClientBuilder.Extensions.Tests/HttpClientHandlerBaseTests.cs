using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vendigo.HttpClientBuilder.Extensions
{
    public class HttpClientHandlerBaseTests
    {
        protected readonly Mock<HttpClientHandler> handlerMock = new();
        protected HttpClient httpClient;

        public void Init(DelegatingHandler handler)
        {
            handler.InnerHandler = handlerMock.Object;
            SetHandlerResponse(200, "");

            httpClient = new HttpClient(handler) {
                BaseAddress = new Uri("http://localhost")
            };            
        }

        protected void SetHandlerResponse(int code, string responseData)
        {
            var response = new HttpResponseMessage((System.Net.HttpStatusCode)code) {
                Content = new StringContent(responseData, Encoding.UTF8, "application/json")
            };
            SetHandlerResponse(response);
        }

        protected void SetHandlerResponse(HttpResponseMessage response)
        {
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(response));
        }
    }
}
