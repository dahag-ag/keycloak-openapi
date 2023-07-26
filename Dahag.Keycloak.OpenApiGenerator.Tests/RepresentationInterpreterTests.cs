using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Dahag.Keycloak.OpenApiGenerator.Parsing;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;
using NUnit.Framework;

namespace Dahag.Keycloak.OpenApiGenerator.Tests;

public class RepresentationInterpreterTests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	public void Interpret_SimpleRepresentation_Works()
	{
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.SimpleRepresentation.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new RepresentationInterpreter();
		var actuals = interpreter.Visit(parser.compilationUnit());
		Assert.That(actuals, Has.Count.EqualTo(1));
		var actual = actuals[0];

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Name, Is.EqualTo("ClientRepresentation"));
		Assert.That(actual.Properties, Has.Count.EqualTo(2));
		Assert.That(actual.Properties[0].Name, Is.EqualTo("Id"));
		Assert.That(actual.Properties[0].Type, Is.EqualTo("String"));
		Assert.That(actual.Properties[1].Name, Is.EqualTo("Enabled"));
		Assert.That(actual.Properties[1].Type, Is.EqualTo("Boolean"));
	}
	
	[Test]
	public void Interpret_WithIgnored_IgnoresIgnored()	
	{
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.RepresentationWithIgnored.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new RepresentationInterpreter();
		var actuals = interpreter.Visit(parser.compilationUnit());
		Assert.That(actuals, Has.Count.EqualTo(1));
		var actual = actuals[0];

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Name, Is.EqualTo("ClientRepresentation"));
		Assert.That(actual.Properties, Has.Count.EqualTo(1));
		Assert.That(actual.Properties[0].Name, Is.EqualTo("Enabled"));
		Assert.That(actual.Properties[0].Type, Is.EqualTo("Boolean"));
	}
	
	[Test]
	public void Interpret_NestedRepresentation_Works()
	{
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.RepresentationWithNestedType.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new RepresentationInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());

		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Count, Is.EqualTo(2));
		Assert.That(actual[0].Name, Is.EqualTo("ClientRepresentation"));
		Assert.That(actual[0].Properties, Has.Count.EqualTo(2));
		Assert.That(actual[0].Properties[0].Name, Is.EqualTo("Id"));
		Assert.That(actual[0].Properties[0].Type, Is.EqualTo("String"));
		Assert.That(actual[0].Properties[1].Name, Is.EqualTo("Enabled"));
		Assert.That(actual[0].Properties[1].Type, Is.EqualTo("Boolean"));
		Assert.That(actual[1].Name, Is.EqualTo("Composites"));
		Assert.That(actual[1].Properties, Has.Count.EqualTo(2));
		Assert.That(actual[1].Properties[0].Name, Is.EqualTo("IdNested"));
		Assert.That(actual[1].Properties[0].Type, Is.EqualTo("String"));
		Assert.That(actual[1].Properties[1].Name, Is.EqualTo("EnabledNested"));
		Assert.That(actual[1].Properties[1].Type, Is.EqualTo("Boolean"));
	}


	[Test]
	public void Interpret_Enum_Works()
	{
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.Enum.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new EnumInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());
		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Map, Has.Count.EqualTo(2));

		Assert.That(actual.Map[0], Is.EqualTo("POSITIVE"));
		Assert.That(actual.Map[1], Is.EqualTo("NEGATIVE"));
	}

	[Test]
	public void Interpret_EnumWithStableIndex_Works()
	{
		using var manifestResourceStream = typeof(Tests).Assembly.GetManifestResourceStream($"Dahag.Keycloak.OpenApiGenerator.Tests.TestFiles.EnumStableIndex.java");
		using var textReader = new StreamReader(manifestResourceStream!, Encoding.UTF8);
		var parser = CreateJavaParser(textReader);

		var interpreter = new EnumInterpreter();
		var actual = interpreter.Visit(parser.compilationUnit());
		Assert.That(actual, Is.Not.Null);
		Assert.That(actual.Map, Has.Count.EqualTo(3));

		Assert.That(actual.Map[0], Is.EqualTo("ENFORCING"));
		Assert.That(actual.Map[1], Is.EqualTo("PERMISSIVE"));
		Assert.That(actual.Map[2], Is.EqualTo("DISABLED"));
	}
	
	public static JavaParser CreateJavaParser(TextReader input)
	{
		var antlrInputStream = new AntlrInputStream(input);
		var javaLexer = new JavaLexer(antlrInputStream);
		var commonTokenStream = new CommonTokenStream(javaLexer);
		return new JavaParser(commonTokenStream);
	}
}