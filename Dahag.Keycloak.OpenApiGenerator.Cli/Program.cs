// See https://aka.ms/new-console-template for more information

using CommandLine;
using Dahag.Keycloak.OpenApiGenerator;
using Dahag.Keycloak.OpenApiGenerator.Cli;
using Dahag.Keycloak.OpenApiGenerator.PostProcess;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Serilog;
using Serilog.Extensions.Logging;

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.CreateLogger();

try
{
	var result = Parser.Default.ParseArguments<Options>(args);

	result.MapResult(options =>
	{
		if (!Directory.Exists(options.KeycloakRoot))
			throw new Exception($"Keycloak root directory '{options.KeycloakRoot}' could not be found");

		var outputRoot = options.Output ?? Directory.GetCurrentDirectory();
		
		if (!Directory.Exists(outputRoot))
			throw new Exception($"output root directory '{outputRoot}' could not be found");
		
		using var loggerFactory = new SerilogLoggerFactory();
		var repoParser = new KeycloakRepositoryParser(loggerFactory.CreateLogger<KeycloakRepositoryParser>(), options.KeycloakRoot);
		var apiDefinition = repoParser.Parse();
		var postProcessor = new KeycloakRawDefinitionsPostProcess(loggerFactory.CreateLogger<KeycloakRawDefinitionsPostProcess>());
		Log.Information("Starting Post Process");
		var postProcessed = postProcessor.PostProcess(apiDefinition);
		Log.Information("Generating OpenApi definition");
		var openApiDocument = new KeycloakOpenApiGenerator().Generate(postProcessed.ActionCollections, postProcessed.Representations);

		var definitionsYmlPath = Path.Combine(Directory.GetCurrentDirectory(), "definition.yml");
		Log.Information($"Writing: {definitionsYmlPath}");
		using (var targetFile = File.Open(definitionsYmlPath, FileMode.Create))
		{
			openApiDocument.SerializeAsYaml(targetFile, OpenApiSpecVersion.OpenApi3_0);
		}
		var definitionsJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "definition.json");
		Log.Information($"Writing: {definitionsJsonPath}");
		using (var targetFile = File.Open(definitionsJsonPath, FileMode.Create))
		{
			openApiDocument.SerializeAsJson(targetFile, OpenApiSpecVersion.OpenApi3_0);
		}

		Log.Information("Done.");

		return 0;
	}, errorCase =>
	{
		Log.Error(errorCase.First().ToString());
		return 2;
	});

	
}
catch (Exception e)
{
	Log.Fatal(e, "Crashy crash");
	return 1;
}
finally
{
	Log.CloseAndFlush();
}

return 0;