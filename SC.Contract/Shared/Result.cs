namespace SC.Contract.Shared;

public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; private set; }

    protected Result(bool isSuccess, Error error)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot have an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failure result must have an error.");
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    public static Result Success() => new(true, Error.None);
    
    public static Result Failure(Error error) => new(false, error);
    
    public static Result<TValue> Success<TValue>(TValue value) => new(true, value, Error.None);
    
    public static Result<TValue> Failure<TValue>(Error error) => new(false, default(TValue), error);
    
    public static Result<TValue> Create<TValue>(TValue? value) => value is null ? Failure<TValue>(Error.NullValue) : Success(value);
}