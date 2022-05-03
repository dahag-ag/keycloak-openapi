namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public interface IActionCollection
{
	public string Name { get; }
	IEnumerable<IAction> Actions { get; }
}