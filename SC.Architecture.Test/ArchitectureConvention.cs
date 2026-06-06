using System.Reflection;
using NetArchTest.Rules;
using SC.Contract.Abstraction.Message;
using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;

namespace SC.Architecture.Test;

/// <summary>
/// Enforces naming conventions and interface-implementation contracts
/// that must hold across every layer of the SmartCanteen solution.
/// </summary>
public class ArchitectureConvention
{
    // ── Assemblies ────────────────────────────────────────────────────────────
    private static readonly Assembly _applicationAssembly = SC.Application.Assembly.Get;
    private static readonly Assembly _domainAssembly = SC.Domain.Assembly.Get;
    private static readonly Assembly _contractAssembly = SC.Contract.Assembly.Get;
    private static readonly Assembly _infrastructureAssembly = SC.Infrastructure.Assembly.Get;
    private static readonly Assembly _persistenceAssembly = SC.Persistence.Assembly.Get;
    private static readonly Assembly _apiAssembly = SC.Api.Assembly.Get;

    // =========================================================================
    //  MediatR – Query conventions
    // =========================================================================

    [Fact]
    public void Classes_EndingWith_Query_Should_Implement_IQuery()
    {
        // Every type whose name ends in "Query" must implement IQuery or IQuery<TResponse>.
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .ImplementInterface(typeof(IQuery<>))
            .Or()
            .ImplementInterface(typeof(IQuery))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All types ending with 'Query' must implement IQuery or IQuery<TResponse>.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void IQuery_Implementations_Should_Be_Named_With_Query_Suffix()
    {
        // The reverse: every IQuery implementation must have the "Query" suffix.
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Or()
            .ImplementInterface(typeof(IQuery))
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All IQuery / IQuery<T> implementations must be named with a 'Query' suffix.\n" +
            FormatFailures(result));
    }

    // =========================================================================
    //  MediatR – QueryHandler conventions
    // =========================================================================

    [Fact]
    public void Classes_EndingWith_QueryHandler_Should_Implement_IQueryHandler()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Or()
            .ImplementInterface(typeof(IQueryHandler<>))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All types ending with 'QueryHandler' must implement IQueryHandler<TQuery,TResponse> or IQueryHandler<TQuery>.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void IQueryHandler_Implementations_Should_Be_Named_With_QueryHandler_Suffix()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Or()
            .ImplementInterface(typeof(IQueryHandler<>))
            .Should()
            .HaveNameEndingWith("QueryHandler")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All IQueryHandler implementations must be named with a 'QueryHandler' suffix.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void QueryHandlers_Should_Use_Generic_Result_Response()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All QueryHandlers must implement IQueryHandler<TQuery,TResponse> to enforce Result<T> responses.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void QueryHandlers_Should_Not_Be_Public()
    {
        // Handlers should be internal: consumers must go through MediatR, not resolve handlers directly.
        var offenders = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .GetTypes()
            .Where(t => t.IsPublic)
            .ToList();

        Assert.Empty(offenders);
    }

    // =========================================================================
    //  MediatR – Command conventions
    // =========================================================================

