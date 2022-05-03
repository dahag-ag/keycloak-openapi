using System.Diagnostics;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{Name}")]
public class ActionCollection : IActionCollection
{
	public string Name { get; }
	public IEnumerable<IAction> Actions { get; }

	public ActionCollection(string name, IEnumerable<IAction> actions)
	{
		Name = name;
		Actions = actions;
	}

	public static IActionCollection? Create(RawRxJsResource rawRxJsResource, List<IActionCollection> alreadyProcessed, List<IRepresentation> complexTypes,
		List<KeycloakEnum> keycloakEnums)
	{
		var parentRoutes = rawRxJsResource.Actions.Where(x => x.ProbablyParentOfAnotherResource).ToList();

		if (parentRoutes.Any())
			return ParentActionCollection.TryCreate(rawRxJsResource, alreadyProcessed, complexTypes, keycloakEnums);

		var resourceActions = rawRxJsResource.Actions.Select(x => RxJsAction.CreateNormal(x, complexTypes, keycloakEnums)).ToList();
		return new ActionCollection(rawRxJsResource.Name, resourceActions);
	}
}