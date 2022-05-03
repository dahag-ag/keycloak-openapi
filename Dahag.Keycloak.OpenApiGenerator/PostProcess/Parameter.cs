using System.Diagnostics;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public interface IParameter
{
	ParamSource Source { get; }
	string? Documentation { get; set; }
	string Name { get; }
	string? Default { get; set; }
	TypeInfo TypeInfo { get; }
}

[DebuggerDisplay("[{Source}]{Name}")]
public class Parameter : IParameter
{
	public ParamSource Source { get; }
	public string? Documentation { get; set; }
	public string Name { get; }
	public string? Default { get; set; }
	public TypeInfo TypeInfo { get; }

	public Parameter(ParamSource source, string name, TypeInfo typeInfo)
	{
		Source = source;
		Name = name;
		TypeInfo = typeInfo;
	}

	public static IParameter Create(RawRxjsParam raw, RawDocumentation? rawDocumentation, List<IRepresentation> representations, List<KeycloakEnum> enums)
	{	
		if (raw.ParamSource == null)
			throw new Exception("No source");

		if (string.IsNullOrEmpty(raw.Name))
			throw new Exception("Empty name");
		
		if (string.IsNullOrEmpty(raw.Type))
			throw new Exception("No Type info");

		var typeInfo = TypeInfo.TryCreate(raw.Type, representations, enums);

		if (typeInfo == null)
			throw new Exception($"Could not find type for {raw.Type}");
		
		return new Parameter(raw.ParamSource!.Value, raw.PathParam ?? raw.Name, typeInfo)
		{
			Default = raw.Default,
			Documentation = rawDocumentation?.ParamText.GetValueOrDefault(raw.Name)
		};
	}
}

public class IncrementingParameter : IParameter
{
	public IParameter OriginalParameter { get; }
	private int _counter = 1;
	public IncrementingParameter(IParameter originalParameter)
	{
		OriginalParameter = originalParameter;
	}

	public ParamSource Source => OriginalParameter.Source;

	public string? Documentation
	{
		get => OriginalParameter.Documentation;
		set => OriginalParameter.Documentation = value;
	}

	public string Name => OriginalParameter.Name + _counter;

	public string? Default
	{
		get => OriginalParameter.Default;
		set => OriginalParameter.Default = value;
	}

	public TypeInfo TypeInfo => OriginalParameter.TypeInfo;

	public void Increment()
	{
		_counter++;
	}
}

public static class ParameterExtensions
{
	public static string OriginalName(this IParameter parameter)
	{
		if (parameter is IncrementingParameter asIncrementingParameter)
			return asIncrementingParameter.OriginalParameter.Name;

		return parameter.Name;
	}
}