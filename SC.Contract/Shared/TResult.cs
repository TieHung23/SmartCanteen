namespace SC.Contract.Shared;

public class Result<TValue> : Result
{
    public TValue? Value { get; private set; }
    
    protected internal Result(bool isSuccess, TValue? value, Error error) : base(isSuccess, error)
    {
        Value = value;
    }
    
    public static implicit operator Result<TValue>(TValue value) => Create(value);
}