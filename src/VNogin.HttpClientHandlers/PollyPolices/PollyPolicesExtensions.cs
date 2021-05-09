using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Extensions.Http;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PollyPolicesExtensions
    {
        public const string PoliciesConfigurationSectionName = "HttpClientPolicies";

        /// <summary>
        /// Register Http polly policies from configuration from section with name 'HttpClientPolicies' 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddConfigurationHttpClientPolicies(
            this IServiceCollection services,
            IConfiguration configuration)
            => AddConfigurationHttpClientPolicies(services, configuration, PoliciesConfigurationSectionName);

        /// <summary>
        /// Register Http polly policies from configuration
        /// </summary>
        /// <see cref="https://rehansaeed.com/optimally-configuring-asp-net-core-httpclientfactory/"/>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="configurationSectionName"></param>
        /// <returns></returns>
        public static IServiceCollection AddConfigurationHttpClientPolicies(
            this IServiceCollection services,
            IConfiguration configuration,
            string configurationSectionName)
        {
            var section = configuration.GetSection(configurationSectionName);
            services.Configure<PolicyOptions>(configuration);
            var policyOptions = configuration.Get<PolicyOptions>() ?? new PolicyOptions();

            var policyRegistry = services.AddPolicyRegistry();

            policyRegistry.Add(
                PolicyName.HttpRetry,
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        policyOptions.HttpRetry.Count,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(policyOptions.HttpRetry.BackoffPower, retryAttempt))));

            policyRegistry.Add(
                PolicyName.HttpCircuitBreaker,
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: policyOptions.HttpCircuitBreaker.ExceptionsAllowedBeforeBreaking,
                        durationOfBreak: policyOptions.HttpCircuitBreaker.DurationOfBreak));

            return services;
        }

        public static class PolicyName
        {
            public const string HttpCircuitBreaker = nameof(HttpCircuitBreaker);
            public const string HttpRetry = nameof(HttpRetry);
        }

        private class PolicyOptions
        {
            public CircuitBreakerPolicyOptions HttpCircuitBreaker { get; set; } = new CircuitBreakerPolicyOptions();
            public RetryPolicyOptions HttpRetry { get; set; } = new RetryPolicyOptions();
        }

        private class CircuitBreakerPolicyOptions
        {
            public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
            public int ExceptionsAllowedBeforeBreaking { get; set; } = 12;
        }

        private class RetryPolicyOptions
        {
            public int Count { get; set; } = 3;
            public int BackoffPower { get; set; } = 2;
        }
    }
}
