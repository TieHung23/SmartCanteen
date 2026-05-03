namespace SC.Contract.Shared;

public class Result<TValue> : Result
{
    protected internal Result(bool isSuccess, TValue? value, Error error, string? message) : base(isSuccess, error,
        message)
    {
        Value = value;
    }

    public TValue? Value { get; private set; }
}