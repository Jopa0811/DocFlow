using MediatR;
using DocFlow.Shared;

namespace DocFlow.Api.Dokumente;

public record GetDokumenteQuery : IRequest<IReadOnlyCollection<DokumentDto>>;

public class GetDokumenteQueryHandler(DokumentStore store)
    : IRequestHandler<GetDokumenteQuery, IReadOnlyCollection<DokumentDto>>
{
    public Task<IReadOnlyCollection<DokumentDto>> Handle(GetDokumenteQuery request, CancellationToken cancellationToken)
        => Task.FromResult(store.GetAll());
}
