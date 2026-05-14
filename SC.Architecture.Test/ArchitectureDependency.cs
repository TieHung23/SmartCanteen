using System;
using System.Reflection;
using NetArchTest.Rules;

namespace SC.Architecture.Test;

public class ArchitectureDependency
{
    private static readonly Assembly _domainAssembly = SC.Domain.Assembly.Get;
    private static readonly Assembly _applicationAssembly = SC.Application.Assembly.Get;
    private static readonly Assembly _contractAssembly = SC.Contract.Assembly.Get;
    private static readonly Assembly _infrastructureAssembly = SC.Infrastructure.Assembly.Get;
    private static readonly Assembly _persistenceAssembly = SC.Persistence.Assembly.Get;
    private static readonly Assembly _apiAssembly = SC.Api.Assembly.Get;

    [Fact]
    public void Domain_Should_Not_Depend_On_Anything_Except_Contract()
    {
        var otherProjects = new[]
        {
            _applicationAssembly.GetName().Name!,
            _infrastructureAssembly.GetName().Name!,
            _persistenceAssembly.GetName().Name!,
            _apiAssembly.GetName().Name!
        };

        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer should not depend on other layers.");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Persistence_Or_Api()
    {
        var forbidden = new[] { _infrastructureAssembly.GetName().Name!, _persistenceAssembly.GetName().Name!, _apiAssembly.GetName().Name! };

        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, "Application layer should not depend on Infrastructure, Persistence, or Api layers.");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Application_Or_Api()
    {
        var forbidden = new[] { _applicationAssembly.GetName().Name!, _apiAssembly.GetName().Name! };

        var result = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, "Infrastructure layer should not depend on Application or Api layers.");
    }

    [Fact]
    public void Persistence_Should_Not_Depend_On_Application_Or_Api()
    {
        var forbidden = new[] { _applicationAssembly.GetName().Name!, _apiAssembly.GetName().Name! };

        var result = Types.InAssembly(_persistenceAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, "Persistence layer should not depend on Application or Api layers.");
    }

    [Fact]
    public void Contract_Should_Not_Depend_On_Other_Layers()
    {
        var forbidden = new[] { _applicationAssembly.GetName().Name!, _infrastructureAssembly.GetName().Name!, _persistenceAssembly.GetName().Name!, _apiAssembly.GetName().Name! };

        var result = Types.InAssembly(_contractAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, "Contract layer should not depend on other layers.");
    }
}

