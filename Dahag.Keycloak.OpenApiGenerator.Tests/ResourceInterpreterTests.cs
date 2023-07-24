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
	[TestCase("TwoSimpleEndpointsResource.java")]
	[TestCase("TwoSimpleEndpointsResourceShuffledAttributes.java")]
	[TestCase("TwoSimpleEndpointsResourceWithRandomPrivateMethods.java")]
	public void Interpret_TwoSimpleEndpoints_TwoResources(string testFile)
	{
		var actual = ParseResource(testFile);

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
		var actual = ParseResource("DifferentConsumeAndProduceTypesEndpointsResource.java");

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
		var actual = ParseResource("ImplicitPathResource.java");

		var expected = new List<RawRxJsResourceAction>
		{
			new()
			{
				Path = null,
				ImplicitPath = "clients",
				HttpMethod = HttpMethod.Get,
				ReturnsType = "Stream<ClientRepresentation>"
			}
		};

		Assert.That(actual.Actions, Has.Count.EqualTo(1));
		actual.Actions.AssertEquality(expected);
	}

	[Test]
	public void Interpret_Parent_Works()
	{
		var actual = ParseResource("ParentResource.java");

		Assert.That(actual.Actions, Has.Count.EqualTo(1));
		Assert.That(actual.Actions[0].Path, Is.EqualTo("{id}"));
		Assert.That(actual.Actions[0].HttpMethod, Is.EqualTo(HttpMethod.NoneFound));
		Assert.That(actual.Actions[0].ReturnsType, Is.EqualTo("ClientResource"));
		Assert.That(actual.Actions[0].ProbablyParentOfAnotherResource, Is.EqualTo(true));
	}

	[Test]
	public void Interpret_Params_Works()
	{
		var actual = ParseResource("SimpleParamsResource.java");

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
		var actual = ParseResource("AdvancedParamsResource.java");

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
	
		
	[Test]
	public void Interpret_ImplicitBodyParams_Works()
	{
		var actual = ParseResource("ImplicitParamsResource.java");

		Assert.That(actual.Actions, Has.Count.EqualTo(1));
		actual.Actions[0].Parameters.AssertEquality(new List<RawRxjsParam>
		{
			new()
			{
				Name = "data",
				ParamSource = ParamSource.Body,
				Type = "Object",
				PathParam = null,
				Default = null,
				Implicit = true
			}
		});
	}


	[Test]
	public void Interpret_InlineClassCreationInMethodBody_CorrectParams()
	{
		var actual = ParseResource("ResourceWithInlineClassCreationAndMethods.java");

		Assert.That(actual.Actions, Has.Count.EqualTo(3));
		actual.Actions[0].AssertEquality(new RawRxJsResourceAction
		{
			Path = "partialImport",
			HttpMethod = HttpMethod.Post,
			Consumes = new List<MediaType> { MediaType.ApplicationJson },
			ReturnsType = "Response",
			Parameters = new List<RawRxjsParam>()
			{
				new()
				{
					Name = "rep",
					Type = "PartialImportRepresentation",
					ParamSource = ParamSource.Body
				}
			}
		});
		actual.Actions[1].AssertEquality(new RawRxJsResourceAction
		{
			Path = "partial-export",
			HttpMethod = HttpMethod.Post,
			Consumes = new List<MediaType> { MediaType.ApplicationJson },
			ReturnsType = "RealmRepresentation",
			Parameters = new List<RawRxjsParam>()
			{
				new ()
				{
					Name = "exportGroupsAndRoles",
					ParamSource = ParamSource.Query,
					PathParam = "exportGroupsAndRoles",
					Type = "Boolean"
				},
				new ()
				{
					Name = "exportClients",
					ParamSource = ParamSource.Query,
					PathParam = "exportClients",
					Type = "Boolean"
				}
			}
		});
		actual.Actions[2].AssertEquality(new RawRxJsResourceAction
		{
			Path = "keys",
			HttpMethod = HttpMethod.NoneFound,
			Consumes = new List<MediaType>(),
			ReturnsType = "KeyResource"
		});
	}

	[Test]
	[TestCase("ResourceWithoutInlineClassCreation.java", "ResourceWithInlineClassCreation.java")]
	[TestCase("ResourceWithoutInlineClassCreationAndMethods.java", "ResourceWithInlineClassCreationAndMethods.java")]
	public void Interpret_InlineClassCreationVsNonInlineClassCreation_SameResult(string without, string with)
	{
		var expected = ParseResource(without);
		var actual = ParseResource(with);

		actual.Actions.AssertEquality(expected.Actions);
	}
	
	[Test]
	public void Interpret_OpenApiAnnotations_DoesNotCrash()
	{
		ParseResource("ResourceWithOpenApiAnnotations.java");
	}

	private static RawRxJsResource ParseResource(string testFileName)
	{
		using var manifestResourceStream =
			typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.{testFileName}");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);
		var interpreter = new ResourceInterpreter();
		var rawRxJsResource = interpreter.Visit(parser.compilationUnit());

		Assert.That(rawRxJsResource, Is.Not.Null);

		return rawRxJsResource;
	}

	private static JavaParser CreateJavaParser(TextReader input)
	{
		var antlrInputStream = new AntlrInputStream(input);
		var javaLexer = new JavaLexer(antlrInputStream);
		var commonTokenStream = new CommonTokenStream(javaLexer);
		return new JavaParser(commonTokenStream);
	}
}