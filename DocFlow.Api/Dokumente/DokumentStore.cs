using System.Collections.Concurrent;
using DocFlow.Shared;

namespace DocFlow.Api.Dokumente;

public class DokumentStore
{
    private readonly ConcurrentDictionary<Guid, DokumentDto> _dokumente = new();

    public DokumentStore()
    {
        Seed(new DokumentDto(
            Guid.NewGuid(),
            "Mandantenvereinbarung – Muster",
            "Diese Vorlage regelt die Grundlagen der Zusammenarbeit mit neuen Mandanten, " +
            "einschließlich Vergütung, Kündigungsfristen und Vertraulichkeit.",
            DateTime.UtcNow));
    }

    private void Seed(DokumentDto dokument) => _dokumente[dokument.Id] = dokument;

    public IReadOnlyCollection<DokumentDto> GetAll()
        => _dokumente.Values.OrderByDescending(d => d.ErstelltAmUtc).ToList();

    public DokumentDto Add(string titel, string inhalt)
    {
        var dokument = new DokumentDto(Guid.NewGuid(), titel, inhalt, DateTime.UtcNow);
        _dokumente[dokument.Id] = dokument;
        return dokument;
    }
}
