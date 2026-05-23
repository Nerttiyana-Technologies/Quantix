// Quantix.Sample.MinimalApi — reference usage of Quantix.
//
// AddQuantix and the QuantixMediator are generated into this project by the Quantix source
// generator. There is no runtime reflection: every endpoint below dispatches through
// generated, strongly-typed code.
//
// The host uses WebApplication.CreateSlimBuilder and the project publishes with Native AOT
// (see the .csproj). Every endpoint returns a string, so the response path stays
// serializer-free — the sample publishes with zero trim or AOT warnings.

using Quantix;
using Quantix.Sample;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddQuantix();

var app = builder.Build();

// A query — returns a personalised greeting.
app.MapGet(
    "/greeting/{name}",
    (string name, IMediator mediator, CancellationToken ct) => mediator.Send(new GetGreeting(name), ct));

// A command with a result — records a page visit and reports the running count.
app.MapGet(
    "/visit/{page}",
    async (string page, IMediator mediator, CancellationToken ct) =>
        $"Visit #{await mediator.Send(new RecordVisit(page), ct)} recorded for '{page}'.");

// A generic query — DescribeEntity<Product> is closed and dispatched at compile time.
app.MapGet(
    "/describe/{id:int}",
    (int id, IMediator mediator, CancellationToken ct) => mediator.Send(new DescribeEntity<Product>(id), ct));

app.Run();
