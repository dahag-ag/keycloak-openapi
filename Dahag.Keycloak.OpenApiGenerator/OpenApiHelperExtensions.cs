using Dahag.Keycloak.OpenApiGenerator.Parsing.Resource;
using Dahag.Keycloak.OpenApiGenerator.PostProcess;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using HttpMethod = Dahag.Keycloak.OpenApiGenerator.Parsing.Resource.HttpMethod;

namespace Dahag.Keycloak.OpenApiGenerator;

public static class OpenApiHelperExtensions
{
	public static OperationType ToOpenApi(this HttpMethod httpMethod)
	{
		return httpMethod switch
		{
			HttpMethod.Get => OperationType.Get,
			HttpMethod.Put => OperationType.Put,
			HttpMethod.Post => OperationType.Post,
			HttpMethod.Delete => OperationType.Delete,
			HttpMethod.NoneFound => throw new Exception("Bruh"),
			_ => throw new ArgumentOutOfRangeException(nameof(httpMethod), httpMethod, null)
		};
	}


	public static List<OpenApiParameter> ToOpenApiPathParameters(this IEnumerable<IParameter> parameters)
	{
		return parameters.Where(parameter => parameter.Source == ParamSource.Path).Select(x => x.ToOpenApiPathParameter()).ToList();
	}

	public static OpenApiResponse ToOpenApiResponse(this IAction action)
	{
		if (action.ReturnType == null)
			throw new Exception("Return type is unknown");

		if (action.ReturnType?.Primitive is Primitive.Response or Primitive.Void)
		{
			return new OpenApiResponse
			{
				Description = "Success"
			};
		}

		if (action.Produces.SingleOrDefault(x => x == MediaType.OctetStream) != default)
		{
			return new OpenApiResponse
			{
				Description = "Success",
				Content = new Dictionary<string, OpenApiMediaType>
				{
					{
						MediaType.OctetStream.ToOpenApi(), new OpenApiMediaType
						{
							Schema = new OpenApiSchema
							{
								Type = "string",
								Format = "binary"
							}
						}
					}
				}
			};
		}


		var produces = action.Produces.Select(x => x.ToOpenApi());

		return new OpenApiResponse
		{
			Description = "Success",
			Content = produces.ToDictionary(x => x, _ => action.ReturnType!.ToOpenApiMediaType())
		};
	}

	public static OpenApiParameter ToOpenApiPathParameter(this IParameter param)
	{
		return new OpenApiParameter
		{
			Name = param.Name,
			In = ParameterLocation.Path,
			Description = param.Documentation,
			Required = true,
			Schema = param.TypeInfo.ToOpenApiSchema()
		};
	}

	public static OpenApiRequestBody? ToOpenApiBody(this IEnumerable<IParameter> parameters, List<IAction> actions)
	{
		var allBodyParameters = actions.SelectMany(action => action.Parameters.Where(x => x.Source == ParamSource.Body)).ToList();

		if (!allBodyParameters.Any())
			return null;

		var combinedMediaTypes = actions.SelectMany(x => x.Consumes).ToList();


		if (actions.Count > 1 && combinedMediaTypes.Contains(MediaType.FormUrlEncoded))
		{
			return parameters.ToOpenApiBody(actions.Where(x => x.Consumes.Contains(MediaType.FormUrlEncoded)).ToList());
		}

		if (actions.Count != allBodyParameters.Count)
			throw new Exception("Parameter count should be the same as action count");


		var bodyParameter = parameters.Single(x => x.Source == ParamSource.Body);

		if (!combinedMediaTypes.Any())
		{
			combinedMediaTypes = new List<MediaType>() { MediaType.ApplicationJson };
		}
		
		return new OpenApiRequestBody
		{
			Content = combinedMediaTypes.Select(x => x.ToOpenApi()).ToDictionary(x => x, _ =>
			{
				var openApiMediaType = bodyParameter.TypeInfo.ToOpenApiMediaType();
				return openApiMediaType ?? throw new Exception("null body will cause issues");
			})
		};
	}

