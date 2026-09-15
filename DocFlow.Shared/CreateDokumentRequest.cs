using System.ComponentModel.DataAnnotations;

namespace DocFlow.Shared;

public class CreateDokumentRequest
{
    [Required(ErrorMessage = "Titel darf nicht leer sein.")]
    [MinLength(3, ErrorMessage = "Titel muss mindestens 3 Zeichen lang sein.")]
    public string Titel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Inhalt darf nicht leer sein.")]
    [MinLength(10, ErrorMessage = "Inhalt muss mindestens 10 Zeichen lang sein.")]
    public string Inhalt { get; set; } = string.Empty;
}
