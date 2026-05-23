using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>
/// Tests for generic messages (design section 4.6, D14): the generator closes an open-generic
/// handler over every concrete instantiation of its message found in the compilation, and
/// reports QTX0011 for an open-generic handler that no instantiation uses.
/// </summary>
public class GenericMessageTests
{
    [Fact]
    public void Closes_an_open_generic_handler_over_each_instantiation()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed class Customer { }
            public sealed class Order { }

            public sealed record GetById<TEntity>(int Id) : IQuery<string>;

            public sealed class GetByIdHandler<TEntity> : IQueryHandler<GetById<TEntity>, string>
            {
                public ValueTask<string> Handle(GetById<TEntity> query, CancellationToken ct) => new("x");
            }

            public static class Usage
            {
                public static object A() => new GetById<Customer>(1);
                public static object B() => new GetById<Order>(2);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.False(result.HasDiagnostic("QTX0011"));
        Assert.False(result.HasDiagnostic("QTX0001"));
        Assert.Contains("GetByIdHandler<global::App.Customer>", result.AllGeneratedSource);
        Assert.Contains("GetByIdHandler<global::App.Order>", result.AllGeneratedSource);
        Assert.Contains("global::App.GetById<global::App.Customer>", result.AllGeneratedSource);
        Assert.Contains("global::App.GetById<global::App.Order>", result.AllGeneratedSource);
    }

    [Fact]
    public void Reports_QTX0011_for_an_open_generic_handler_with_no_instantiation()
    {
        const string source = """
            using Quantix;
            using System.Threading;
            using System.Threading.Tasks;

            namespace App;

            public sealed record GetById<TEntity>(int Id) : IQuery<string>;

            public sealed class GetByIdHandler<TEntity> : IQueryHandler<GetById<TEntity>, string>
            {
                public ValueTask<string> Handle(GetById<TEntity> query, CancellationToken ct) => new("x");
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0011"));
    }

    [Fact]
    public void Reports_QTX0001_for_a_generic_instantiation_with_no_handler()
    {
        const string source = """
            using Quantix;

            namespace App;

            public sealed class Customer { }

            public sealed record GetById<TEntity>(int Id) : IQuery<string>;

            public static class Usage
            {
                public static object A() => new GetById<Customer>(1);
            }
            """;

        GeneratorResult result = GeneratorTestHarness.Run(source);

        Assert.True(result.HasDiagnostic("QTX0001"));
    }
}
