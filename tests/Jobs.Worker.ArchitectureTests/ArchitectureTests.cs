using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Jobs.Worker.ArchitectureTests;

public class ArchitectureTests
{
    private const string DomainNamespace = "Jobs.Worker.Domain";
    private const string ApplicationNamespace = "Jobs.Worker.Application";
    private const string InfrastructureNamespace = "Jobs.Worker.Infrastructure";
    private const string ApiNamespace = "Jobs.Worker.Api";

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Application()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Domain.Entities.JobDefinition).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on Application layer");
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Infrastructure()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Domain.Entities.JobDefinition).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on Infrastructure layer");
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Api()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Domain.Entities.JobDefinition).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Domain layer should not depend on API layer");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_Infrastructure()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Application.Commands.CreateJobCommand).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should not depend on Infrastructure layer");
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_Api()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Application.Commands.CreateJobCommand).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Application layer should not depend on API layer");
    }

    [Fact]
    public void Handlers_ShouldBe_InApplicationLayer()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Application.Handlers.CreateJobCommandHandler).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ResideInNamespace(ApplicationNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "All handlers should be in Application layer");
    }

    [Fact]
    public void Entities_ShouldBe_Sealed_Or_Abstract()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Domain.Entities.JobDefinition).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.Entities")
            .Should()
            .BeSealed()
            .Or()
            .BeAbstract()
            .GetResult();

        // Assert - This might fail depending on your design choice
        // Remove or adjust if entities are not sealed
        result.FailingTypes.Should().BeNullOrEmpty(
            "Entities should be sealed or abstract to prevent inheritance issues");
    }

    [Fact]
    public void Controllers_And_Endpoints_ShouldBe_InApiLayer()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Api.Hubs.JobsHub).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Hub")
            .Or()
            .HaveNameEndingWith("Controller")
            .Should()
            .ResideInNamespace(ApiNamespace)
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Controllers and Hubs should be in API layer");
    }

    [Fact]
    public void Repositories_ShouldBe_InInfrastructureLayer()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Infrastructure.Repositories.JobRepository).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespace($"{InfrastructureNamespace}.Repositories")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Repository implementations should be in Infrastructure layer");
    }

    [Fact]
    public void ValueObjects_ShouldBe_Immutable()
    {
        // Arrange
        var assembly = typeof(Jobs.Worker.Domain.ValueObjects.RetryPolicy).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.ValueObjects")
            .Should()
            .BeImmutable()
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Value objects should be immutable");
    }

    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        // Arrange
        var assemblies = new[]
        {
            typeof(Jobs.Worker.Domain.Entities.JobDefinition).Assembly,
            typeof(Jobs.Worker.Application.Interfaces.IJobRepository).Assembly
        };

        // Act & Assert
        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"All interfaces in {assembly.GetName().Name} should start with 'I'");
        }
    }
}
