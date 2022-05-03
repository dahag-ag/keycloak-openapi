namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

public class RawRxjsParam
{
	public string? Name { get; set; }
	public string? PathParam { get; set; }
	public string? Type { get; set; }
	public string? Default { get; set; }
	public bool InternalJavaJankToIgnore { get; set; }
	public ParamSource? ParamSource { get; set; }	

	public override string ToString()
	{
		var ignore = InternalJavaJankToIgnore ? $"IGNORE": "";
		var defaultExpression = Default != null? $" = {Default}": "";
		var paramSourceExpression = ParamSource != null? Enum.GetName(ParamSource.Value): "";
		return $"{ignore}({paramSourceExpression}){PathParam}({Name}:{Type}){defaultExpression}";
	}
}

public enum ParamSource
{
	Body,
	Query,
	Path,
	Form
}