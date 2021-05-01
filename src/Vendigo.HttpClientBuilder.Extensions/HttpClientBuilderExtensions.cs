using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vendigo.HttpClientBuilder.CorrelationIdHandler;

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
        
    }
}
