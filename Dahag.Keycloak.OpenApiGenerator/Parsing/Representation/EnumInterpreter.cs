using Dahag.Keycloak.OpenApiGenerator.PostProcess;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;

public class EnumInterpreter : JavaParserBaseVisitor<KeycloakEnum?>
{
	private KeycloakEnum? _enum;
	private int _counter = 0;

	public override KeycloakEnum? VisitEnumDeclaration(JavaParser.EnumDeclarationContext context)
	{
		_enum = new KeycloakEnum
		{
			Name = context.identifier().GetText()
		};

		base.VisitEnumDeclaration(context);

		return _enum;
	}


	public override KeycloakEnum? VisitEnumConstant(JavaParser.EnumConstantContext context)
	{
		var staticIndexEnum = !(context.arguments()?.IsEmpty ?? true);
		var enumIdentifier = context.identifier().GetText();
		if (staticIndexEnum)
		{
			_enum!.Map.Add(int.Parse(context.arguments().expressionList().GetText()), enumIdentifier);
		}
		else
		{
			_enum!.Map.Add(_counter, enumIdentifier);
		}
		_counter++;
		return base.VisitEnumConstant(context);
	}
}