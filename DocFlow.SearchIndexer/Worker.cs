using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using DocFlow.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocFlow.SearchIndexer;

public class Worker(ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var elasticUri = configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
        var elasticClient = new ElasticsearchClient(new Uri(elasticUri));

        var factory = new ConnectionFactory { HostName = configuration["RabbitMq:Host"] ?? "localhost" };

        IConnection connection = await ConnectWithRetryAsync(factory, stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.DokumenteExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            RabbitMqTopology.SearchIndexQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            RabbitMqTopology.SearchIndexQueue, RabbitMqTopology.DokumenteExchange, RabbitMqTopology.DokumentErstelltRoutingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            var @event = JsonSerializer.Deserialize<DokumentErstelltEvent>(args.Body.Span);
            if (@event is not null)
            {
                await IndexDocumentAsync(elasticClient, @event, stoppingToken);
            }

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            RabbitMqTopology.SearchIndexQueue, autoAck: false, consumer, cancellationToken: stoppingToken);

        logger.LogInformation("SearchIndexer lauscht auf Queue '{Queue}'...", RabbitMqTopology.SearchIndexQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task IndexDocumentAsync(
        ElasticsearchClient elasticClient, DokumentErstelltEvent @event, CancellationToken cancellationToken)
    {
        var response = await elasticClient.IndexAsync(
            @event,
            idx => idx.Index(RabbitMqTopology.DokumenteExchange).Id(@event.DokumentId.ToString()),
            cancellationToken);

        if (response.IsValidResponse)
        {
            logger.LogInformation("Dokument \"{Titel}\" (Id {Id}) indexiert.", @event.Titel, @event.DokumentId);
        }
        else
        {
            logger.LogWarning(
                "Indexierung fehlgeschlagen für Dokument {Id}: {Fehler}", @event.DokumentId, response.DebugInformation);
        }
    }

    private async Task<IConnection> ConnectWithRetryAsync(ConnectionFactory factory, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await factory.CreateConnectionAsync(cancellationToken);
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning("RabbitMQ noch nicht erreichbar (Versuch {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
