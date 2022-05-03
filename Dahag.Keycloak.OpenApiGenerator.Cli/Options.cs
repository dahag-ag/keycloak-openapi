using CommandLine;

namespace Dahag.Keycloak.OpenApiGenerator.Cli;

public class Options
{
	[Value(0, Required = true)]
	public string KeycloakRoot { get; set; } = null!;
	
	[Value(1, Required = false)]
	public string? Output { get; set; }
}