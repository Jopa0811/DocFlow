using MediatR;
using DocFlow.Shared;

namespace DocFlow.Api.Dokumente;

public record CreateDokumentCommand(string Titel, string Inhalt) : IRequest<DokumentDto>;

public class CreateDokumentCommandHandler(DokumentStore store, IPublisher mediatorPublisher)
    : IRequestHandler<CreateDokumentCommand, DokumentDto>
{
    public async Task<DokumentDto> Handle(CreateDokumentCommand request, CancellationToken cancellationToken)
    {
        var created = store.Add(request.Titel, request.Inhalt);
        await mediatorPublisher.Publish(new DokumentErstelltNotification(created), cancellationToken);
        return created;
    }
}
