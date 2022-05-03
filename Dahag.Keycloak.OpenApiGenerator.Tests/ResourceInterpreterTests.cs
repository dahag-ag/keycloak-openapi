using System.Collections.Generic;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using NUnit.Framework;

namespace Dahag.Keycloak.OpenApiGenerator.Tests;

public class Tests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	[TestCase("TwoSimpleEndpointsResource")]
	[TestCase("TwoSimpleEndpointsResourceShuffledAttributes")]
	[TestCase("TwoSimpleEndpointsResourceWithRandomPrivateMethods")]
	public void Interpret_TwoSimpleEndpoints_TwoResources(string testFile)
	{
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.{testFile}.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());

		var expected = new List<RawRxJsResourceAction>
		{
			new()
			{
				Path = "registration-access-token",
				HttpMethod = HttpMethod.Post,
				ReturnsType = "ClientRepresentation"
			},
			new()
			{
				Path = "client-secret",
				HttpMethod = HttpMethod.Get,
				ReturnsType = "CredentialRepresentation"
			}
		};

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Actions, Has.Count.EqualTo(2));
		actual.Actions.AssertEquality(expected);
	}


	[Test]
	public void Interpret_DifferentConsumeAndProduceTypes_Correct()
	{
		using var manifestResourceStream =
			typeof(Tests).Assembly.GetManifestResourceStream(
				$"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.DifferentConsumeAndProduceTypesEndpointsResource.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Actions, Has.Count.EqualTo(3));
		Assert.That(actual.Actions[0].ProbablyParentOfAnotherResource, Is.False);
		Assert.That(actual.Actions[0].Produces, Is.EquivalentTo(new List<MediaType>
		{
			MediaType.ApplicationJson
		}));
		Assert.That(actual.Actions[0].Consumes, Is.EquivalentTo(new List<MediaType>
		{
			MediaType.ApplicationJson
		}));
		Assert.That(actual.Actions[1].Produces, Is.Null);
		Assert.That(actual.Actions[1].Consumes, Is.EquivalentTo(new List<MediaType>
		{
			MediaType.FormUrlEncoded
		}));
		Assert.That(actual.Actions[2].Consumes, Is.EquivalentTo(new List<MediaType>
		{
			MediaType.ApplicationJson,
			MediaType.Xml,
			MediaType.PlainText,
			MediaType.MultiPartFormData
		}));
	}

	[Test]
	public void Interpret_ImplicitPath_DetectsPath()
	{
		using var manifestResourceStream =
			typeof(Tests).Assembly.GetManifestResourceStream("Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.ImplicitPathResource.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());

		var expected = new List<RawRxJsResourceAction>
		{
			new()
			{
				Path = "clients",
				HttpMethod = HttpMethod.Get,
				ReturnsType = "Stream<ClientRepresentation>"
			}
		};

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Actions, Has.Count.EqualTo(1));
		actual.Actions.AssertEquality(expected);
	}
	
	[Test]
	public void Interpret_Parent_Works()
	{
		using var manifestResourceStream =
			typeof(Tests).Assembly.GetManifestResourceStream("Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.ParentResource.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Actions, Has.Count.EqualTo(1));
		Assert.That(actual.Actions[0].Path, Is.EqualTo("{id}"));
		Assert.That(actual.Actions[0].HttpMethod, Is.EqualTo(HttpMethod.Get));
		Assert.That(actual.Actions[0].ReturnsType, Is.EqualTo("ClientResource"));
		Assert.That(actual.Actions[0].ProbablyParentOfAnotherResource, Is.EqualTo(true));
	}

	[Test]
	public void Interpret_Params_Works()
	{
		using var manifestResourceStream =
			typeof(Tests).Assembly.GetManifestResourceStream("Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.SimpleParamsResource.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());


		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Actions, Has.Count.EqualTo(2));
		actual.Actions[0].Parameters.AssertEquality(new List<RawRxjsParam>
		{
			new()
			{
				Name = "firstResult",
				ParamSource = ParamSource.Query,
				Type = "Integer",
				PathParam = "first"
			},
			new()
			{
				Name = "maxResults",
				ParamSource = ParamSource.Query,
				Type = "Integer",
				PathParam = "max"
			}
		});
		actual.Actions[1].Parameters.AssertEquality(new List<RawRxjsParam>
		{
			new()
			{
				Name = "providerId",
				ParamSource = ParamSource.Path,
				Type = "String",
				PathParam = "providerId"
			},
			new()
			{
				Name = "firstResult",
				ParamSource = ParamSource.Query,
				Type = "Integer",
				PathParam = "first"
			},
			new()
			{
				Name = "action",
				ParamSource = ParamSource.Form,
				Type = "String",
				PathParam = "action"
			},
		});
	}

	[Test]
	public void Interpret_AdvancedParams_Works()
	{
		using var manifestResourceStream =
			typeof(Tests).Assembly.GetManifestResourceStream("Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.AdvancedParamsResource.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());


		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Actions, Has.Count.EqualTo(1));
		actual.Actions[0].Parameters.AssertEquality(new List<RawRxjsParam>
		{
			new()
			{
				Name = "providerId",
				ParamSource = ParamSource.Path,
				Type = "String",
				PathParam = "providerId",
				Default = "foo"
			},
			new()
			{
				Name = "complex",
				ParamSource = ParamSource.Body,
				Type = "Foo",
				PathParam = null,
				Default = "bar"
			},
			new()
			{
				Name = "ignored",
				Type = "HttpResponse",
				PathParam = null,
				Default = "jank",
				InternalJavaJankToIgnore = true
			}
		});
	}

	public static JavaParser CreateJavaParser(TextReader input)
	{
		var antlrInputStream = new AntlrInputStream(input);
		var javaLexer = new JavaLexer(antlrInputStream);
		var commonTokenStream = new CommonTokenStream(javaLexer);
		return new JavaParser(commonTokenStream);
	}
}