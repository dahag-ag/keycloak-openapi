namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public class TypeInfo
{
	public Primitive? Primitive { get; }
	public IRepresentation? Complex { get; }
	public Collection? Collection { get; }
	public KeycloakEnum? KeycloakEnum { get; }
	public string? AmbiguousSource { get; }

	public TypeInfo(Primitive primitive)
	{
		Primitive = primitive;
	}

	public TypeInfo(IRepresentation complex)
	{
		Complex = complex;
	}

	public TypeInfo(Collection collection)
	{
		Collection = collection;
	}

	public TypeInfo(string ambiguousSource)
	{
		AmbiguousSource = ambiguousSource;
	}

	public TypeInfo(KeycloakEnum keycloakEnum)
	{
		KeycloakEnum = keycloakEnum;
	}

	public static TypeInfo? TryCreate(string input, List<IRepresentation> representations, List<KeycloakEnum> enums)
	{
		var knownComplexTypes = representations.ToList();
		return TryAsPrimitive(input) ?? TryAsEnum(input, enums) ?? TryAsCollection(input, knownComplexTypes, enums) ?? TryAsComplex(input, knownComplexTypes);
	}


	private static TypeInfo? TryAsPrimitive(string type)
	{
		var primitive = ParsePrimitive(type);

		if (primitive != null)
			return new TypeInfo(primitive.Value);

		return null;
	}

	private static Primitive? ParsePrimitive(string type)
	{
		return type switch
		{
			"String" => PostProcess.Primitive.String,
			"int" => PostProcess.Primitive.Integer,
			"Integer" => PostProcess.Primitive.Integer,
			"JsonNode" => PostProcess.Primitive.JsonNode,
			"DateTime" => PostProcess.Primitive.DateTime,
			"boolean" or "Boolean" => PostProcess.Primitive.Boolean,
			"long" => PostProcess.Primitive.Long,
			"Long" => PostProcess.Primitive.Long,
			"Object" or "MultipartFormDataInput" => PostProcess.Primitive.Object,
			"void" => PostProcess.Primitive.Void,
			"Response" => PostProcess.Primitive.Response,
			"byte" => PostProcess.Primitive.Byte,
			_ => null
		};
	}

	private static TypeInfo? TryAsCollection(string type, List<IRepresentation> knownComplexTypes, List<KeycloakEnum> knownEnums)
	{
		var collection = Collection.TryCreate(type, knownComplexTypes.ToList(), knownEnums);

		if (collection != null)
			return new TypeInfo(collection);

		return null;
	}

	private static TypeInfo? TryAsEnum(string type, IEnumerable<KeycloakEnum> knownEnums)
	{
		var matchingEnum = knownEnums.SingleOrDefault(x => x.Name == type);

		if (matchingEnum != null)
			return new TypeInfo(matchingEnum);

		return null;
	}

	private static TypeInfo? TryAsComplex(string type, List<IRepresentation> knownComplexTypes)
	{
		if (IgnoreHelper.IgnoredAmbiguous.Contains(type))
			return new TypeInfo(type);

		var matchingComplex = knownComplexTypes.SingleOrDefault(x => x.Name == type);

		if (matchingComplex != null)
			return new TypeInfo(matchingComplex);

		return null;
	}

	public override string ToString()
	{
		if (Primitive != null)
			return Enum.GetName(Primitive.Value)!;

		if (Collection != null)
			return Collection.ToString()!;

		if (AmbiguousSource != null)
			return AmbiguousSource;

		if (KeycloakEnum != null)
			return KeycloakEnum.Name;

		return Complex!.Name;
	}
}