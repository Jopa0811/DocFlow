using Elastic.Clients.Elasticsearch;
using MediatR;
using DocFlow.Messaging;
using DocFlow.Shared;

namespace DocFlow.Api.Dokumente;

public record SearchDokumenteQuery(string Suchbegriff) : IRequest<IReadOnlyCollection<DokumentDto>>;

public class SearchDokumenteQueryHandler(ElasticsearchClient elasticClient)
    : IRequestHandler<SearchDokumenteQuery, IReadOnlyCollection<DokumentDto>>
{
    public async Task<IReadOnlyCollection<DokumentDto>> Handle(SearchDokumenteQuery request, CancellationToken cancellationToken)
    {
        var response = await elasticClient.SearchAsync<DokumentErstelltEvent>(s => s
            .Indices(RabbitMqTopology.DokumenteExchange)
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(new[] { "titel", "inhalt" })
                    .Query(request.Suchbegriff))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            return [];
        }

        return response.Documents
            .Select(d => new DokumentDto(d.DokumentId, d.Titel, d.Inhalt, d.ErstelltAmUtc))
            .ToList();
    }
}
