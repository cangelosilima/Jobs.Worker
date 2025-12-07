# Jobs.Worker Client SDK

Official multi-platform Client SDK for Jobs.Worker API supporting .NET 8, .NET Framework 4.8, and TypeScript.

## 📦 Packages

| Platform | Package | Version | Description |
|----------|---------|---------|-------------|
| .NET 8 | `Jobs.Worker.Client` | 1.0.0 | Full SDK with REST + SignalR support |
| .NET Framework 4.8 | `Jobs.Worker.Client.Net48` | 1.0.0 | REST client only |
| TypeScript/JavaScript | `@jobs-worker/client` | 1.0.0 | REST + SignalR support |

## 🚀 Quick Start

### .NET 8

```bash
# Install the package
dotnet add package Jobs.Worker.Client

# Or via NuGet Package Manager
Install-Package Jobs.Worker.Client
```

```csharp
using Jobs.Worker.Client;

// REST API Client
var settings = new ClientSettings
{
    BaseUrl = "https://api.example.com",
    RetryCount = 3,
    TimeoutSeconds = 30
};

var jobsClient = new JobsClient(settings: settings);
var jobs = await jobsClient.GetAllJobsAsync();

// SignalR Hub Client
var hubClient = new JobsHubClient(settings);
hubClient.SubscribeToEvents();

hubClient.OnJobStarted += (update) =>
{
    Console.WriteLine($"Job {update.JobName} started");
};

await hubClient.StartAsync();
```

### .NET Framework 4.8

```bash
# Install the package
Install-Package Jobs.Worker.Client.Net48
```

```csharp
using Jobs.Worker.Client.Net48;

var settings = new ClientSettingsNet48
{
    BaseUrl = "https://api.example.com",
    RetryCount = 3,
    TimeoutSeconds = 30
};

var jobsClient = new JobsClient(settings: settings);
var jobs = await jobsClient.GetAllJobsAsync();
```

### TypeScript

```bash
# Install via npm
npm install @jobs-worker/client

# Or via yarn
yarn add @jobs-worker/client
```

```typescript
import { JobsWorkerApiClient, JobsHubClient, ClientSettings } from '@jobs-worker/client';

// REST API Client
const settings: Partial<ClientSettings> = {
    baseUrl: 'https://api.example.com',
    retryCount: 3,
    timeoutSeconds: 30
};

const apiClient = new JobsWorkerApiClient(settings);
const jobs = await apiClient.getAllJobs();

// SignalR Hub Client
const hubClient = new JobsHubClient(settings);
hubClient.subscribeToEvents();

hubClient.onJobStarted = (update) => {
    console.log(`Job ${update.jobName} started`);
};

await hubClient.start();
```

## 🏗️ Building from Source

### Prerequisites

- .NET 8 SDK
- NSwag CLI (`dotnet tool install -g NSwag.ConsoleCore`)
- Node.js 18+ (for TypeScript client)
- Jobs.Worker API running (for code generation)

### Generate All Clients

```bash
# Navigate to the client SDK directory
cd Jobs.Worker.Client

# Ensure the API is running on https://localhost:5001
# Then generate all clients:

# 1. Generate .NET 8 REST client
nswag run nswag-dotnet.json

# 2. Generate .NET Framework 4.8 REST client
nswag run nswag-net48.json

# 3. Generate TypeScript REST client
nswag run nswag-typescript.json

# Note: SignalR clients are pre-generated (SignalRClientBase.cs and signalr-client.ts)
```

### Build .NET 8 Package

```bash
cd Jobs.Worker.Client
dotnet restore
dotnet build -c Release
dotnet pack -c Release -o ./nupkg
```

### Build .NET Framework 4.8 Package

```bash
cd Jobs.Worker.Client/Jobs.Worker.Client.Net48
dotnet restore
dotnet build -c Release
dotnet pack -c Release -o ../nupkg
```

### Build TypeScript Package

```bash
cd Jobs.Worker.Client
npm install
npm run build
npm pack
```

## 📋 API Endpoints

The SDK provides strongly-typed clients for all API endpoints:

### Jobs API
- `GET /api/jobs` - Get all jobs
- `GET /api/jobs/{id}` - Get job by ID
- `POST /api/jobs` - Create a new job
- `PUT /api/jobs/{id}` - Update a job
- `DELETE /api/jobs/{id}` - Delete a job
- `POST /api/jobs/{id}/activate` - Activate a job
- `POST /api/jobs/{id}/disable` - Disable a job
- `POST /api/jobs/{id}/archive` - Archive a job
- `POST /api/jobs/{id}/trigger` - Trigger a job execution
- `GET /api/jobs/{jobId}/schedules` - Get job schedules
- `GET /api/jobs/{jobId}/executions` - Get job executions

