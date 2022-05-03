using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Dahag.Keycloak.OpenApiGenerator.Parsing;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Documentation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using NUnit.Framework;

namespace Dahag.Keycloak.OpenApiGenerator.Tests;

public class DocumentationFinderTests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	public void FindComments_ThreeDocumentedBlocks_Works()
	{
		const string testFile = "Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.DocumentedResource.java";
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream(testFile);
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);

		var fullTestFileText = textReader.ReadToEnd();
		manifestResourceStream!.Position = 0;
			
		var parser = CreateJavaParser(textReader);

		var interpreter = new ResourceInterpreter();
		var resource = interpreter.Visit(parser.compilationUnit());

		Assert.That(resource, Is.Not.Null);
		Assert.That(resource.Actions, Has.Count.EqualTo(4));

		var documentationFinder = new DocumentationFinder();
		var documentations = resource.Actions.Select(x => documentationFinder.Find(x, fullTestFileText)).Where(x => x != null).ToList(); 
		
		Assert.That(documentations, Has.Count.EqualTo(4));
		Assert.That(documentations[0]!.Text, Contains.Substring("Returns a list of user sessions associated with this client"));
		Assert.That(documentations[0]!.ParamText, Has.Count.EqualTo(3));
		Assert.That(documentations[1]!.Text, Contains.Substring("Get user sessions for clientB"));
		Assert.That(documentations[1]!.ParamText, Has.Count.EqualTo(4));
		Assert.That(documentations[2]!.Text, Contains.Substring("Get user sessions for clientC"));
		Assert.That(documentations[2]!.ParamText, Has.Count.EqualTo(3));
		Assert.That(documentations[3]!.Text, Contains.Substring("Get user sessions for clientD"));
	}

	public static JavaParser CreateJavaParser(TextReader input)
	{
		var antlrInputStream = new AntlrInputStream(input);
		var javaLexer = new JavaLexer(antlrInputStream);
		var commonTokenStream = new CommonTokenStream(javaLexer);
		return new JavaParser(commonTokenStream);
	}
}