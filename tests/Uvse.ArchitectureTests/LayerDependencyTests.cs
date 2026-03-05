using NetArchTest.Rules;

namespace Uvse.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Web()
    {
        var result = Types.InAssembly(typeof(Uvse.Domain.Common.SystemRoles).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Uvse.Application", "Uvse.Infrastructure", "Uvse.Web")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Web()
    {
        var result = Types.InAssembly(typeof(Uvse.Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Uvse.Infrastructure", "Uvse.Web")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_Implementations_Should_Default_To_Internal()
    {
        var result = Types.InAssembly(typeof(Uvse.Infrastructure.DependencyInjection).Assembly)
            .That()
            .ResideInNamespace("Uvse.Infrastructure.Providers")
            .Or()
            .ResideInNamespace("Uvse.Infrastructure.Tenancy")
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
