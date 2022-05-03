namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public class TemporaryForceCreatedStandInRepresentation : IRepresentation
{
	public IRepresentation? RealRepresentationImplementation { get; set; }
	public string Name { get; set; }

	public IEnumerable<Property> Properties =>
		RealRepresentationImplementation?.Properties ?? throw new Exception("ForceCreatedRepresentation was not completed");
}