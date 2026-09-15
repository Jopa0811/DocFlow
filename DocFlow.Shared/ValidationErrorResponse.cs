namespace DocFlow.Shared;

public record ValidationErrorResponse(Dictionary<string, string[]> Errors);
