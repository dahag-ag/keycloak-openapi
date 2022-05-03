using System.Text.RegularExpressions;

namespace Dahag.Keycloak.OpenApiGenerator.PostProcess;

public class Collection
{
	public CollectionType Type { get; }
	public TypeInfo First { get; }
	public TypeInfo? Second { get; }

	public Collection(CollectionType type, TypeInfo first, TypeInfo? second = null)
	{
		Type = type;
		First = first;
		Second = second;
	}

	public static Collection? TryCreate(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		return AsList(rawType, representations, keycloakEnums) ?? AsStream(rawType, representations, keycloakEnums) ??
			AsSet(rawType, representations, keycloakEnums) ??
			AsMultiValuedHashMapped(rawType, representations, keycloakEnums) ??
			AsMapped(rawType, representations, keycloakEnums) ?? AsArray(rawType, representations, keycloakEnums);
	}

	private static Collection? AsList(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var listRegex = new Regex("^(?:(?:List)|(?:Collection))<(?<inner>.+)>");
		var listHit = listRegex.Match(rawType);

		if (!listHit.Success)
			return null;

		var innerHit = TypeInfo.TryCreate(listHit.Groups["inner"].Value, representations, keycloakEnums);

		if (innerHit == null)
			return null;

		return new Collection(CollectionType.List, innerHit);
	}

	private static Collection? AsStream(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var regex = new Regex("^Stream<(?<inner>.+)>");
		var hit = regex.Match(rawType);

		if (!hit.Success)
			return null;

		var innerHit = TypeInfo.TryCreate(hit.Groups["inner"].Value, representations, keycloakEnums);

		if (innerHit == null)
			return null;

		return new Collection(CollectionType.Stream, innerHit);
	}

	private static Collection? AsSet(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var regex = new Regex("^Set<(?<inner>.+)>");
		var hit = regex.Match(rawType);

		if (!hit.Success)
			return null;

		var innerHit = TypeInfo.TryCreate(hit.Groups["inner"].Value, representations, keycloakEnums);

		if (innerHit == null)
			return null;

		return new Collection(CollectionType.Set, innerHit);
	}

	private static Collection? AsMapped(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var mapRegex = new Regex("^Map<(?<key>\\w+), ?(?<value>.+)>");
		var mapHit = mapRegex.Match(rawType);

		if (!mapHit.Success)
			return null;

		var keyHit = TypeInfo.TryCreate(mapHit.Groups["key"].Value, representations, keycloakEnums);
		if (keyHit == null)
			return null;

		var valueHit = TypeInfo.TryCreate(mapHit.Groups["value"].Value, representations, keycloakEnums);
		if (valueHit == null)
			return null;

		return new Collection(CollectionType.Map, keyHit, valueHit);
	}

	private static Collection? AsMultiValuedHashMapped(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var mapRegex = new Regex("^MultivaluedHashMap<(?<key>\\w+), ?(?<value>.+)>");
		var mapHit = mapRegex.Match(rawType);

		if (!mapHit.Success)
			return null;

		var keyHit = TypeInfo.TryCreate(mapHit.Groups["key"].Value, representations, keycloakEnums);
		if (keyHit == null)
			return null;

		var valueHit = TypeInfo.TryCreate(mapHit.Groups["value"].Value, representations, keycloakEnums);
		if (valueHit == null)
			return null;

		return new Collection(CollectionType.MultivaluedHashMap, keyHit, valueHit);
	}

	private static Collection? AsArray(string rawType, List<IRepresentation> representations, List<KeycloakEnum> keycloakEnums)
	{
		var arrayRegex = new Regex(@"^(?<type>.+)\[\]$");
		var arrayHit = arrayRegex.Match(rawType);

		if (!arrayHit.Success)
			return null;

		var typeHit = TypeInfo.TryCreate(arrayHit.Groups["type"].Value, representations, keycloakEnums);
		if (typeHit == null)
			return null;

		return new Collection(CollectionType.Array, typeHit);
	}

	public override string ToString()
	{
		var secondParam = Second != null ? $", {Second}" : "";
		return $"{Enum.GetName(Type)}<{First}{secondParam}>";
	}
}