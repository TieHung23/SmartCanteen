namespace SC.Contract.Shared;

public class Error(string code, string message) : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("NullValue", "Value cannot be null.");
    public static readonly Error EmptyValue = new("EmptyValue", "Value cannot be empty.");
    public static readonly Error InvalidValue = new("InvalidValue", "Value is invalid.");
    public static readonly Error ServerError = new("ServerError", "An unexpected server error occurred.");

    private string Code { get; } = code;

    private string Message { get; } = message;

    public bool Equals(Error? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Code == other.Code && Message == other.Message;
    }

    public static implicit operator string(Error error)
    {
        return error.Code;
    }

    public static bool operator ==(Error left, Error right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Error left, Error right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Error)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code, Message);
    }
}