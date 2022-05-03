using System.Diagnostics;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;

[DebuggerDisplay("{Name}")]
public class RawRepresentation
{
	public string Name { get; set; }
	public List<RawProperty> Properties { get; } = new();
}