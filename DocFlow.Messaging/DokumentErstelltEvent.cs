namespace DocFlow.Messaging;

public record DokumentErstelltEvent(Guid DokumentId, string Titel, string Inhalt, DateTime ErstelltAmUtc);
