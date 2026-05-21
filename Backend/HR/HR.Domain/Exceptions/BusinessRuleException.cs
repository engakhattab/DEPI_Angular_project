namespace HR.Domain.Exceptions;

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION") : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string message, Exception innerException, string errorCode = "BUSINESS_RULE_VIOLATION")
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
