using Microsoft.Extensions.DependencyInjection;
using Polly.Extensions.Http;
using Polly;
using System.Net.Sockets;

namespace BMS.Utilities.HTTPClientHelper
{
    public static class Extension
    {
        private static readonly Random _jitterer = new Random();
        public static IHttpClientBuilder AddRobustHttpClient<TClient, TImplementation>(
            this IServiceCollection services, int retryCount = 5, 
            int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30) 
            where TClient : class where TImplementation : class, TClient
        {
            return services.AddHttpClient<TClient, TImplementation>()
                .AddPolicyHandler(GetRetryPolicy(retryCount))
                .AddPolicyHandler(GetCircuitBreakerPolicy(handledEventsAllowedBeforeBreaking, durationOfBreakInSeconds));
        }

        public static IHttpClientBuilder AddRobustHttpClient<TClient>(
            this IServiceCollection services, int retryCount = 5,
            int handledEventsAllowedBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
            where TClient : class
        {
            return services.AddHttpClient<TClient>()
                .AddPolicyHandler(GetRetryPolicy(retryCount))
                .AddPolicyHandler(GetCircuitBreakerPolicy(handledEventsAllowedBeforeBreaking, durationOfBreakInSeconds));
        }

        private static  IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(retryCount, // exponential back-off plus some jitter
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                    + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100)));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
            int handledEventsAllowedBeforeBreaking, int durationOfBreakInSeconds)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking, TimeSpan.FromSeconds(durationOfBreakInSeconds));
        }
    }
}