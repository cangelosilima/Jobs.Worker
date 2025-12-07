using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;

namespace Jobs.Worker.Client.Net48
{
    /// <summary>
    /// Base client providing common functionality for all API clients (.NET Framework 4.8)
    /// </summary>
    public abstract class BaseClientNet48 : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private readonly ClientSettingsNet48 _settings;

        /// <summary>
        /// Gets the base URL of the API
        /// </summary>
        public string BaseUrl { get; private set; }

        /// <summary>
        /// Gets the health check URL
        /// </summary>
        public string HealthCheckUrl { get; private set; }

        /// <summary>
        /// Gets the retry count for failed requests
        /// </summary>
        public int RetryCount { get; private set; }

        /// <summary>
        /// Gets the timeout for requests
        /// </summary>
        public TimeSpan Timeout { get; private set; }

        /// <summary>
        /// Initializes a new instance of the BaseClientNet48 class
        /// </summary>
        /// <param name="httpClient">The HttpClient instance to use (optional)</param>
        /// <param name="settings">Client settings (optional, will use defaults if not provided)</param>
        protected BaseClientNet48(HttpClient httpClient = null, ClientSettingsNet48 settings = null)
        {
            _settings = settings ?? LoadDefaultSettings();

            if (httpClient == null)
            {
                _httpClient = CreateHttpClient();
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
        protected HttpClient HttpClient
        {
            get { return _httpClient; }
        }

        /// <summary>
        /// Creates an HttpClient
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// Loads default settings from embedded resource
        /// </summary>
        private static ClientSettingsNet48 LoadDefaultSettings()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jobs.Worker.Client.Net48.settings.Jobs.Worker.Client.Net48.settings.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return new ClientSettingsNet48
                    {
                        BaseUrl = "https://localhost:5001",
                        HealthCheckUrl = "https://localhost:5001/health",
                        RetryCount = 3,
                        TimeoutSeconds = 30
                    };
                }

                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<ClientSettingsNet48>(json)
                        ?? throw new InvalidOperationException("Failed to load default settings");
                }
            }
        }

        /// <summary>
        /// Executes a request with retry policy
        /// </summary>
        protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        Console.WriteLine(string.Format("Request failed. Waiting {0} before retry #{1}", timespan, retryCount));
                    });

            return await retryPolicy.ExecuteAsync(action);
        }

        /// <summary>
        /// Performs a health check against the API
        /// </summary>
        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default(CancellationToken))
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
            if (_disposeHttpClient && _httpClient != null)
            {
                _httpClient.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Client settings configuration for .NET Framework 4.8
    /// </summary>
    public class ClientSettingsNet48
    {
        /// <summary>
        /// Base URL of the API
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// Health check endpoint URL
        /// </summary>
        public string HealthCheckUrl { get; set; }

        /// <summary>
        /// Number of retry attempts for failed requests
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; }

        public ClientSettingsNet48()
        {
            BaseUrl = "https://localhost:5001";
            HealthCheckUrl = "https://localhost:5001/health";
            RetryCount = 3;
            TimeoutSeconds = 30;
        }
    }
}
