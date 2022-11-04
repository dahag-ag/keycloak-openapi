using System.Collections.Generic;
using System.Linq;
using Dahag.Keycloak.OpenApiGenerator.Parsing;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using NUnit.Framework;

namespace Dahag.Keycloak.OpenApiGenerator.Tests;

public static class RawRxJsResourceAssertExtensions {
	
	public static void AssertEquality(this IEnumerable<RawRxJsResourceAction> actual, IEnumerable<RawRxJsResourceAction> expected)
	{
		Assert.That(actual.Count(), Is.EqualTo(expected.Count()), "Mismatch in action count");
		var together = actual.Zip(expected);

		foreach (var tuple in together)
		{
			tuple.First.AssertEquality(tuple.Second);
		}
	}
	
	public static void AssertEquality(this RawRxJsResourceAction actual, RawRxJsResourceAction expected)
	{
		Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
	}
	
	public static void AssertEquality(this IEnumerable<RawRxjsParam> actual, IEnumerable<RawRxjsParam> expected)
	{
		Assert.That(actual.Count(), Is.EqualTo(expected.Count()), "Mistmatch in param count");
		var together = actual.Zip(expected);

		foreach (var tuple in together)
		{
			tuple.First.AssertEquality(tuple.Second);
		}
	}
	
	public static void AssertEquality(this RawRxjsParam actual, RawRxjsParam expected)
	{
		Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
	}
}