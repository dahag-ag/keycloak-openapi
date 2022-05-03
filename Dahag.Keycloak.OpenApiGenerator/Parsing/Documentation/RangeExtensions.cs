using System.Text.RegularExpressions;

namespace Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;

public static class RangeExtensions
{
	public static int LineStart(this Match match, string rawText)
	{
		var textBeforeMatch = rawText[..match.Index];
		return textBeforeMatch.Count(x => x == '\n');
	}

	public static Range Range(this Match match, string rawText)
	{
		var start = match.LineStart(rawText);
		var newLinesInMatch = match.Value.Count(x => x == '\n');
		return new Range(start, start + newLinesInMatch);
	}

	public static bool DoIntersect(this Range a, Range b)
	{
		return a.Contains(b) || b.Contains(a);
	}

	public static bool Contains(this Range rangeA, Range rangeB)
	{
		return rangeA.Contains(rangeB.Start.Value) || rangeA.Contains(rangeB.End.Value);
	}

	public static bool Contains(this Range range, int value)
	{
		return value > range.Start.Value && value < range.End.Value;
	}
}