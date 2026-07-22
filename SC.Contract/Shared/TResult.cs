namespace SC.Contract.Shared;

public class Result<TValue> : Result
{
    protected internal Result(bool isSuccess, TValue? value, Error error, string? message, string? reason = null)
        : base(isSuccess, error, message, reason)
    {
        Value = value;
    }

    public TValue? Value { get; private set; }
}