### Executions API
- `GET /api/executions` - Get all executions (with filtering)
- `GET /api/executions/{id}` - Get execution by ID
- `GET /api/executions/running` - Get running executions
- `POST /api/executions/{id}/cancel` - Cancel an execution
- `GET /api/executions/{id}/logs` - Get execution logs

### Schedules API
- `POST /api/schedules/jobs/{jobId}/schedules` - Create a schedule

### Dashboard API
- `GET /api/dashboard/stats` - Get dashboard statistics
- `GET /api/dashboard/execution-trends` - Get execution trends
- `GET /api/dashboard/top-failing-jobs` - Get top failing jobs
- `GET /api/dashboard/stale-jobs` - Get stale jobs
- `GET /api/dashboard/upcoming-schedules` - Get upcoming schedules

### Audit API
- `GET /api/audit` - Get audit logs (with filtering)
- `GET /api/audit/job/{jobId}` - Get audit logs for a job

### Circuit Breakers API
- `GET /api/circuit-breakers` - Get all circuit breakers
- `GET /api/circuit-breakers/job/{jobId}` - Get circuit breaker by job ID
- `GET /api/circuit-breakers/open` - Get open circuit breakers
- `POST /api/circuit-breakers/{jobId}/close` - Close a circuit breaker
- `POST /api/circuit-breakers/{jobId}/open` - Open a circuit breaker

### Health API
- `GET /health` - General health check
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /health/jobs` - Jobs health check

## 🔌 SignalR Hub Events

### Server-to-Client Events

The hub broadcasts the following events:

- **JobExecutionUpdated** - Job execution state changed
- **JobStarted** - Job execution started
- **JobCompleted** - Job execution completed successfully
- **JobFailed** - Job execution failed
- **MetricsUpdated** - System metrics updated
- **AuditLogAdded** - New audit log entry created
- **NotificationReceived** - New notification received

### Client-to-Server Methods

Clients can invoke these methods on the hub:

- **SendJobExecutionUpdate** - Send job execution update
- **SendJobStarted** - Send job started notification
- **SendJobCompleted** - Send job completed notification
- **SendJobFailed** - Send job failed notification
- **SendMetricsUpdate** - Send metrics update
- **SendAuditLogEntry** - Send audit log entry
- **SendNotification** - Send notification

## ⚙️ Configuration

### Default Settings

All clients load default settings from embedded resources:

```json
{
  "baseUrl": "https://localhost:5001",
  "healthCheckUrl": "https://localhost:5001/health",
  "retryCount": 3,
  "timeoutSeconds": 30
}
```

### Override Settings

#### .NET 8

```csharp
var settings = new ClientSettings
{
    BaseUrl = "https://production.example.com",
    HealthCheckUrl = "https://production.example.com/health",
    RetryCount = 5,
    TimeoutSeconds = 60
};

// Pass to client constructor
var client = new JobsClient(settings: settings);
```

#### .NET Framework 4.8

```csharp
var settings = new ClientSettingsNet48
{
    BaseUrl = "https://production.example.com",
    HealthCheckUrl = "https://production.example.com/health",
    RetryCount = 5,
    TimeoutSeconds = 60
};

var client = new JobsClient(settings: settings);
```

#### TypeScript

```typescript
const settings: Partial<ClientSettings> = {
    baseUrl: 'https://production.example.com',
    healthCheckUrl: 'https://production.example.com/health',
    retryCount: 5,
    timeoutSeconds: 60
};

const client = new JobsWorkerApiClient(settings);
```

### Using Custom HttpClient (.NET)

```csharp
// .NET 8
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");

var client = new JobsClient(httpClient: httpClient);

// .NET Framework 4.8
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");

var client = new JobsClient(httpClient: httpClient);
```

## 🔄 Retry Policy

All clients implement automatic retry with exponential backoff:

- **Default retries**: 3 attempts
- **Backoff strategy**: Exponential (2^attempt seconds)
- **Retry delays**: 2s, 4s, 8s
- **Handles**: Transient HTTP errors (408, 5xx)

## 💉 Dependency Injection

### .NET 8 with ASP.NET Core

```csharp
// Startup.cs or Program.cs
services.AddHttpClient<IJobsClient, JobsClient>((serviceProvider, client) =>
{
    var settings = new ClientSettings
    {
        BaseUrl = Configuration["JobsWorkerApi:BaseUrl"],
        RetryCount = 3,
        TimeoutSeconds = 30
    };
    return new JobsClient(client, settings);
});

