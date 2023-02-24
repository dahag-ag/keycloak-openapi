using System.Diagnostics;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

[DebuggerDisplay($"{nameof(ToString)}()")]
public class RawRxjsParam
{
	public string? Name { get; set; }
	public string? PathParam { get; set; }
	public string? Type { get; set; }
	public string? Default { get; set; }
	public bool InternalJavaJankToIgnore { get; set; }
	public bool Implicit { get; set; }
	public ParamSource? ParamSource { get; set; }	

	public override string ToString()
	{
		var ignore = InternalJavaJankToIgnore ? "IGNORE": "";
		var defaultExpression = Default != null? $" = {Default}": "";
		var paramSourceExpression = ParamSource != null? Enum.GetName(ParamSource.Value): "";
		var implicitExpression = Implicit ? "implicit": "explicit";
		return $"{ignore}({implicitExpression})({paramSourceExpression}){PathParam}({Name}:{Type}){defaultExpression}";
	}

	public static RawRxjsParam CreateImplicitBodyParameter()
	{
		return new RawRxjsParam
		{
			Name = "data",
			ParamSource = Resource.ParamSource.Body,
			Type = "Object",
			PathParam = null,
			Default = null,
			Implicit = true
		};
	}
}

public enum ParamSource
{
	Body,
	Query,
	Path,
	Form
}