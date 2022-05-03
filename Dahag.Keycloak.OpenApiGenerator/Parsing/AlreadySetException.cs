namespace Dahag.Keycloak.OpenApiGenerator.Parsing;

public class AlreadySetException : Exception
{
	public AlreadySetException(string propertyName) : base(
		$"Attempted to set '{propertyName}' even though it was already set. That means that the Visitor logic is not detecting the end of a RxJs resource properly and the next resource bled over.")
	{
	}
}