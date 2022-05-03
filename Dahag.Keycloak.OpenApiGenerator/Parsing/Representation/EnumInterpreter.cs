using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
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
		_enum!.Map.Add(_counter, context.GetText());
		_counter++;
		return base.VisitEnumConstant(context);
	}
}