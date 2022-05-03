using Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using Dahag.Keycloak.OpenApiGenerator.PostProcess;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing;

public class RawKeycloakApiDefinition
{
	public List<RawRepresentation> Representations { get; }
	public List<RawRxJsResource> Resources { get; }
	public List<KeycloakEnum> Enums { get; }

	public RawKeycloakApiDefinition(List<RawRepresentation> representations, List<RawRxJsResource> resources, List<KeycloakEnum> enums)
	{
		Representations = representations;
		Resources = resources;
		Enums = enums;
	}
}