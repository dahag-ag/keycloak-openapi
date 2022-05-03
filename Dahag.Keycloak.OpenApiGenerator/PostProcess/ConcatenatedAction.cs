using System.Diagnostics;
using System.Text.RegularExpressions;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using HttpMethod = Dahag.Keycloak.OpenApiGenerator.Parsing.Resource.HttpMethod;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{HttpMethod}_{Route} -> {ReturnType.ToString()}")]
public class ConcatenatedAction : IAction
{
	public string Tag => _childAction.Tag ?? _baseAction.Tag ?? throw new Exception("Missing Tag");

	public string? Route
	{
		get
		{
			var baseRoute = _baseAction.Route;
			
			var childRoute = _childAction.Route;

			if (!childRoute?.StartsWith("/") ?? false)
				childRoute = "/" + childRoute;
			
			if (!baseRoute?.StartsWith("/") ?? false)
				baseRoute = "/" + baseRoute;

			return FixRouteParameterDuplicates(baseRoute + childRoute);
		}
	}

	public string? ImplicitRoute {
		get
		{
			var baseRoute = _baseAction.ImplicitRoute;
			
			var childRoute = _childAction.ImplicitRoute;

			if (!childRoute.StartsWith("/"))
				childRoute = "/" + childRoute;
			
			if (!baseRoute.StartsWith("/"))
				baseRoute = "/" + baseRoute;

			return FixRouteParameterDuplicates(baseRoute + childRoute);
		}
	}

	private string? FixRouteParameterDuplicates(string? route)
	{
		if (route == null)
		{
			return route;
		}
		
		var parameters = Parameters;

		var sameParameterGroups = parameters.GroupBy(ParameterExtensions.OriginalName).Where(x => x.Count() > 1).ToList();

		if (!sameParameterGroups.Any())
			return route;

		foreach (var sameParameterGroup in sameParameterGroups)
		{
			var parameterNameToReplace = sameParameterGroup.Key;
			var parameterRegex = new Regex($@"\{{{parameterNameToReplace}\}}");
			var sameParameterGroupList = sameParameterGroup.ToList();

			foreach (var parameter in sameParameterGroupList)
			{
				route = parameterRegex.Replace(route, $"{{{parameter.Name}}}", 1);
			}
		}

		return route;
	}

	public string? Documentation => !string.IsNullOrWhiteSpace(_childAction.Documentation) ? _childAction.Documentation :_baseAction.Documentation;
	public IEnumerable<IParameter> Parameters
	{
		get
		{
			var parameters = FixDuplicateParameters(_baseAction.Parameters.ToList(), _childAction.Parameters.ToList());
			return parameters;
		}
	}

	private static IEnumerable<IParameter> FixDuplicateParameters(IList<IParameter> ownParameters, IList<IParameter> childParameters)
	{
		var ownCollisions = ownParameters.Where(own => childParameters.Any(childParameter => childParameter.Name == own.Name));
		var ownNonCollisions = ownParameters.Where(own => childParameters.All(childParameter => childParameter.Name != own.Name));
		var childCollisions = childParameters.Where(child => ownParameters.Any(ownParameter => ownParameter.Name == child.Name));
		var childNonCollisions = childParameters.Where(child => ownParameters.All(ownParameter => ownParameter.Name != child.Name));

		var incrementedOwnCollisions = ownCollisions.Select(Increment).ToList();

		var incrementedChildCollisions = childCollisions.Select(Increment).ToList();

		if (incrementedOwnCollisions.Any())
			incrementedChildCollisions = incrementedChildCollisions.Select(Increment).ToList();

		var parameters = ownNonCollisions.Concat(incrementedOwnCollisions).Concat(childNonCollisions).Concat(incrementedChildCollisions);
		return parameters;
	}

	private static IParameter Increment(IParameter parameter)
	{
		if (parameter is not IncrementingParameter asIncrementingParameter) 
			return new IncrementingParameter(parameter);
		
		asIncrementingParameter.Increment();
		return parameter;
	}

	public TypeInfo? ReturnType => _childAction.ReturnType;
	public IEnumerable<MediaType> Consumes => _baseAction.Consumes.Concat(_childAction.Consumes);
	public IEnumerable<MediaType> Produces => _baseAction.Produces.Concat(_childAction.Produces);
	public HttpMethod HttpMethod => _childAction.HttpMethod;

	private readonly IAction _baseAction;
	private readonly IAction _childAction;

	public ConcatenatedAction(IAction baseAction, IAction childAction)
	{
		_baseAction = baseAction;
		_childAction = childAction;
	}
}