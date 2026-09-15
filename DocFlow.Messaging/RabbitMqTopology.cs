namespace DocFlow.Messaging;

public static class RabbitMqTopology
{
    public const string DokumenteExchange = "dokumente";
    public const string DokumentErstelltRoutingKey = "dokument.erstellt";
    public const string SearchIndexQueue = "dokumente.search-index";
}
