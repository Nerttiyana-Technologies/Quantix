// Quantix.Sample.MinimalApi — reference usage of Quantix.
//
// AddQuantix and the QuantixMediator are generated into this project by the Quantix source
// generator. There is no runtime reflection: the GET endpoint below dispatches through
// generated, strongly-typed code.

using Quantix;
using Quantix.Sample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQuantix();

var app = builder.Build();

app.MapGet(
    "/greeting/{name}",
    (string name, IMediator mediator, CancellationToken ct) => mediator.Send(new GetGreeting(name), ct));

app.MapGet(
    "/visit/{page}",
    (string page, IMediator mediator, CancellationToken ct) => mediator.Send(new RecordVisit(page), ct));

app.MapGet(
    "/describe/{id:int}",
    (int id, IMediator mediator, CancellationToken ct) => mediator.Send(new DescribeEntity<Product>(id), ct));

app.Run();
