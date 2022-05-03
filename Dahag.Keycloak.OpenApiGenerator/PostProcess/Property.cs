using System.Diagnostics;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{Type.ToString()} {Name}")]
public class Property
{
	public string Name { get; }
	public TypeInfo Type { get; }

	public Property(string name, TypeInfo type)
	{
		Name = name;
		Type = type;
	}

	public static Property? TryCreate(RawProperty rawProperty, List<IRepresentation> knownRepresentations, List<KeycloakEnum> enums)
	{
		var typeInfo = TypeInfo.TryCreate(rawProperty.Type, knownRepresentations, enums);

		if (typeInfo == null)
			return null;

		return new Property(rawProperty.Name, typeInfo);
	}
}