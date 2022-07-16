using Microsoft.Extensions.Logging;
using System;
using VNogin.HttpClientHandlers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Add retry and circut braaker policy to http client
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddConfigurationPolicies(this IHttpClientBuilder builder) => builder
            .AddPolicyHandlerFromRegistry(PollyPolicesExtensions.PolicyName.HttpRetry)
            .AddPolicyHandlerFromRegistry(PollyPolicesExtensions.PolicyName.HttpCircuitBreaker);

        /// <summary>
        /// Add logging handler to HttpClient
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="adjustSettings"></param>
        /// <returns></returns>
        public static IHttpClientBuilder AddLoggingHandler(this IHttpClientBuilder builder, Action<LoggingHttpHandlerSettings>? adjustSettings = null)
        {
            var name = builder.Name;

            var settings = new LoggingHttpHandlerSettings();
            adjustSettings?.Invoke(settings);

            ValidateValue(nameof(settings.LogLevelFunc), settings.LogLevelFunc);
            ValidateValue(nameof(settings.LogLevelExceptionFunc), settings.LogLevelExceptionFunc);
            ValidateValue(nameof(settings.LogReformatProvider), settings.LogReformatProvider);

            ValidateValue(nameof(settings.LogReformatProvider.RequestFunc), settings.LogReformatProvider.RequestFunc);
            ValidateValue(nameof(settings.LogReformatProvider.ResponseFunc), settings.LogReformatProvider.ResponseFunc);

            return builder.AddHttpMessageHandler(sp =>
                new LoggingHttpHandler(sp.GetRequiredService<ILoggerFactory>(), name, settings)
            );

            void ValidateValue(string paramName, object obj)
            {
                if (obj is null)
                    throw new ArgumentNullException(paramName: paramName, $"{paramName} cannot be null");
            }
        }
    }
}
