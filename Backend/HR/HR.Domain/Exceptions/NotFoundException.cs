namespace HR.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public static NotFoundException EntityNotFound(string entityType, object identifier) =>
        new($"{entityType} with identifier '{identifier}' was not found.");
}
