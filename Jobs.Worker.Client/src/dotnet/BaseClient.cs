using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;

namespace Jobs.Worker.Client
{
    /// <summary>
    /// Base client providing common functionality for all API clients
    /// </summary>
    public abstract class BaseClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private readonly ClientSettings _settings;

        /// <summary>
        /// Gets the base URL of the API
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Gets the health check URL
        /// </summary>
        public string HealthCheckUrl { get; }

        /// <summary>
        /// Gets the retry count for failed requests
        /// </summary>
        public int RetryCount { get; }

        /// <summary>
        /// Gets the timeout for requests
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Initializes a new instance of the BaseClient class
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use (optional)</param>
        /// <param name="settings">Client settings (optional, will use defaults if not provided)</param>
        protected BaseClient(HttpClient? httpClient = null, ClientSettings? settings = null)
        {
            _settings = settings ?? LoadDefaultSettings();

            if (httpClient == null)
            {
                _httpClient = CreateHttpClientWithRetry();
                _disposeHttpClient = true;
            }
            else
            {
                _httpClient = httpClient;
                _disposeHttpClient = false;
            }

            BaseUrl = _settings.BaseUrl;
            HealthCheckUrl = _settings.HealthCheckUrl;
            RetryCount = _settings.RetryCount;
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

            _httpClient.Timeout = Timeout;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        /// <summary>
        /// Gets the configured HttpClient
        /// </summary>
        protected HttpClient HttpClient => _httpClient;

        /// <summary>
        /// Creates an HttpClient with Polly retry policy
        /// </summary>
        private HttpClient CreateHttpClientWithRetry()
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Console.WriteLine($"Request failed. Waiting {timespan} before retry #{retryCount}");
                    });

            var client = new HttpClient();
            return client;
        }

        /// <summary>
        /// Loads default settings from embedded resource
        /// </summary>
        private static ClientSettings LoadDefaultSettings()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jobs.Worker.Client.settings.Jobs.Worker.Client.settings.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return new ClientSettings
                {
                    BaseUrl = "https://localhost:5001",
                    HealthCheckUrl = "https://localhost:5001/health",
                    RetryCount = 3,
                    TimeoutSeconds = 30
                };
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<ClientSettings>(json)
                ?? throw new InvalidOperationException("Failed to load default settings");
        }

        /// <summary>
        /// Performs a health check against the API
        /// </summary>
        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(HealthCheckUrl, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disposes the client and releases resources
        /// </summary>
        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient?.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Client settings configuration
    /// </summary>
    public class ClientSettings
    {
        /// <summary>
        /// Base URL of the API
        /// </summary>
        public string BaseUrl { get; set; } = "https://localhost:5001";

        /// <summary>
        /// Health check endpoint URL
        /// </summary>
        public string HealthCheckUrl { get; set; } = "https://localhost:5001/health";

        /// <summary>
        /// Number of retry attempts for failed requests
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}