services.AddSingleton<JobsHubClient>(sp =>
{
    var settings = new ClientSettings
    {
        BaseUrl = Configuration["JobsWorkerApi:BaseUrl"]
    };
    return new JobsHubClient(settings);
});
```

### Usage in Controller

```csharp
public class JobsController : ControllerBase
{
    private readonly IJobsClient _jobsClient;

    public JobsController(IJobsClient jobsClient)
    {
        _jobsClient = jobsClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var jobs = await _jobsClient.GetAllJobsAsync();
        return Ok(jobs);
    }
}
```

## 🧪 Testing

### Health Check

```csharp
// .NET
var client = new JobsClient();
bool isHealthy = await client.HealthCheckAsync();

// TypeScript
const client = new JobsWorkerApiClient();
const isHealthy = await client.healthCheck();
```

## 📚 Advanced Usage

### Working with Executions

```csharp
// .NET 8
var executionsClient = new ExecutionsClient();

// Get all running executions
var running = await executionsClient.GetRunningExecutionsAsync();

// Get executions with filtering
var executions = await executionsClient.GetAllExecutionsAsync(
    jobId: Guid.Parse("job-guid"),
    status: ExecutionStatus.Failed,
    pageNumber: 1,
    pageSize: 25
);

// Cancel an execution
await executionsClient.CancelExecutionAsync(
    id: executionId,
    new CancelExecutionCommand(executionId.ToString(), "admin", "Cancelled by user")
);
```

### Working with Dashboard

```csharp
// .NET 8
var dashboardClient = new DashboardClient();

// Get dashboard stats
var stats = await dashboardClient.GetDashboardStatsAsync();
Console.WriteLine($"Active Jobs: {stats.ActiveJobs}");
Console.WriteLine($"Running Executions: {stats.RunningExecutions}");

// Get execution trends
var trends = await dashboardClient.GetExecutionTrendsAsync(days: 7);

// Get top failing jobs
var failingJobs = await dashboardClient.GetTopFailingJobsAsync(limit: 10);
```

### SignalR Real-time Updates

```csharp
// .NET 8
var hubClient = new JobsHubClient();
hubClient.SubscribeToEvents();

// Handle job events
hubClient.OnJobStarted += (update) =>
{
    Console.WriteLine($"Job {update.JobName} started at {update.StartTime}");
};

hubClient.OnJobCompleted += (update) =>
{
    Console.WriteLine($"Job {update.JobName} completed at {update.EndTime}");
};

hubClient.OnJobFailed += (update) =>
{
    Console.WriteLine($"Job {update.JobName} failed: {update.ErrorMessage}");
};

hubClient.OnMetricsUpdated += (metrics) =>
{
    Console.WriteLine($"Success Rate: {metrics.SuccessRatePercentage:F2}%");
};

// Connection lifecycle events
hubClient.Reconnecting += (ex) =>
{
    Console.WriteLine("Connection lost, reconnecting...");
    return Task.CompletedTask;
};

hubClient.Reconnected += (connectionId) =>
{
    Console.WriteLine($"Reconnected with ID: {connectionId}");
    return Task.CompletedTask;
};

await hubClient.StartAsync();

// Keep the connection alive
Console.WriteLine("Press any key to disconnect...");
Console.ReadKey();

await hubClient.StopAsync();
await hubClient.DisposeAsync();
```

### TypeScript SignalR Usage

```typescript
import { JobsHubClient } from '@jobs-worker/client';

const hubClient = new JobsHubClient({
    baseUrl: 'https://api.example.com'
});

hubClient.subscribeToEvents();

// Handle events
hubClient.onJobStarted = (update) => {
    console.log(`Job ${update.jobName} started`);
};

hubClient.onJobCompleted = (update) => {
    console.log(`Job ${update.jobName} completed`);
};

hubClient.onMetricsUpdated = (metrics) => {
    console.log(`Success Rate: ${metrics.successRatePercentage.toFixed(2)}%`);
};

// Connection lifecycle
hubClient.onReconnecting = (error) => {
    console.log('Reconnecting...', error);
};

hubClient.onReconnected = (connectionId) => {
    console.log('Reconnected:', connectionId);
};

// Start the connection
await hubClient.start();

// Later: disconnect
await hubClient.stop();
```

## 🚢 Publishing Packages

### Publishing to NuGet

```bash
# Build and pack .NET 8
cd Jobs.Worker.Client
dotnet pack -c Release

# Build and pack .NET Framework 4.8
cd Jobs.Worker.Client.Net48
dotnet pack -c Release

# Push to NuGet
dotnet nuget push ./nupkg/Jobs.Worker.Client.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push ./nupkg/Jobs.Worker.Client.Net48.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Publishing to npm

```bash
cd Jobs.Worker.Client
npm run build

# Login to npm (first time only)
npm login

# Publish
npm publish --access public
```

