using MediatR;
using DocFlow.Messaging;

namespace DocFlow.Api.Dokumente;

public class PublishDokumentErstelltToRabbitMq(IEventPublisher publisher)
    : INotificationHandler<DokumentErstelltNotification>
{
    public Task Handle(DokumentErstelltNotification notification, CancellationToken cancellationToken)
        => publisher.PublishAsync(
            new DokumentErstelltEvent(
                notification.Dokument.Id,
                notification.Dokument.Titel,
                notification.Dokument.Inhalt,
                notification.Dokument.ErstelltAmUtc),
            RabbitMqTopology.DokumentErstelltRoutingKey,
            cancellationToken);
}
