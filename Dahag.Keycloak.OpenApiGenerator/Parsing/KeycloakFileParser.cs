using Antlr4.Runtime;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using Dahag.Keycloak.OpenApiGenerator.PostProcess;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing;

public class KeycloakFileParser
{
	public static RawRxJsResource ParseResource(string input)
	{
		var parser = CreateJavaParser(input);

		var resourceInterpreter = new ResourceInterpreter();
		var parsedResource = resourceInterpreter.Visit(parser.compilationUnit());

		if (resourceInterpreter.CurrentPending != null)
			throw new Exception("There was still a pending action");
		
		foreach (var action in parsedResource.Actions)
			PostProcessDocumentation(action, input);

		return parsedResource;
	}
	
	private static void PostProcessDocumentation(RawRxJsResourceAction action, string input)
	{
		var documentationFinder = new DocumentationFinder();
		var documentation = documentationFinder.Find(action, input);
		action.Documentation = documentation;
	}
	
	public static List<RawRepresentation> ParseRepresentation(string input)
	{
		var parser = CreateJavaParser(input);

		var representationInterpreter = new RepresentationInterpreter();
		var parsedRepresentation = representationInterpreter.Visit(parser.compilationUnit());

		return parsedRepresentation;
	}	
	public static KeycloakEnum? ParseEnum(string input)
	{
		var parser = CreateJavaParser(input);

		var enumInterpreter = new EnumInterpreter();
		var parsedRepresentation = enumInterpreter.Visit(parser.compilationUnit());

		return parsedRepresentation;
	}
	
	private static JavaParser CreateJavaParser(string input)
	{
		var antlrInputStream = new AntlrInputStream(input);
		var javaLexer = new JavaLexer(antlrInputStream);
		var commonTokenStream = new CommonTokenStream(javaLexer);
		return new JavaParser(commonTokenStream);
	}
}