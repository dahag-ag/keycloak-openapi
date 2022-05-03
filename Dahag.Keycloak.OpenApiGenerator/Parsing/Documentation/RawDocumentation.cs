namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;

public class RawDocumentation
{
	public string? Text { get; set; }
	public Dictionary<string, string> ParamText { get; set; } = new();
}