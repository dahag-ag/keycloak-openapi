using System.Text.RegularExpressions;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

public class ActionAnnotationInterpreter : JavaParserBaseVisitor<ActionAnnotation?>
{
	private ActionAnnotation? _actionAnnotation = new();

	public override ActionAnnotation? VisitAnnotation(JavaParser.AnnotationContext context)
	{
		var qualifiedName = context.qualifiedName().GetText();

		if (qualifiedName == "Path")
		{
			var pathValue = context.elementValue().GetText().Trim('"');
			_actionAnnotation!.Path = pathValue;
		}
		else if (qualifiedName == "Produces")
		{
			_actionAnnotation!.Produces = ParseAsMediaTypes(context.elementValue().GetText()).ToList();
		}
		else if (qualifiedName == "Consumes")
		{
			_actionAnnotation!.Consumes = ParseAsMediaTypes(context.elementValue().GetText()).ToList();
		}
		else if (Enum.TryParse<HttpMethod>(qualifiedName, true, out var httpMethod))
		{
			_actionAnnotation!.HttpMethod = httpMethod;
		}
		else
		{
			_actionAnnotation = null;
		}

		return _actionAnnotation;	
	}

	private IEnumerable<MediaType> ParseAsMediaTypes(string input)
	{
		var regex = new Regex(@"MediaType\.(?<mediaType>\w+)");

		var matches = regex.Matches(input);

		return matches.Select(x => ParseAsMediaType(x.Groups["mediaType"].Value));
	}
	
	private static MediaType ParseAsMediaType(string rawMediaType)
	{
		return rawMediaType switch
		{
			"APPLICATION_JSON" => MediaType.ApplicationJson,
			"APPLICATION_FORM_URLENCODED" => MediaType.FormUrlEncoded,
			"APPLICATION_XML" => MediaType.Xml,
			"TEXT_PLAIN" => MediaType.PlainText,
			"MULTIPART_FORM_DATA" => MediaType.MultiPartFormData,
			"APPLICATION_OCTET_STREAM" => MediaType.OctetStream,
			_ => throw new ArgumentOutOfRangeException(nameof(rawMediaType), rawMediaType, "Did not recognize this Keycloak MediaType")
		};
	}
}