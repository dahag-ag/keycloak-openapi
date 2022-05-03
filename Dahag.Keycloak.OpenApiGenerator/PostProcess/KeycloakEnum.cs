using System.Diagnostics;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{Name}")]
public class KeycloakEnum
{
	public string Name { get; set; }
	public Dictionary<int, string> Map { get; } = new();
}