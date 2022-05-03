using System.Text.RegularExpressions;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;

public class DocumentationFinder
{
	private readonly Regex _commentBlockRegex = new(@"(?: *\/\*{2}[\n\r]+)(((?: +\* ?).*[\n\r]+)*) *\*\/[\n\r]+");
	private readonly Regex _parameterDocumentationRegex = new(@"@param (?<parameter>\w+) ?(?<parameterText>.*)");

	public RawDocumentation? Find(RawRxJsResourceAction rawRxJsResourceAction, string fullTextContext)
	{
		var matches = _commentBlockRegex.Matches(fullTextContext);

		var estimatedCommentRange = new Range(rawRxJsResourceAction.PersistedAtLine - 8, rawRxJsResourceAction.PersistedAtLine - 2);
		var commentBlockInRange = matches.SingleOrDefault(x => x.Range(fullTextContext).DoIntersect(estimatedCommentRange));

		if (commentBlockInRange == null)
		{
			return null;
		}

		var documentation = ParseCommentBlock(commentBlockInRange.Value);

		return documentation;
	}


	private RawDocumentation ParseCommentBlock(string commentBlock)
	{
		var parameterMatches = _parameterDocumentationRegex.Matches(commentBlock)
			.ToDictionary(x => x.Groups["parameter"].Value, x => x.Groups["parameterText"].Value.Trim());

		var cleanedBody = BodyWithoutParameters(commentBlock);
		
		return new RawDocumentation
		{
			Text = cleanedBody,
			ParamText = parameterMatches
		};
	}

	private string BodyWithoutParameters(string commentBlock)
	{
		if (commentBlock.Contains('@'))
		{
			commentBlock = commentBlock[..commentBlock.IndexOf('@')];
		}
		
		var cleanedBodyLines = commentBlock.Split('\n').Select(x => x.Replace('*', ' ').Trim());

		return string.Join('\n', cleanedBodyLines);
	}
}