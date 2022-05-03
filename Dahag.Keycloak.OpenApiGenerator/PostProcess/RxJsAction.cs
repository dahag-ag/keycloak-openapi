using System.Diagnostics;
using System.Text.RegularExpressions;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using HttpMethod = Dahag.Keycloak.OpenApiGenerator.Parsing.Resource.HttpMethod;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

[DebuggerDisplay("{HttpMethod}_{Route} -> {ReturnType.ToString()}")]
public class RxJsAction : IAction
{
	public string Tag { get; set; }

	public string? Route
	{
		get
		{
			if (!string.IsNullOrEmpty(_route) && !_route.StartsWith("/"))
			{
				return "/" + _route;
			}

			return _route;
		}
	}

	public string? ImplicitRoute { get; set; }

	public string? Documentation { get; }
	public IEnumerable<IParameter> Parameters { get; }
	public TypeInfo? ReturnType { get; }
	public IEnumerable<MediaType> Consumes { get; }
	public IEnumerable<MediaType> Produces { get; }
	public HttpMethod HttpMethod { get; }

	public RxJsAction(string? route, string? documentation, IEnumerable<IParameter> parameters, TypeInfo? returnType, IEnumerable<MediaType> consumes,
		IEnumerable<MediaType> produces, HttpMethod httpMethod)
	{
		_route = route;
		Documentation = documentation;
		Parameters = parameters;
		ReturnType = returnType;
		Consumes = consumes;
		Produces = produces;
		HttpMethod = httpMethod;
	}

	public static IAction CreateNormal(RawRxJsResourceAction raw, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		if (raw.ProbablyParentOfAnotherResource)
			throw new Exception("Wrong method :v ");

		var parameters = raw.Parameters.Where(x => !x.InternalJavaJankToIgnore)
			.Select(x => Parameter.Create(x, raw.Documentation, representations, keycloakEnums)).ToList();
		var returnType = TypeInfo.TryCreate(raw.ReturnsType!, representations, keycloakEnums) ??
						 throw new Exception($"Couldn't resolve type '{raw.ReturnsType}'");

		if (raw.HttpMethod == null)
			throw new Exception("No httpMethod");

		if (raw.Path == null && raw.ImplicitPath == null)
			throw new Exception("No path");

		return new RxJsAction(FixPath(raw.Path), raw.Documentation?.Text, parameters, returnType, raw.Consumes ?? new List<MediaType>(),
			raw.Produces ?? new List<MediaType>(), raw.HttpMethod.Value)
		{
			Tag = raw.Tag,
			ImplicitRoute = raw.ImplicitPath
		};
	}

	public static IAction CreateBase(RawRxJsResourceAction raw, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var normalType = TypeInfo.TryCreate(raw.ReturnsType!, representations, keycloakEnums);

		if (normalType != null)
			throw new Exception("Supposedly parent route action return value was able to be resolved to a normal type. Sus");

		if (!raw.ProbablyParentOfAnotherResource)
			throw new Exception("Wrong method :v ");

		var parameters = raw.Parameters.Where(x => !x.InternalJavaJankToIgnore)
			.Select(x => Parameter.Create(x, raw.Documentation, representations, keycloakEnums))
			.ToList();

		if (raw.HttpMethod == null)
			throw new Exception("No httpMethod");

		if (raw.Path == null)
			throw new Exception("No path");

		return new RxJsAction(FixPath(raw.Path), raw.Documentation?.Text, parameters, null, raw.Consumes ?? new List<MediaType>(),
			raw.Produces ?? new List<MediaType>(), raw.HttpMethod.Value)
		{
			Tag = raw.Tag,
			ImplicitRoute = raw.ImplicitPath
		};
	}

	private static Regex _specialPathRegex = new("{(?<parameter>\\w+):.+}");
	private readonly string? _route;

	private static string? FixPath(string? input)
	{
		if (input == null)
			return null;
		
		var match = _specialPathRegex.Match(input);

		if (!match.Success)
			return input;

		var parameterName = match.Groups["parameter"];

		return input.Replace(match.Value, $"{{{parameterName}}}");
	}
}