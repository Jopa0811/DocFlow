using Elastic.Clients.Elasticsearch;
using FluentValidation;
using DocFlow.Api.Common;
using DocFlow.Api.Dokumente;
using DocFlow.Messaging;
using DocFlow.Shared;
using Microsoft.AspNetCore.Diagnostics;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton<DokumentStore>();

var elasticUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
builder.Services.AddSingleton(new ElasticsearchClient(new Uri(elasticUri)));

var rabbitMqChannel = await ConnectToRabbitMqAsync(builder.Configuration["RabbitMq:Host"] ?? "localhost");
builder.Services.AddSingleton(rabbitMqChannel);
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

const string ClientCorsPolicy = "ClientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        policy.WithOrigins("https://localhost:7166", "http://localhost:5166")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationException.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());
            await context.Response.WriteAsJsonAsync(new ValidationErrorResponse(errors));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Ein unerwarteter Fehler ist aufgetreten." });
    });
});

app.UseHttpsRedirection();

app.UseCors(ClientCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task<IChannel> ConnectToRabbitMqAsync(string host)
{
    var factory = new ConnectionFactory { HostName = host };

    IConnection? connection = null;
    for (var attempt = 1; attempt <= 10 && connection is null; attempt++)
    {
        try
        {
            connection = await factory.CreateConnectionAsync();
        }
        catch (Exception) when (attempt < 10)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    connection ??= await factory.CreateConnectionAsync();

    var channel = await connection.CreateChannelAsync();
    await channel.ExchangeDeclareAsync(RabbitMqTopology.DokumenteExchange, ExchangeType.Topic, durable: true);
    return channel;
}