    [Fact]
    public void Classes_EndingWith_Command_Should_Implement_ICommand()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All types ending with 'Command' must implement ICommand or ICommand<TResponse>.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void ICommand_Implementations_Should_Be_Named_With_Command_Suffix()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All ICommand / ICommand<T> implementations must be named with a 'Command' suffix.\n" +
            FormatFailures(result));
    }

    // =========================================================================
    //  MediatR – CommandHandler conventions
    // =========================================================================

    [Fact]
    public void Classes_EndingWith_CommandHandler_Should_Implement_ICommandHandler()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All types ending with 'CommandHandler' must implement ICommandHandler<TCommand> or ICommandHandler<TCommand,TResponse>.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void ICommandHandler_Implementations_Should_Be_Named_With_CommandHandler_Suffix()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("CommandHandler")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All ICommandHandler implementations must be named with a 'CommandHandler' suffix.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void CommandHandlers_Should_Use_Generic_Result_Response()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All CommandHandlers must implement ICommandHandler<TCommand,TResponse> to enforce Result<T> responses.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void CommandHandlers_Should_Not_Be_Public()
    {
        // Same visibility rule as QueryHandlers.
        var offenders = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .GetTypes()
            .Where(t => t.IsPublic)
            .ToList();

        Assert.Empty(offenders);
    }

    // =========================================================================
    //  MediatR – DomainEvent conventions
    // =========================================================================

    [Fact]
    public void Classes_EndingWith_DomainEvent_Should_Implement_IDomainEvent()
    {
        // Domain events live in SC.Domain.
        var result = Types.InAssembly(_domainAssembly)
            .That()
            .HaveNameEndingWith("DomainEvent")
            .Should()
            .ImplementInterface(typeof(IDomainEvent))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All types ending with 'DomainEvent' must implement IDomainEvent.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void IDomainEvent_Implementations_Should_Be_Named_With_DomainEvent_Suffix()
    {
        var result = Types.InAssembly(_domainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .HaveNameEndingWith("DomainEvent")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All IDomainEvent implementations must be named with a 'DomainEvent' suffix.\n" +
            FormatFailures(result));
    }

    // =========================================================================
    //  Domain – Entity / AggregateRoot / ValueObject conventions
    // =========================================================================

    [Fact]
    public void Domain_Entities_Should_Inherit_Entity()
    {
        // Every concrete class in a "Domain" sub-folder that is not a ValueObject
        // and is not an enum or open generic definition must inherit from Entity<T>.
        var offenders = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain.")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotInherit(typeof(ValueObject))
            .GetTypes()
            .Where(t => !t.IsEnum
                     && !t.IsGenericTypeDefinition
                     && t.Name != "ApiLog"
                     && !InheritsFromGenericType(t, typeof(Entity<>)))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ValueObjects_Should_Inherit_ValueObject_Base()
    {
        // Any concrete class whose name ends with "ValueObject" or resides in
        // a "ValueObject" namespace folder must extend ValueObject.
        var result = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespaceContaining(".ValueObject")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(ValueObject))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All types in a 'ValueObject' namespace must inherit ValueObject.\n" +
            FormatFailures(result));
    }

    [Fact]
    public void AggregateRoots_Should_Inherit_AggregateRoot()
    {
        // Every concrete type in an "AggregateRoot" namespace must inherit
        // from AggregateRoot<T> to gain domain event support.
        var offenders = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespaceContaining(".AggregateRoot")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(t => t.Name != "ApiLog"
                     && !InheritsFromGenericType(t, typeof(AggregateRoot<>)))
            .ToList();

        Assert.Empty(offenders);
    }

    // =========================================================================
    //  Domain – IAuditableEntity convention
    // =========================================================================

    [Fact]
    public void Domain_Aggregates_Should_Implement_IAuditableEntity()
    {
        // All concrete domain entity classes in a *.Domain.* namespace (excluding
        // enums, value objects, and open generic definitions) must implement IAuditableEntity<T>.
        var offenders = Types.InAssembly(_domainAssembly)
            .That()
            .ResideInNamespaceContaining(".Domain.")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotInherit(typeof(ValueObject))
            .GetTypes()
            .Where(t => !t.IsEnum
                     && !t.IsGenericTypeDefinition
                     && t.Name != "ApiLog"
                     && !ImplementsGenericInterface(t, typeof(IAuditableEntity<>)))
            .ToList();

        Assert.Empty(offenders);
    }

    // =========================================================================
    //  Infrastructure services – interface naming convention
    // =========================================================================

    [Fact]
    public void Infrastructure_Services_Should_Have_Matching_Interface()
    {
        // Every concrete (non-abstract) class in SC.Infrastructure.Services.*
        // must implement at least one interface that starts with "I".
        var offenders = Types.InAssembly(_infrastructureAssembly)
            .That()
            .ResideInNamespaceContaining("Services")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(t => !t.GetInterfaces().Any(i => i.Name.StartsWith("I")))
            .ToList();

        Assert.Empty(offenders);
    }

    // =========================================================================
    //  API Controllers – naming and base class
    // =========================================================================

    [Fact]
    public void Controllers_Should_End_With_Controller_Suffix()
    {
        var result = Types.InAssembly(_apiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Controllers_Should_Inherit_ControllerBase()
    {
        var result = Types.InAssembly(_apiAssembly)
            .That()
            .HaveNameEndingWith("Controller")
            .Should()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "All controllers must inherit ControllerBase.\n" +
            FormatFailures(result));
    }

    // =========================================================================
    //  Placement rules – commands/queries belong in Application
    // =========================================================================

    [Fact]
    public void IQuery_Implementations_Should_Reside_In_Application_Assembly()
    {
        // No assembly other than SC.Application should declare IQuery types.
        foreach (var asm in new[] { _domainAssembly, _contractAssembly, _infrastructureAssembly, _persistenceAssembly, _apiAssembly })
        {
            var offenders = Types.InAssembly(asm)
                .That()
                .ImplementInterface(typeof(IQuery<>))
                .Or()
                .ImplementInterface(typeof(IQuery))
                .GetTypes()
                .ToList();

            Assert.Empty(offenders);
        }
    }

    [Fact]
    public void ICommand_Implementations_Should_Reside_In_Application_Assembly()
    {
        foreach (var asm in new[] { _domainAssembly, _contractAssembly, _infrastructureAssembly, _persistenceAssembly, _apiAssembly })
        {
            var offenders = Types.InAssembly(asm)
                .That()
                .ImplementInterface(typeof(ICommand))
                .Or()
                .ImplementInterface(typeof(ICommand<>))
                .GetTypes()
                .ToList();

            Assert.Empty(offenders);
        }
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    /// <summary>Returns true when <paramref name="type"/> is a C# record class.</summary>
    private static bool IsRecord(Type type)
        => type.GetMethod("<Clone>$") is not null
        || type.GetProperties().Any(p => p.Name == "EqualityContract");

    /// <summary>Walks the inheritance chain checking for a matching open generic type.</summary>
    private static bool InheritsFromGenericType(Type type, Type openGeneric)
    {
        var current = type.BaseType;
        while (current is not null && current != typeof(object))
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>Returns true when <paramref name="type"/> implements an open generic interface.</summary>
    private static bool ImplementsGenericInterface(Type type, Type openGenericInterface)
        => type.GetInterfaces()
               .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

    /// <summary>Formats the failing type names into a readable list for assertion messages.</summary>
    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypes is null or { Count: 0 }) return string.Empty;
        return "Failing types:\n  - " + string.Join("\n  - ", result.FailingTypes.Select(t => t.FullName));
    }
}
