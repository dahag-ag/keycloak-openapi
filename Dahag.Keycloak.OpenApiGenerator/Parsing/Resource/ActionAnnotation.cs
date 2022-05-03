using System.Diagnostics;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

[DebuggerDisplay("{_path} ## {_httpMethod}")]

public class ActionAnnotation
{
	public List<MediaType>? Consumes { get; set; }

	public List<MediaType>? Produces { get; set; }
	public string? Path { get; set; }
	public HttpMethod? HttpMethod { get; set; }
}