namespace SC.Contract.Shared;

public class Result
{
    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; private set; }

    protected Result(bool isSuccess, Error error, string? message = "")
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot have an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failure result must have an error.");
            default:
                Message = message;
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    public static Result Success(string? message) => new(true, Error.None, message);
    
    public static Result Failure(Error error, string? message) => new(false, error, message);
    
    public static Result<TValue> Success<TValue>(TValue value, string? message) => new(true, value, Error.None, message);
    
    public static Result<TValue> Failure<TValue>(Error error,string? message) => new(false, default(TValue), error, message);

    protected static Result<TValue> Create<TValue>(TValue? value, string? message) => value is null ? Failure<TValue>(Error.NullValue, message) : Success(value, message);
}