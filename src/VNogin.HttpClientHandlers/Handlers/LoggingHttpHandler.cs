using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VNogin.HttpClientHandlers.Handlers
{
    public class LoggingHttpHandler : DelegatingHandler
    {
        /// <summary>
        /// logger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Logging settings
        /// </summary>
        private readonly Settings _settings;


        /// <summary>
        /// Construct logging deleteging handler
        /// </summary>
        /// <param name="logFactory"></param>
        /// <param name="name"></param>
        public LoggingHttpHandler(ILoggerFactory logFactory, string name, Settings settings)
        {
            _logger = logFactory.CreateLogger($"VNogin.HttpClient.LoggingHandler.{name}");
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool logBody = _settings.LogBody(_logger, request);

            string requestBody = null!;
            string responseBody = null!;
            if (logBody)
                requestBody = request.Content != null ? (await request.Content.ReadAsStringAsync(cancellationToken)) : string.Empty;

            var start = Stopwatch.GetTimestamp();
            try {
                var response = await base.SendAsync(request, cancellationToken);
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                if (logBody)
                    responseBody = response.Content != null ? (await response.Content.ReadAsStringAsync(cancellationToken)) : string.Empty;

                var logLevel = _settings.LogLevel(request, response.StatusCode, null);
                if (_logger.IsEnabled(logLevel)) {
                    var dict = new Dictionary<string, object>();
                    if (logBody) {
                        dict.Add(Pattern.RequestMessage, request);
                        dict.Add(Pattern.RequestBody, requestBody);
                        dict.Add(Pattern.ResponseMessage, response);
                        dict.Add(Pattern.ResponseBody, responseBody);
                    }

                    using var _ = _logger.BeginScope(dict);
                    _logger.Log(logLevel, Pattern.PatternDefault, request.Method.Method, request.RequestUri, response.StatusCode, elapsedMs);
                }

                return response;
            } catch (Exception e) when (LogError(e)) {
                throw;
            }

            static double GetElapsedMilliseconds(long start, long stop) => (stop - start) * 1000 / (double)Stopwatch.Frequency;

            bool LogError(Exception e)
            {
                var logLevel = _settings.LogLevel(request, null, e);
                if (_logger.IsEnabled(logLevel)) {
                    var dict = new Dictionary<string, object>();
                    if (logBody) {
                        dict.Add(Pattern.RequestMessage, request);
                        dict.Add(Pattern.RequestBody, requestBody);
                    }

                    using var _ = _logger.BeginScope(dict);
                    _logger.Log(logLevel, e, Pattern.PatternException, request.Method.Method, request.RequestUri);
                }

                return true;
            }
        }

        public class Settings
        {
            /// <summary>
            /// Get log level function
            /// </summary>
            public Func<HttpRequestMessage, HttpStatusCode?, Exception?, LogLevel> LogLevel { get; set; } = LogLevelDefault;

            /// <summary>
            /// If true, then request and response body will be logged. By default log body on level <see cref="LogLevel.Debug"/>
            /// </summary>
            public Func<ILogger, HttpRequestMessage, bool> LogBody { get; set; } = LogBodyDefault;            

        }

        public static bool LogBodyDefault(ILogger logger, HttpRequestMessage request) => logger.IsEnabled(LogLevel.Debug);

        public static LogLevel LogLevelDefault(HttpRequestMessage request, HttpStatusCode? statusCode, Exception? e)
        {
            if (e != null)
                return LogLevel.Warning;

            if (statusCode != null && (int)statusCode >= 200 && (int)statusCode < 300)
                return LogLevel.Information;

            return LogLevel.Warning;
        }

        public class Pattern
        {
            public const string Method = "HttpMethod";

            public const string Uri = "HttpRequestUri";

            public const string StatusCode = "StatusCode";

            public const string Elapsed = "Elapsed";

            public const string RequestMessage = "RequestMessage";

            public const string RequestBody = "RequestBody";

            public const string ResponseMessage = "ResponseMessage";

            public const string ResponseBody = "ResponseBody";

            public const string PatternDefault = "{" + Method + "} {" + Uri + "} responded {" + StatusCode + "} in {" + Elapsed + "} ms";

            public static readonly string RequestPart = Environment.NewLine
                + "Request message: {RequestMessage}" + Environment.NewLine
                + "Request body: {RequestBody}";

            public static readonly string PatternBody = PatternDefault + RequestPart + Environment.NewLine
                + "Response message: {ResponseMessage}" + Environment.NewLine
                + "Response body: {ResponseBody}";

            public const string PatternException = "{" + Method + "} {" + Uri + "} failed";

            public static readonly string PatternExceptionBody = PatternException + RequestPart;
        }
    }
}
