namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public interface IRepresentation
{
	string Name { get; }
	IEnumerable<Property> Properties { get; }
}