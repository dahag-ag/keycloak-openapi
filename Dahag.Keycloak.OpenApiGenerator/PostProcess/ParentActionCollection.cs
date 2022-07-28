using System.Diagnostics;
using System.Diagnostics;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{Name}")]
public class ParentActionCollection : IActionCollection
{
	public string Name { get; }
	public IEnumerable<IAction> Actions => GetActions().Concat(_normalActions);
	public IEnumerable<IActionCollection> SubCollections => _actions.Select(x => x.ChildCollection);
	private readonly IEnumerable<(IAction BaseAction, IActionCollection ChildCollection)> _actions;
	private readonly IEnumerable<IAction> _normalActions;

	public ParentActionCollection(string name, IEnumerable<(IAction BaseAction, IActionCollection ChildCollection)> nested,
		IEnumerable<IAction> normalActions)
	{
		Name = name;
		_actions = new List<(IAction BaseAction, IActionCollection ChildCollection)>(nested!);
		_normalActions = normalActions;
	}

	private IEnumerable<IAction> GetActions()
	{
		foreach (var ownAction in _actions)
		{
			foreach (var childAction in ownAction.ChildCollection.Actions)
			{
				yield return new ConcatenatedAction(ownAction.BaseAction, childAction);
			}
		}
	}

	public static IActionCollection? TryCreate(RawRxJsResource rawRxJsResource, List<IActionCollection> actionCollections,
		List<IRepresentation> representations, List<KeycloakEnum> enums)
	{
		var parentActions = rawRxJsResource.Actions.Where(x => x.ProbablyParentOfAnotherResource)
			.Where(resourceAction => !IgnoreHelper.DisregardAsParentActionFuncs.Any(checkFunc => checkFunc(resourceAction))).ToList();

		var convertedParentedActions = new List<(IAction BaseAction, IActionCollection ChildCollection)>();

		foreach (var rawParentAction in parentActions)
		{
			var matchingCollection = actionCollections.SingleOrDefault(x => x.Name == rawParentAction.ReturnsType);

			if (matchingCollection == null)
				return null;

			var selfAsBaseAction = RxJsAction.CreateBase(rawParentAction, representations, enums);

			convertedParentedActions.Add((selfAsBaseAction, matchingCollection));
		}

		var normalActions = rawRxJsResource.Actions.Where(x => !x.ProbablyParentOfAnotherResource).ToList();
		var convertedNormals = normalActions.Select(x => RxJsAction.CreateNormal(x, representations, enums)).ToList();

		return new ParentActionCollection(rawRxJsResource.Name, convertedParentedActions, convertedNormals);
	}
}