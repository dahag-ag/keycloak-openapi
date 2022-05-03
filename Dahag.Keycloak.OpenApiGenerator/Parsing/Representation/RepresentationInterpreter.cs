using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;

public class RepresentationInterpreter : JavaParserBaseVisitor<List<RawRepresentation>>
{
	private RawRepresentation? _self;
	private List<RawRepresentation> _nested = new();
	private bool _wasLastSeenModifierPublic;
	private bool _ignoreNext;

	public override List<RawRepresentation> VisitClassDeclaration(JavaParser.ClassDeclarationContext context)
	{
		var isNested = _self != null;
		if (isNested)
		{
			var nested = new RepresentationInterpreter().VisitClassDeclaration(context);
			_nested.AddRange(nested);
			return new List<RawRepresentation> {_self!}.Concat(nested).ToList();
		}

		_self = new RawRepresentation
		{
			Name = context.identifier().GetText()
		};
		
		base.VisitClassDeclaration(context);

		return new List<RawRepresentation> {_self!}.Concat(_nested).ToList();
	}

	public override List<RawRepresentation> VisitModifier(JavaParser.ModifierContext context)
	{
		_wasLastSeenModifierPublic = context.GetText() == "public";
		return base.VisitModifier(context);
	}

	public override List<RawRepresentation> VisitMethodDeclaration(JavaParser.MethodDeclarationContext context)
	{
		if (_ignoreNext)
		{
			_ignoreNext = false;
			return base.VisitMethodDeclaration(context);
		}
		
		if (!_wasLastSeenModifierPublic)
			return base.VisitMethodDeclaration(context);
		
		var rawIdentifier = context.identifier().GetText();

		var asValidPropertyName = AsValidPropertyName(rawIdentifier);

		if (asValidPropertyName == null) 
			return base.VisitMethodDeclaration(context);
		
		var type = context.typeTypeOrVoid().GetText();
		_self.Properties.Add(new RawProperty(asValidPropertyName, type));

		return base.VisitMethodDeclaration(context);
	}

	public override List<RawRepresentation> VisitAnnotation(JavaParser.AnnotationContext context)
	{
		if (context.qualifiedName().GetText() == "JsonIgnore")
		{
			_ignoreNext = true;
		}
		
		return base.VisitAnnotation(context);
	}

	private static string? AsValidPropertyName(string input)
	{
		if (input.StartsWith("get"))
		{
			return input[3..];
		}

		// ReSharper disable once ConvertIfStatementToReturnStatement
		if (input.StartsWith("is"))
		{
			return input[2..];
		}

		return null;
	}
}