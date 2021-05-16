using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace VNogin.HttpClientHandlers.Tests
{
    public class LoggingHttpHandlerTests : HttpClientHandlerBaseTests
    {
        readonly LoggingHttpHandler handler;
        readonly Mock<ILoggerFactory> logFactoryMock = new ();
        readonly Mock<ILogger> loggerMock = new();
        readonly string httpClientName = Guid.NewGuid().ToString();
        readonly string expectedLoggerName;

        readonly HttpRequestMessage requestMessage;

        readonly Mock<Func<ILogger, HttpRequestMessage, bool>> logBodyMock = new();
        readonly Mock<Func<HttpRequestMessage, HttpStatusCode?, Exception, LogLevel>> logLevelMock = new();

        public LoggingHttpHandlerTests()
        {
            expectedLoggerName = $"VNogin.HttpClientHandlers.Logging.{httpClientName}";
            logFactoryMock.Setup(f => f.CreateLogger(expectedLoggerName)).Returns(loggerMock.Object);

            handler = new LoggingHttpHandler(logFactoryMock.Object, httpClientName, new LoggingHttpHandler.Settings() {
                LogBody = logBodyMock.Object,
                LogLevel = logLevelMock.Object
            });

            requestMessage = new HttpRequestMessage();

            Init(handler);
        }

        [Theory, AutoData]
        public async Task LogLevelDisabledTest(HttpStatusCode statusCode)
        {
            // setup
            logLevelMock.SetReturnsDefault<LogLevel>(LogLevel.Information);
            loggerMock.Setup(f => f.IsEnabled(LogLevel.Information)).Returns(false);
            var expectedResponse = new HttpResponseMessage(statusCode);
            SetHandlerResponse(expectedResponse);

            // action
            var response = await httpClient.SendAsync(requestMessage);

            // asserts
            response.Should().BeSameAs(expectedResponse);

            logLevelMock.Verify(f => f(requestMessage, statusCode, null), Times.Once);
            handlerMock.Protected().Verify("SendAsync", Times.Once(), requestMessage, ItExpr.IsAny<CancellationToken>());
            logFactoryMock.Verify(f => f.CreateLogger(expectedLoggerName), Times.Once);
            loggerMock.Invocations.Where(i => i.Method.Name != "IsEnabled").Should().BeEmpty();
        }

        [Theory, AutoData]
        public async Task LogLevelEnabledTest(LogLevel logLevel, HttpStatusCode statusCode)
        {
            // setup
            logLevelMock.SetReturnsDefault<LogLevel>(logLevel);
            loggerMock.Setup(f => f.IsEnabled(logLevel)).Returns(true);
            var expectedResponse = new HttpResponseMessage(statusCode);
            SetHandlerResponse(expectedResponse);

            // action
            var response = await httpClient.SendAsync(requestMessage);

            // asserts
            response.Should().BeSameAs(expectedResponse);

            logLevelMock.Verify(f => f(requestMessage, statusCode, null), Times.Once);
            handlerMock.Protected().Verify("SendAsync", Times.Once(), requestMessage, ItExpr.IsAny<CancellationToken>());
            logFactoryMock.Verify(f => f.CreateLogger(expectedLoggerName), Times.Once);
            loggerMock.Invocations.Where(i => i.Method.Name == "Log").Should()
                .ContainSingle().Which
                .Arguments[0].As<LogLevel>().Should().Be(logLevel);            
        }

        [Theory, AutoData]
        public async Task LogExceptionTest(HttpRequestException exception, LogLevel logLevel, HttpStatusCode statusCode)
        {
            // setup
            logLevelMock.SetReturnsDefault<LogLevel>(logLevel);
            loggerMock.Setup(f => f.IsEnabled(logLevel)).Returns(true);
            var expectedResponse = new HttpResponseMessage(statusCode);

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);

            // action
            Func<Task> action = () => httpClient.SendAsync(requestMessage);

            // asserts
            (await action.Should().ThrowExactlyAsync<HttpRequestException>()).Which.Should().BeSameAs(exception);

            logLevelMock.Verify(f => f(requestMessage, null, exception), Times.Once);
            handlerMock.Protected().Verify("SendAsync", Times.Once(), requestMessage, ItExpr.IsAny<CancellationToken>());
            logFactoryMock.Verify(f => f.CreateLogger(expectedLoggerName), Times.Once);
            loggerMock.Invocations.Where(i => i.Method.Name == "Log").Should()
                .ContainSingle().Which
                .Arguments[0].As<LogLevel>().Should().Be(logLevel);
        }
    }
}
