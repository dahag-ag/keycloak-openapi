using Dahag.Keycloak.OpenApiGenerator.Parsing;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using Dahag.Keycloak.OpenApiGenerator.PostProcess;
using Microsoft.Extensions.Logging;

namespace Dahag.Keycloak.OpenApiGenerator;

public class KeycloakRepositoryParser
{
	private readonly ILogger<KeycloakRepositoryParser> _logger;
	private readonly string _root;
	private const string RelativeRepresentationRootPath = "core/src/main/java/org/keycloak/representations/idm";
	private const string RelativeRepresentationAuthorizationRootPath = "core/src/main/java/org/keycloak/representations/idm/authorization";
	private const string RelativeRepresentationMiscRootPath = "core/src/main/java/org/keycloak/representations";
	private const string RelativeAdminResourceRootPath = "services/src/main/java/org/keycloak/services/resources/admin";

	public KeycloakRepositoryParser(ILogger<KeycloakRepositoryParser> logger, string root)
	{
		_logger = logger;
		_root = root;
	}

	public RawKeycloakApiDefinition Parse()
	{
		var idmRepresentationRoot = Path.Combine(_root, RelativeRepresentationRootPath);
		var authorizationRepresentationRoot = Path.Combine(_root, RelativeRepresentationAuthorizationRootPath);
		var miscRepresentationRoot = Path.Combine(_root, RelativeRepresentationMiscRootPath);
		var adminResourceRoot = Path.Combine(_root, RelativeAdminResourceRootPath);

		var idmRepresentations = ParseRepresentations(idmRepresentationRoot,
			x => x.EndsWith("presentation.java", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith("Reference.java")).ToList();
		var miscRepresentations = ParseRepresentations(miscRepresentationRoot, s => true).ToList();
		var manualRepresentations = ParseManuallyIncludedRepresentations(_root).ToList();

		idmRepresentations = idmRepresentations.Concat(miscRepresentations).Concat(manualRepresentations).ToList();

		var authorizationRepresentations = ParseRepresentations(authorizationRepresentationRoot,
			x => x.EndsWith("presentation.java", StringComparison.InvariantCultureIgnoreCase) || x.EndsWith("Reference.java")).ToList();
		var resources = ParseResources(adminResourceRoot, x => x.EndsWith("Resource.java"))
			.ToList();
		var enums = ParseEnums(idmRepresentationRoot).Concat(ParseEnums(authorizationRepresentationRoot)).Concat(ParseManualEnums()).ToList();

		var ambiguousResources = new List<RawRepresentation>();

		foreach (var authorizationRepresentation in authorizationRepresentations)
		{
			if (idmRepresentations.Any(x => x.Name == authorizationRepresentation.Name))
				ambiguousResources.Add(authorizationRepresentation);

			idmRepresentations.Add(authorizationRepresentation);
		}

		if (ambiguousResources.Count > 1 || ambiguousResources[0].Name != "ClientPolicyRepresentation")
			throw new Exception("Not the expected ambiguous class");

		IgnoreHelper.IgnoredAmbiguous.AddRange(ambiguousResources.Select(x => x.Name));

		return new RawKeycloakApiDefinition(idmRepresentations, resources, enums.ToList());
	}

	private IEnumerable<RawRxJsResource> ParseResources(string resourceRoot, Func<string, bool> filter)
	{
		var allResourceFilePaths = Directory.GetFiles(resourceRoot).Where(filter).ToList();

		_logger.LogInformation("Found {amount} resource files", allResourceFilePaths.Count);

		return allResourceFilePaths.Select(ParseResourceFile);
	}

	private RawRxJsResource ParseResourceFile(string resourceFilePath)
	{
		_logger.LogInformation("Handling {file}", Path.GetFileName(resourceFilePath));
		var content = File.ReadAllText(resourceFilePath);
		return KeycloakFileParser.ParseResource(content);
	}

	private string[] _manualEnums =
	{
		"core/src/main/java/org/keycloak/TokenCategory.java"
	};

	private IEnumerable<KeycloakEnum> ParseManualEnums()
	{
		var allRepresentationFilePaths = _manualEnums.Select(x => Path.Combine(_root, x)).ToList();

		_logger.LogInformation("Found {amount} enum files", allRepresentationFilePaths.Count);

		return allRepresentationFilePaths.SelectMany(ParseEnumFile);
	}

	private IEnumerable<KeycloakEnum> ParseEnums(string idmRepresentationRootPath)
	{
		var allRepresentationFilePaths = Directory.GetFiles(idmRepresentationRootPath)
			.Where(x => !x.EndsWith("Representation.java")).ToList();

		_logger.LogInformation("Found {amount} enum files", allRepresentationFilePaths.Count);

		return allRepresentationFilePaths.SelectMany(ParseEnumFile);
	}

	private IEnumerable<KeycloakEnum> ParseEnumFile(string enumFilePath)
	{
		_logger.LogInformation("Handling {file}", Path.GetFileName(enumFilePath));
		var content = File.ReadAllText(enumFilePath);
		var keycloakEnum = KeycloakFileParser.ParseEnum(content);

		if (keycloakEnum == null)
			yield break;

		yield return keycloakEnum;
	}

	private IEnumerable<RawRepresentation> ParseRepresentations(string idmRepresentationRootPath, Func<string, bool> filter)
	{
		var allRepresentationFilePaths = Directory.GetFiles(idmRepresentationRootPath)
			.Where(filter).ToList();

		_logger.LogInformation("Found {amount} representation files", allRepresentationFilePaths.Count);
		return allRepresentationFilePaths.Select(ParseRepresentationFile).SelectMany(representations => representations);
	}


	private readonly string[] _extraRepresentationFiles =
	{
		"server-spi/src/main/java/org/keycloak/storage/user/SynchronizationResult.java",
		"services/src/main/java/org/keycloak/services/resources/admin/ClientScopeEvaluateResource.java",
		"core/src/main/java/org/keycloak/representations/idm/authorization/Permission.java"
	};

	private IEnumerable<RawRepresentation> ParseManuallyIncludedRepresentations(string root)
	{
		return _extraRepresentationFiles.Select(extraFile => Path.Combine(root, extraFile)).SelectMany(ParseRepresentationFile)
			.Where(x => !x.Name.EndsWith("Resource"));
	}

	private List<RawRepresentation> ParseRepresentationFile(string representationFilePath)
	{
		_logger.LogInformation("Handling {file}", Path.GetFileName(representationFilePath));
		var content = File.ReadAllText(representationFilePath);
		var representations = KeycloakFileParser.ParseRepresentation(content);
		return representations;
	}
}

public static class IgnoreHelper
{
	public static readonly List<string> IgnoredAmbiguous = new()
	{
		"ErrorRepresentation",
		"KeyUse",
		"PublicKey",
		"Policy",
		"CapabilityType",
		"GlobalRequestResult",
		"ClaimValue<CLAIM_TYPE>",
		"ClaimValue"
	};

	public static readonly Func<RawRxJsResourceAction, bool>[] DisregardAsParentActionFuncs =
	{
		action => action.ReturnsType == "AuthorizationService",
		action => action.Tag == "RealmAdminResource" && action.ReturnsType == "Object",
	};
}