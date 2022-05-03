using Dahag.Keycloak.OpenApiGenerator.PostProcess;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Validations;

namespace Dahag.Keycloak.OpenApiGenerator;

public class KeycloakOpenApiGenerator
{
	public OpenApiDocument Generate(IEnumerable<IActionCollection> actionCollections, IEnumerable<IRepresentation> representations)
	{
		var document = new OpenApiDocument
		{
			Paths = CreatePaths(actionCollections),
			Components = CreateComponents(representations),
			Info = new OpenApiInfo
			{
				Title = "Keycloak REST Api",
				Description = "This is a REST API reference for the Keycloak Admin",
				Version = "1"
			},
			SecurityRequirements = new List<OpenApiSecurityRequirement>
			{
				new()
				{
					{new OpenApiSecurityScheme()
					{
						Reference = new OpenApiReference()
						{
							Type = ReferenceType.SecurityScheme,
							Id = "access_token"
						},
					}, new List<string>()}
				}
			}
		};

		var openApiErrors = document.Validate(ValidationRuleSet.GetDefaultRuleSet()).ToList();

		if (openApiErrors.Any())
		{
			throw new Exception("Api definition contains errors");
		}

		return document;
	}

	private static OpenApiComponents CreateComponents(IEnumerable<IRepresentation> representations)
	{
		var withoutIgnored = representations.Where(x => !IgnoreHelper.IgnoredAmbiguous.Contains(x.Name)).ToList();
		var duplicateRepresentations = withoutIgnored.GroupBy(x => x.Name).Where(x => x.Count() > 1);

		if (duplicateRepresentations.Any())
			throw new Exception("duplicate representations");

		return new OpenApiComponents
		{
			Schemas = withoutIgnored.ToDictionary(representation => representation.Name, representation => representation.ToOpenApiSchema()),
			SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>()
			{
				{
					"access_token", new OpenApiSecurityScheme
					{
						In = ParameterLocation.Header,
						Name = "access_token",
						Type = SecuritySchemeType.Http,
						Scheme = "bearer",
						BearerFormat = "JWT"
					}
				}
			}
		};
	}

	private OpenApiPaths CreatePaths(IEnumerable<IActionCollection> actionCollections)
	{
		var topLevelCollection = actionCollections.Single(x => x.Name == "RealmsAdminResource");
		var groupedByPath = topLevelCollection.Actions.OrderBy(x => x.Route).GroupBy(x => x.Route ?? "/").ToList();
		var paths = new OpenApiPaths();

		foreach (var pathGroup in groupedByPath)
		{
			var pathItem = CreateOpenApoCreateOpenApiPathItem(pathGroup.ToList());
			paths.Add(pathGroup.Key, pathItem);
		}

		return paths;
	}


	private OpenApiPathItem CreateOpenApoCreateOpenApiPathItem(List<IAction> samePathActions)
	{
		var anyOfThem = samePathActions.First();
		var groupedByMethod = samePathActions.GroupBy(x => x.HttpMethod);
		return new OpenApiPathItem
		{
			Description = anyOfThem.Route,
			Parameters = anyOfThem.Parameters.ToOpenApiPathParameters(),
			Operations = groupedByMethod.ToDictionary(
				methodGroup => methodGroup.Key.ToOpenApi(),
				methodGroup => CreateOperation(methodGroup.ToList()))
		};
	}

	private OpenApiOperation CreateOperation(List<IAction> actions)
	{
		var action = actions.First();

		return new OpenApiOperation
		{
			Tags = new List<OpenApiTag>()
			{
				new()
				{
					Name = action.Tag?.Replace("Resource", "") ?? throw new Exception("No Tag")
				}	
			},
			Description = action.Documentation,
			Parameters = action.Parameters.ToOpenApiQueryParameters(),
			RequestBody = action.Parameters.ToOpenApiBody(actions),
			Responses = new OpenApiResponses
			{
				{ "2XX", action.ToOpenApiResponse() }
			}
		};
	}
}