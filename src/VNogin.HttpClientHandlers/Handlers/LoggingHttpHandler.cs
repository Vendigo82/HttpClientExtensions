using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VNogin.HttpClientHandlers;

public record class LogReformatProvider
{
    public Func<HttpRequestMessage, Task<HttpRequestMessage>> RequestFunc { get; set; } = (HttpRequestMessage x) => Task.FromResult(x);
    public Func<HttpResponseMessage, Task<HttpResponseMessage>> ResponseFunc { get; set; } = (HttpResponseMessage x) => Task.FromResult(x);
}

public record class LoggingHttpHandlerSettings
{
    public Func<HttpRequestMessage, double, LogLevel> LogLevelFunc { get; set; } = (_, _) => LogLevel.Information;
    public Func<HttpRequestMessage, Exception, LogLevel> LogLevelExceptionFunc { get; set; } = (_, _) => LogLevel.Warning;
    public LogReformatProvider LogReformatProvider { get; set; } = new();
    public bool IsUseLogBody { get; set; } = true;
}

public class LoggingHttpHandler : DelegatingHandler
{
    private readonly ILogger _logger;
    private readonly Func<HttpRequestMessage, double, LogLevel> _logLevelFunc;
    private readonly Func<HttpRequestMessage, Exception, LogLevel> _logLevelExceptionFunc;
    private readonly LogReformatProvider _logReformatProvider;
    private readonly bool _isUseLogBody;

    public LoggingHttpHandler(ILoggerFactory logFactory, string name, LoggingHttpHandlerSettings settings)
    {
        _logger = logFactory.CreateLogger($"VNogin.HttpClientHandlers.Logging.{name}");
        _logLevelFunc = settings.LogLevelFunc;
        _logLevelExceptionFunc = settings.LogLevelExceptionFunc;
        _logReformatProvider = settings.LogReformatProvider;
        _isUseLogBody = settings.IsUseLogBody;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var startAt = Stopwatch.GetTimestamp();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            return await HandleResponseAsync(request, response, elapsed: GetElapsedInMilliseconds(startAt: startAt));
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(request, exception, elapsed: GetElapsedInMilliseconds(startAt: startAt));
            throw;
        }

        double GetElapsedInMilliseconds(long startAt)
        {
            var difference = (Stopwatch.GetTimestamp() - startAt) * 1000;
            return difference / (double)Stopwatch.Frequency;
        }
    }

    private async Task<HttpResponseMessage> HandleResponseAsync(HttpRequestMessage request, HttpResponseMessage response, double elapsed)
    {
        var logLevel = _logLevelFunc(request, elapsed);
        if (!_logger.IsEnabled(logLevel))
        {
            // do nothing, because log level is not enabled
            return response;
        }

        var loggerScope = null as IDisposable;
        try
        {
            var requestMessage = await _logReformatProvider.RequestFunc(request);
            var responseMessage = await _logReformatProvider.ResponseFunc(response);

            if (_isUseLogBody)
            {
                loggerScope = _logger.BeginScope(
                    new Dictionary<string, object?>
                    {
                        ["RequestMessage"] = requestMessage,
                        ["RequestBody"] = await requestMessage.Content.ReadAsStringAsync(),
                        ["ResponseMessage"] = responseMessage,
                        ["ResponseBody"] = await responseMessage.Content.ReadAsStringAsync()
                    }
                );

                _logger.Log(logLevel, "HTTP {Method} {RequestUri} responded {StatusCode} in {Elapsed} ms",
                    requestMessage.Method.Method,
                    requestMessage.RequestUri,
                    responseMessage.StatusCode,
                    elapsed);
            }
        }
        finally
        {
            loggerScope?.Dispose();
        }

        return response;
    }

    private async Task HandleExceptionAsync(HttpRequestMessage request, Exception exception, double elapsed)
    {
        var logLevel = _logLevelExceptionFunc(request, exception);
        if (!_logger.IsEnabled(logLevel))
        {
            // do nothing, because log level is not enabled
            return;
        }

        var loggerScope = null as IDisposable;
        try
        {
            var requestMessage = await _logReformatProvider.RequestFunc(request);

            if (_isUseLogBody)
            {
                loggerScope = _logger.BeginScope(
                    new Dictionary<string, object?>
                    {
                        ["RequestMessage"] = requestMessage,
                        ["RequestBody"] = await requestMessage.Content.ReadAsStringAsync()
                    }
                );
            }

            _logger.Log(logLevel, exception, "HTTP {Method} {RequestUri} failed in {Elapsed} ms",
                requestMessage.Method.Method,
                requestMessage.RequestUri,
                elapsed);
        }
        finally
        {
            loggerScope?.Dispose();
        }
    }
}
