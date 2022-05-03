using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using HttpMethod = Dahag.Keycloak.OpenApiGenerator.Parsing.Resource.HttpMethod;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public interface IAction
{
	public string? Tag { get; }
	public string? Route { get; }
	public string? ImplicitRoute { get; }
	public string? Documentation { get; }
	public IEnumerable<IParameter> Parameters { get; }
	public TypeInfo? ReturnType { get; }
	public IEnumerable<MediaType> Consumes { get; }
	public IEnumerable<MediaType> Produces { get; }
	public HttpMethod HttpMethod { get; }
}