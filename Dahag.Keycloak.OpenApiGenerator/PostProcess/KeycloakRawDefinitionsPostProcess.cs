using Dahag.Keycloak.OpenApiGenerator.Parsing;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Representation;
using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using Microsoft.Extensions.Logging;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public class KeycloakRawDefinitionsPostProcess
{
	private readonly ILogger<KeycloakRawDefinitionsPostProcess> _logger;

	public KeycloakRawDefinitionsPostProcess(ILogger<KeycloakRawDefinitionsPostProcess> logger)
	{
		_logger = logger;
	}

	public (IEnumerable<IActionCollection> ActionCollections, IEnumerable<IRepresentation> Representations) PostProcess(RawKeycloakApiDefinition rawKeycloakApiDefinition)
	{
		var complexTypes = PostProcess(rawKeycloakApiDefinition.Representations, rawKeycloakApiDefinition.Enums);
		var actionCollections = PostProcessResources(rawKeycloakApiDefinition.Resources, complexTypes, rawKeycloakApiDefinition.Enums);

		return (actionCollections, complexTypes);
	}

	private IEnumerable<IActionCollection> PostProcessResources(List<RawRxJsResource> rawResources, List<IRepresentation> complexTypes,
		List<KeycloakEnum> keycloakEnums)
	{
		var processed = new List<IActionCollection>();

		var pending = new List<RawRxJsResource>(rawResources);

		while (pending.Any())
		{
			var pendingCountBefore = pending.Count;
			var currentRun = new List<RawRxJsResource>(pending);
			foreach (var rawRepresentation in currentRun)
			{
				var resourceProcessed = ActionCollection.Create(rawRepresentation, processed, complexTypes, keycloakEnums);

				if (resourceProcessed == null)
					continue;

				processed.Add(resourceProcessed);
				_logger.LogInformation("Post processed {resource}", resourceProcessed.Name);
				pending.Remove(rawRepresentation);
			}

			var pendingCount = pending.Count;
			if (pendingCountBefore == pendingCount)
			{
				//return processed;
				throw new Exception("Could not process more");
			}
		}

		return processed;
	}

	private List<IRepresentation> PostProcess(IEnumerable<RawRepresentation> rawRepresentations, List<KeycloakEnum> enums)
	{
		var normalProcessed = PostProcessNormal(rawRepresentations, enums);

		if (!normalProcessed.Impossible.Any())
			return normalProcessed.Processed;

		if (normalProcessed.Impossible.Count > 14)
			throw new Exception("More than the expected non working models");

		_logger.LogWarning("Couldn't do a safe interpretation of {notWorkedCount}. Forcing the creation :>", normalProcessed.Impossible.Count);

		var forcedResult = ForcePostProcess(normalProcessed.Impossible, enums, normalProcessed.Processed);

		var forceCreationFailedFor = forcedResult.Where(baseForce =>
			baseForce.Properties.Any(property => property.Type.Complex is TemporaryForceCreatedStandInRepresentation
			{
				RealRepresentationImplementation: TemporaryForceCreatedStandInRepresentation
			})).ToList();

		if (forceCreationFailedFor.Any())
			throw new Exception("Failed to force create some :<");

		return forcedResult;
	}

	private (List<IRepresentation> Processed, List<RawRepresentation> Impossible) PostProcessNormal(IEnumerable<RawRepresentation> rawRepresentations,
		List<KeycloakEnum> enums)
	{
		var processed = new List<IRepresentation>();
		var pending = new List<RawRepresentation>(rawRepresentations);

		while (pending.Any())
		{
			var pendingCountBefore = pending.Count;
			var currentRun = new List<RawRepresentation>(pending);
			var lastRunResult = new List<(RawRepresentation Representation, List<RawProperty> UnprocessableProperties)>();
			foreach (var rawRepresentation in currentRun)
			{
				//Used as a trick to handle self referencing models :>
				var selfTempFake = new TemporaryForceCreatedStandInRepresentation()
				{
					Name = rawRepresentation.Name
				};

				var tempKnownWithFakeSelf = processed.Concat(new[] { selfTempFake }).ToList();

				var representationProcessed = Representation.TryCreate(rawRepresentation, tempKnownWithFakeSelf, enums);
				lastRunResult.Add((rawRepresentation, representationProcessed.UnprocessableProperties));

				if (representationProcessed.Representation == null)
					continue;

				selfTempFake.RealRepresentationImplementation = representationProcessed.Representation;

				processed.Add(representationProcessed.Representation);
				_logger.LogInformation("Post processed {representation}", representationProcessed.Representation.Name);
				pending.Remove(rawRepresentation);
			}

			var pendingCount = pending.Count;
			if (pendingCountBefore == pendingCount)
			{
				return (processed, lastRunResult.Select(x => x.Representation).ToList());
			}
		}

		return (processed, new List<RawRepresentation>());
	}

	private List<IRepresentation> ForcePostProcess(List<RawRepresentation> rawRepresentations, List<KeycloakEnum> enums, List<IRepresentation> normalProcessed)
	{
		var processed = new List<IRepresentation>(normalProcessed);
		var withoutIgnored = rawRepresentations.Where(x => !IgnoreHelper.IgnoredAmbiguous.Contains(x.Name)).ToList();
		
		var forceCreatedStandIns = withoutIgnored.Select(x => new TemporaryForceCreatedStandInRepresentation()
		{
			Name = x.Name
		}).ToList();

		processed = processed.Concat(forceCreatedStandIns).ToList();

		var pending = new List<RawRepresentation>(withoutIgnored);

		while (pending.Any())
		{
			var pendingCountBefore = pending.Count;
			var currentRun = new List<RawRepresentation>(pending);
			var lastRunResult = new List<(RawRepresentation Representation, List<RawProperty> UnprocessableProperties)>();
			foreach (var rawRepresentation in currentRun)
			{
				var representationProcessed = Representation.TryCreate(rawRepresentation, processed, enums);
				lastRunResult.Add((rawRepresentation, representationProcessed.UnprocessableProperties));

				if (representationProcessed.Representation == null)
					continue;

				var matchingStandIn = forceCreatedStandIns.Single(x => x.Name == rawRepresentation.Name);
				matchingStandIn.RealRepresentationImplementation = representationProcessed.Representation;

				processed.Remove(matchingStandIn);
				processed.Add(representationProcessed.Representation);

				_logger.LogInformation("Force Post processed {representation}", representationProcessed.Representation.Name);
				pending.Remove(rawRepresentation);
			}

			var pendingCount = pending.Count;
			if (pendingCountBefore == pendingCount)
			{
				throw new Exception($"Pending count '{pendingCount}' is still the same as before '{pendingCountBefore}'");
			}
		}

		return processed;
	}
}