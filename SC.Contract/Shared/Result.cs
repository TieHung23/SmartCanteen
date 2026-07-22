using System.Text.Json.Serialization;

namespace SC.Contract.Shared;

public class Result
{
    protected Result(bool isSuccess, Error error, string? message = "", string? reason = null)
    {
        switch (isSuccess)
        {
            case true when error != Error.None:
                throw new InvalidOperationException("A successful result cannot have an error.");
            case false when error == Error.None:
                throw new InvalidOperationException("A failure result must have an error.");
            default:
                Message = message;
                Reason = reason;
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }

    public string? Message { get; private set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; private set; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; private set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCode => IsFailure ? Error?.Code : null;

    public static Result Success(string? message)
    {
        return new Result(true, Error.None, message);
    }

    public static Result Failure(Error error, string? message)
    {
        return new Result(false, error, message);
    }

    public static Result Failure(Error error, string? message, string? reason)
    {
        return new Result(false, error, message, reason);
    }

    public static Result<TValue> Success<TValue>(TValue value, string? message)
    {
        return new Result<TValue>(true, value, Error.None, message);
    }

    public static Result<TValue> Failure<TValue>(Error error, string? message)
    {
        return new Result<TValue>(false, default, error, message);
    }

    public static Result<TValue> Failure<TValue>(Error error, string? message, string? reason)
    {
        return new Result<TValue>(false, default, error, message, reason);
    }

    protected static Result<TValue> Create<TValue>(TValue? value, string? message)
    {
        return value is null ? Failure<TValue>(Error.NullValue, message) : Success(value, message);
    }
}
