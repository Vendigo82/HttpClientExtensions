﻿using Microsoft.Extensions.Logging;
using System;
using VNogin.HttpClientHandlers.Handlers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientBuilderExtensions
    {
        public const string CorrelationHeader = "X-Correlation-ID";

        /// <summary>
        /// Add retry and circut braaker policy to http client
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddConfigurationPolicies(this IHttpClientBuilder builder) => builder
            .AddPolicyHandlerFromRegistry(PollyPolicesExtensions.PolicyName.HttpRetry)
            .AddPolicyHandlerFromRegistry(PollyPolicesExtensions.PolicyName.HttpCircuitBreaker);

        /// <summary>
        /// Add HttpContext TraceId as correlation-id header to http client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="correlationHeader">name of the correlation-id header</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddTraceIdCorrelationHandler(
            this IHttpClientBuilder builder, 
            string correlationHeader = CorrelationHeader) => builder
            .AddHttpMessageHandler((sp) => ActivatorUtilities.CreateInstance<CorrelationTraceIdHandler>(sp, correlationHeader));

        /// <summary>
        /// Add logging handler to HttpClient
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="adjustSettings"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddLoggingHandler(
            this IHttpClientBuilder builder,
            Action<LoggingHttpHandler.Settings>? adjustSettings = null)
        {
            var name = builder.Name;
            var settings = new LoggingHttpHandler.Settings();
            adjustSettings?.Invoke(settings);

            if (settings.LogBody == null)
                throw new ArgumentException($"{nameof(settings.LogBody)} can't be null");
            if (settings.LogLevel == null)
                throw new ArgumentException($"{nameof(settings.LogLevel)} can't be null");

            return builder.AddHttpMessageHandler(sp => new LoggingHttpHandler(sp.GetRequiredService<ILoggerFactory>(), name, settings));
        }
        
    }
}