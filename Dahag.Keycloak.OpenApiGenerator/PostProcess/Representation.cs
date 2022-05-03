using System.Diagnostics;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{Name}")]
public class Representation : IRepresentation
{
	public string Name { get; }
	public IEnumerable<Property> Properties { get; }

	public Representation(string name, IEnumerable<Property> properties)
	{
		Name = name;
		Properties = properties;
	}

	public static (Representation? Representation, List<RawProperty> UnprocessableProperties) TryCreate(RawRepresentation rawRepresentation,
		List<IRepresentation> knownRepresentations, List<KeycloakEnum> enums)
	{
		var propertiesConverted = rawRepresentation.Properties.Select(x =>
		{
			var tryCreate = Property.TryCreate(x, knownRepresentations, enums);
			return (Processed: tryCreate, Raw: x);
		}).ToList();

		if (propertiesConverted.Any(x => x.Processed == null))
			return (null, propertiesConverted.Where(x => x.Processed == null).Select(x => x.Raw).ToList());

		var propertyDuplicates = propertiesConverted.GroupBy(x => x.Processed!.Name);
		if (propertyDuplicates.Any(x => x.Count() > 1))
			throw new Exception("Duplicate properties");
		
		return (new Representation(rawRepresentation.Name, propertiesConverted.Select(x => x.Processed)!), new List<RawProperty>());
	}
}