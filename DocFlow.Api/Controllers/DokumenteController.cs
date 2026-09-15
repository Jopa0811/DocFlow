using MediatR;
using DocFlow.Api.Dokumente;
using DocFlow.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DokumenteController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DokumentDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetDokumenteQuery(), cancellationToken));

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyCollection<DokumentDto>>> Search(
        [FromQuery] string q, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new SearchDokumenteQuery(q), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<DokumentDto>> Create(CreateDokumentRequest request, CancellationToken cancellationToken)
    {
        var created = await mediator.Send(new CreateDokumentCommand(request.Titel, request.Inhalt), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }
}