## 🔄 Versioning Strategy

This SDK follows **Semantic Versioning (SemVer)**:

- **MAJOR** version: Breaking API changes
- **MINOR** version: New features (backwards compatible)
- **PATCH** version: Bug fixes (backwards compatible)

### Version Compatibility

| SDK Version | API Version | .NET 8 | .NET Framework 4.8 | TypeScript |
|-------------|-------------|--------|-------------------|------------|
| 1.0.x       | 1.0.x       | ✅     | ✅                | ✅         |
| 1.1.x       | 1.1.x       | ✅     | ✅                | ✅         |

## 🔧 CI/CD Integration

### GitHub Actions

```yaml
name: Build and Publish SDK

on:
  push:
    tags:
      - 'v*'

jobs:
  build-dotnet:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Install NSwag
        run: dotnet tool install -g NSwag.ConsoleCore

      - name: Generate .NET 8 Client
        run: |
          cd Jobs.Worker.Client
          nswag run nswag-dotnet.json

      - name: Build .NET 8
        run: |
          cd Jobs.Worker.Client
          dotnet build -c Release
          dotnet pack -c Release

      - name: Generate .NET Framework 4.8 Client
        run: |
          cd Jobs.Worker.Client
          nswag run nswag-net48.json

      - name: Build .NET Framework 4.8
        run: |
          cd Jobs.Worker.Client/Jobs.Worker.Client.Net48
          dotnet build -c Release
          dotnet pack -c Release

      - name: Publish to NuGet
        run: |
          dotnet nuget push Jobs.Worker.Client/nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
          dotnet nuget push Jobs.Worker.Client/nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json

  build-typescript:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          registry-url: 'https://registry.npmjs.org'

      - name: Install dependencies
        run: |
          cd Jobs.Worker.Client
          npm install

      - name: Generate TypeScript Client
        run: |
          cd Jobs.Worker.Client
          npm run generate

      - name: Build
        run: |
          cd Jobs.Worker.Client
          npm run build

      - name: Publish to npm
        run: |
          cd Jobs.Worker.Client
          npm publish --access public
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

## 🏛️ Clean Architecture Integration

This SDK is designed to integrate seamlessly with Clean Architecture:

### Application Layer

```csharp
// Application/Interfaces/IJobsWorkerClient.cs
public interface IJobsWorkerClient
{
    Task<IEnumerable<Job>> GetAllJobsAsync();
    Task<Job> GetJobByIdAsync(Guid id);
    Task<Guid> CreateJobAsync(CreateJobCommand command);
}

// Application/Services/JobsWorkerClientAdapter.cs
public class JobsWorkerClientAdapter : IJobsWorkerClient
{
    private readonly IJobsClient _client;

    public JobsWorkerClientAdapter(IJobsClient client)
    {
        _client = client;
    }

    public async Task<IEnumerable<Job>> GetAllJobsAsync()
    {
        var jobs = await _client.GetAllJobsAsync();
        return jobs.Select(MapToDomainModel);
    }

    // ... mapping logic
}
```

### Infrastructure Layer

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddJobsWorkerClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new ClientSettings
        {
            BaseUrl = configuration["JobsWorkerApi:BaseUrl"],
            RetryCount = configuration.GetValue<int>("JobsWorkerApi:RetryCount", 3),
            TimeoutSeconds = configuration.GetValue<int>("JobsWorkerApi:TimeoutSeconds", 30)
        };

        services.AddHttpClient<IJobsClient, JobsClient>((sp, client) =>
            new JobsClient(client, settings));

        services.AddScoped<IJobsWorkerClient, JobsWorkerClientAdapter>();

        return services;
    }
}
```

## 🔐 Security Considerations

### Authentication

```csharp
// Add authentication header
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", "YOUR_JWT_TOKEN");

var client = new JobsClient(httpClient);
```

### HTTPS Only

Always use HTTPS in production:

```csharp
var settings = new ClientSettings
{
    BaseUrl = "https://api.example.com"  // Always HTTPS
};
```

## 📖 Additional Resources

- [Jobs.Worker API Documentation](https://github.com/cangelosilima/Jobs.Worker)
- [NSwag Documentation](https://github.com/RicoSuter/NSwag)
- [SignalR Client Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/dotnet-client)
- [TypeScript SignalR Client](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client)

## 📝 License

This SDK is licensed under the MIT License.

## 🤝 Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## 📞 Support

For issues and questions:
- GitHub Issues: [https://github.com/cangelosilima/Jobs.Worker/issues](https://github.com/cangelosilima/Jobs.Worker/issues)
- Email: support@example.com

---

**Built with ❤️ by the Jobs.Worker Team**
