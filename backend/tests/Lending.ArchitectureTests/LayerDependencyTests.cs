using Lending.Domain.Tenancy;
using NetArchTest.Rules;

namespace Lending.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly System.Reflection.Assembly _domainAssembly = typeof(Tenant).Assembly;
    private static readonly System.Reflection.Assembly _applicationAssembly =
        typeof(Lending.Application.Common.Result).Assembly;
    private static readonly System.Reflection.Assembly _infrastructureAssembly =
        typeof(Lending.Infrastructure.Persistence.LendingDbContext).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_outer_layers_or_frameworks()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Lending.Application",
                "Lending.Infrastructure",
                "Lending.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Lending.Infrastructure",
                "Lending.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var result = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("Lending.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Domain_types_are_sealed_or_abstract()
    {
        var result = Types.InAssembly(_domainAssembly)
            .That()
            .AreClasses()
            .And()
            .ArePublic()
            .Should()
            .BeSealed()
            .Or()
            .BeAbstract()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is null
            ? "Architecture rule violated."
            : "Architecture rule violated by: " + string.Join(", ", result.FailingTypeNames);
}

