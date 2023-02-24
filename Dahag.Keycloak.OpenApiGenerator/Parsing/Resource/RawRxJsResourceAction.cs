using System.Diagnostics;
using System.Text.RegularExpressions;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

[DebuggerDisplay("{HttpMethod}_{Path} -> {ReturnsType}")]
public class RawRxJsResourceAction
{
	public override int GetHashCode()
	{
		throw new NotImplementedException();
	}

	public static bool operator ==(RawRxJsResourceAction? left, RawRxJsResourceAction? right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(RawRxJsResourceAction? left, RawRxJsResourceAction? right)
	{
		return !Equals(left, right);
	}

	public string? Path { get; set; }
	public string? ImplicitPath { get; set; }
	public HttpMethod? HttpMethod { get; set; }
	public List<RawRxjsParam> Parameters { get; set; } = new();
	public RawDocumentation? Documentation { get; set; }
	public List<MediaType>? Consumes { get; set; }
	public List<MediaType>? Produces { get; set; }

	public string? ReturnsType
	{
		get => _returnsType;
		set
		{
			if (_returnsType != null)
			{
				throw new AlreadySetException(nameof(ReturnsType));
			}

			_returnsType = value;
		}
	}

	public int FoundAtLine { get; set; }
	public int PersistedAtLine { get; set; }
	public bool ProbablyParentOfAnotherResource { get; set; }
	public string? RawMethodBody { get; set; }
	public string? Tag { get; set; }

	private string? _returnsType;


	public void Set(ActionAnnotation actionAnnotation)
	{
		if (actionAnnotation.Path != null && Path != null)
			throw new AlreadySetException(nameof(Path));

		if (actionAnnotation.HttpMethod != null && HttpMethod != null)
			throw new AlreadySetException(nameof(HttpMethod));

		if (actionAnnotation.Consumes != null && Consumes != null)
			throw new AlreadySetException(nameof(Consumes));

		if (actionAnnotation.Produces != null && Produces != null)
			throw new AlreadySetException(nameof(Produces));

		if (actionAnnotation.Path != null)
			Path = actionAnnotation.Path;

		if (actionAnnotation.HttpMethod != null)
			HttpMethod = actionAnnotation.HttpMethod;

		if (actionAnnotation.Consumes != null)
			Consumes = actionAnnotation.Consumes;

		if (actionAnnotation.Produces != null)
			Produces = actionAnnotation.Produces;
	}

	public bool CouldMaybeHaveAnImplicitBodyParameter()
	{
		return Path != null
			   && HttpMethod is Resource.HttpMethod.Post or Resource.HttpMethod.Put
			   && ProbablyParentOfAnotherResource == false
			   && (Consumes?.Any() ?? false)
			   && (!Parameters.Any() || Parameters.All(x => x.ParamSource == ParamSource.Path))
			   && RawMethodBody != null && (RawMethodBody.Contains(".getMultiPartFormParameters()") || RawMethodBody.Contains(".getDecodedFormParameters()") ||
										    new Regex(@"get.+FromRequest\(\)").IsMatch(RawMethodBody));
	}

	public void AddImplicitBodyParameter()
	{
		Parameters = Parameters.Concat(new List<RawRxjsParam>
		{
			RawRxjsParam.CreateImplicitBodyParameter()
		}).ToList();
	}

	public override string ToString()
	{
		var paramsAsString = string.Join('\n', Parameters.Select(x => x.ToString()));
		var path = Path ?? (ImplicitPath != null ? ($"~{ImplicitPath}~") : null);
		return $"{HttpMethod}_{path} -> {ReturnsType} \n {paramsAsString}";
	}
}

public enum MediaType
{
	PlainText,
	ApplicationJson,
	Xml,
	FormUrlEncoded,
	MultiPartFormData,
	OctetStream
}