using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Jobs.Worker.Application.Commands;
using Jobs.Worker.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Jobs.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobs.Worker.Api.Tests.Integration;

public class JobsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public JobsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<JobSchedulerDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<JobSchedulerDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetJobs_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/jobs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateJob_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreateJobCommand
        {
            Name = "Test Job",
            Description = "Test Description",
            AssemblyName = "Test.Assembly",
            ClassName = "Test.Class",
            MethodName = "Execute",
            AllowManualTrigger = true,
            Owner = new CreateJobCommand.OwnerInfo
            {
                UserId = "user1",
                UserName = "Test User",
                Email = "test@example.com"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GetJobById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/jobs/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
