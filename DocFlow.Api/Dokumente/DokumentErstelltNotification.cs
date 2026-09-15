using MediatR;
using DocFlow.Shared;

namespace DocFlow.Api.Dokumente;

public record DokumentErstelltNotification(DokumentDto Dokument) : INotification;
