using System.Diagnostics;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

[DebuggerDisplay("{Type} {Name}")]
public class RawProperty
{
	public string Name { get; set; }
	public string Type { get; set; }

	public RawProperty(string name, string type)
	{
		Name = name;
		Type = type;
	}
}