	public static string ToOpenApi(this MediaType mediaType)
	{
		return mediaType switch
		{
			MediaType.PlainText => "text/plain",
			MediaType.ApplicationJson => "application/json",
			MediaType.Xml => "application/xml",
			MediaType.FormUrlEncoded => "application/x-www-form-urlencoded",
			MediaType.MultiPartFormData => "multipart/form-data",
			MediaType.OctetStream => "application/octet-stream",
			_ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, null)
		};
	}

	public static List<OpenApiParameter> ToOpenApiQueryParameters(this IEnumerable<IParameter> parameters)
	{
		return parameters.Where(parameter => parameter.Source == ParamSource.Query).Select(x => x.ToOpenApiQueryParameter()).ToList();
	}

	public static OpenApiParameter ToOpenApiQueryParameter(this IParameter param)
	{
		return new OpenApiParameter
		{
			Name = param.Name,
			In = ParameterLocation.Query,
			Description = param.Documentation,
			Schema = param.TypeInfo.ToOpenApiSchema()
		};
	}

	public static OpenApiMediaType ToOpenApiMediaType(this TypeInfo typeInfo)
	{
		return new OpenApiMediaType
		{
			Schema = typeInfo.ToOpenApiSchema()
		};
	}

	public static OpenApiSchema ToOpenApiSchema(this IRepresentation representation)
	{
		
		return new OpenApiSchema
		{
			Type = "object",
			Properties = representation.Properties.ToDictionary(ToLowerPascalCase, property => property.Type.ToOpenApiSchema())
		};
	}

	private static string ToLowerPascalCase(Property property)
	{
		var name = char.ToLower(property.Name[0]) + property.Name[1..];
		return name;
	}

	public static OpenApiSchema ToOpenApiSchema(this TypeInfo typeInfo)
	{
		if (typeInfo.Primitive != null)
		{
			var type = typeInfo.Primitive.Value.ToOpenApiType();

			return new OpenApiSchema
			{
				Type = type.Type,
				Format = type.Format
			};
		}

		if (typeInfo.Complex != null)
		{
			var openApiSchema = new OpenApiSchema
			{
				Reference = new OpenApiReference
				{
					Id = typeInfo.Complex.Name,
					Type = ReferenceType.Schema
				}
			};
			return openApiSchema;
		}

		if (typeInfo.KeycloakEnum != null)
		{
			return new OpenApiSchema
			{
				Type = "string",
				Description = typeInfo.KeycloakEnum.Name,
				Enum = typeInfo.KeycloakEnum.Map.Select(x => (IOpenApiAny)new OpenApiString(x.Value)).ToList()
			};
		}

		if (typeInfo.Collection != null)
		{
			var type = typeInfo.Collection.ToOpenApiType();

			return new OpenApiSchema
			{
				Type = type.Type,
				AdditionalProperties = type.AdditionalProperties,
				Items = type.Items
			};
		}

		if (typeInfo.AmbiguousSource != null)
		{
			return new OpenApiSchema
			{
				Type = "object",
				Description = typeInfo.AmbiguousSource
			};
		}

		throw new Exception("whoops");
	}

	public static (string Type, string? Format) ToOpenApiType(this Primitive primitive)
	{
		return primitive switch
		{
			Primitive.Integer => ("integer", "int32"),
			Primitive.String => ("string", null),
			Primitive.DateTime => ("string", "date-time"),
			Primitive.Float => ("number", "float"),
			Primitive.Boolean => ("boolean", null),
			Primitive.Long => ("integer", "int64"),
			Primitive.Object or Primitive.JsonNode => ("object", null),
			Primitive.Byte => throw new Exception("how to handle byte lol"),
			_ => throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null)
		};
	}

	public static (string Type, OpenApiSchema? AdditionalProperties, OpenApiSchema? Items) ToOpenApiType(this Collection collection)
	{
		return collection.Type switch
		{
			CollectionType.List or CollectionType.Stream or CollectionType.Array or CollectionType.Set => ("array", null, collection.First.ToOpenApiSchema()),
			CollectionType.Map => ("object", collection.Second!.ToOpenApiSchema(), null),
			CollectionType.MultivaluedHashMap => ("object", new TypeInfo(new Collection(CollectionType.List, collection.Second!)).ToOpenApiSchema(), null),
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}