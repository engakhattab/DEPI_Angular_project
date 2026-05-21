namespace HR.Domain.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public static EntityStateConflict DuplicateEntity(string entityType, string field, object value) =>
        new($"A {entityType} with {field} '{value}' already exists.");

    public class EntityStateConflict : ConflictException
    {
        public EntityStateConflict(string message) : base(message)
        {
        }
    }
}
