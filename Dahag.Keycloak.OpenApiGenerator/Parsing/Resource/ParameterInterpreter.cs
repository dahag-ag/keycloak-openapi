namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

public class ParameterInterpreter : JavaParserBaseVisitor<List<RawRxjsParam>>
{
	private List<RawRxjsParam> _params = new();
	private RawRxjsParam? _current;

	public override List<RawRxjsParam> VisitFormalParameters(JavaParser.FormalParametersContext context)
	{
		base.VisitFormalParameters(context);
		return _params;
	}

	public override List<RawRxjsParam> VisitFormalParameter(JavaParser.FormalParameterContext context)
	{
		if (_current != null)
			throw new Exception("There is still a pending param. Fix the parsing logic lol");
		
		_current = new RawRxjsParam
		{
			Type = context.typeType().GetText(),
			Name = context.variableDeclaratorId().identifier().GetText()
		};

		base.VisitFormalParameter(context);

		FinalizeCurrent();

		return _params;
	}

	public override List<RawRxjsParam> VisitLastFormalParameter(JavaParser.LastFormalParameterContext context)
	{
		if (_current != null)
			throw new Exception("There is still a pending param. Fix the parsing logic lol");
		
		_current = new RawRxjsParam
		{
			Type = context.typeType().GetText(),
			Name = context.variableDeclaratorId().identifier().GetText()
		};

		base.VisitLastFormalParameter(context);

		FinalizeCurrent();

		return _params;
	}

	private void FinalizeCurrent()
	{
		if (!_current!.InternalJavaJankToIgnore && _current.ParamSource == null)
			_current.ParamSource = ParamSource.Body;
		
		_params.Add(_current!);
		_current = null;
	}

	public override List<RawRxjsParam> VisitAnnotation(JavaParser.AnnotationContext context)
	{
		if (_current == null)
			throw new Exception("Expected to be building a query parameter");

		var qualifiedName = context.qualifiedName().GetText();

		if (ShouldCompletelyIgnoreParam(qualifiedName))
		{
			_current.InternalJavaJankToIgnore = true;
			return base.VisitAnnotation(context);
		}

		if (TryHandleAsDefaultValue(qualifiedName, context))
			return base.VisitAnnotation(context);
		
		if (TryHandleAsParamPlacementInfo(qualifiedName, context))
			return base.VisitAnnotation(context);

		throw new ArgumentOutOfRangeException(nameof(qualifiedName), qualifiedName, "This is not a recognized parameter annotation");
	}

	private static bool ShouldCompletelyIgnoreParam(string qualifiedName)
	{
		return qualifiedName switch
		{
			"Context" => true,
			_ => false
		};
	}
	
	private bool TryHandleAsDefaultValue(string qualifiedName, JavaParser.AnnotationContext context)
	{
		// ReSharper disable once InvertIf
		if (qualifiedName == "DefaultValue")
		{
			_current!.Default = context.elementValue().GetText().Trim('"');
			return true;
		}

		return false;
	}
	
	private bool TryHandleAsParamPlacementInfo(string qualifiedName, JavaParser.AnnotationContext context)
	{
		var paramPlacementType = DetermineParamType(qualifiedName);

		if (paramPlacementType == null)
			return false;
		
		_current!.ParamSource = paramPlacementType.Value;

		if (_current.ParamSource != ParamSource.Body)
			_current.PathParam = context.elementValue().GetText().Trim('"');

		return true;
	}
	
	private static ParamSource? DetermineParamType(string qualifiedName)
	{
		return qualifiedName switch
		{
			"QueryParam" => ParamSource.Query,
			"PathParam" => ParamSource.Path,
			"FormParam" => ParamSource.Form,
			"" => ParamSource.Body,
			_ => null
		};
	}
}