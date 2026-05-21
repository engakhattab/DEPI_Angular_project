namespace HR.Shared.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public ServiceError? Error { get; }

    private Result(bool isSuccess, T? value, ServiceError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;

        if (isSuccess && value is null)
        {
            throw new InvalidOperationException("A successful result must contain a non-null value.");
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && value is not null)
        {
            throw new InvalidOperationException("A failed result cannot contain a value.");
        }
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(ServiceError error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ServiceError? Error { get; }

    private Result(bool isSuccess, ServiceError? error)
    {
        IsSuccess = isSuccess;
        Error = error;

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must contain an error.");
        }

        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }
    }

    public static Result Success() => new(true, null);

    public static Result Failure(ServiceError error) => new(false, error);
}
