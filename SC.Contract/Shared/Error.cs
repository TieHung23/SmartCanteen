using System.Text.Json.Serialization;

namespace SC.Contract.Shared;

public class Error(string code, string message, int httpStatusCode = 400) : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty, 200);
    public static readonly Error NullValue = new("NullValue", "Value cannot be null.", 400);
    public static readonly Error EmptyValue = new("EmptyValue", "Value cannot be empty.", 400);
    public static readonly Error InvalidValue = new("InvalidValue", "Value is invalid.", 400);
    public static readonly Error ServerError = new("ServerError", "An unexpected server error occurred.", 500);

    // Auth & registration (BR-02, BR-12, BR-13, BR-19, BR-24)
    public static readonly Error EmailAlreadyExists = new("EmailAlreadyExists", "An account with this email already exists.", 409);
    public static readonly Error WeakPassword = new("WeakPassword", "Password does not meet the minimum security requirements.", 400);
    public static readonly Error InvalidCredentials = new("InvalidCredentials", "Email or password is incorrect.", 401);
    public static readonly Error EmailNotVerified = new("EmailNotVerified", "Please verify your email before logging in.", 401);
    public static readonly Error AccountNotActive = new("AccountNotActive", "Account is not active.", 403);
    public static readonly Error AccountSuspended = new("AccountSuspended", "Account has been suspended.", 401);
    public static readonly Error InvalidOrExpiredToken = new("InvalidOrExpiredToken", "The token is invalid or has expired.", 400);
    public static readonly Error InvalidRefreshToken = new("InvalidRefreshToken", "The refresh token is invalid, revoked, or expired.", 401);
    public static readonly Error Forbidden = new("Forbidden", "You do not have permission to perform this action.", 403);

    // Google sign-in (BR-01, BR-03, BR-04)
    public static readonly Error GoogleTokenInvalid = new("GoogleTokenInvalid", "The Google sign-in token is invalid or has expired.", 401);
    public static readonly Error GoogleEmailNotVerified = new("GoogleEmailNotVerified", "The Google account email address is not verified.", 401);
    public static readonly Error NonFptGoogleAccount = new("NonFptGoogleAccount", "Google sign-in is only available for FPT University accounts.", 403);

    // Identity verification (BR-07, BR-27, BR-29, BR-30, BR-31, BR-36, BR-39, BR-49)
    public static readonly Error StudentIdAlreadyUsed = new("StudentIdAlreadyUsed", "This institutional ID is already linked to another account.", 409);
    public static readonly Error VerificationAlreadyPending = new("VerificationAlreadyPending", "You already have a pending verification request.", 409);
    public static readonly Error VerificationNotFound = new("VerificationNotFound", "Verification request was not found.", 404);
    public static readonly Error VerificationNotPending = new("VerificationNotPending", "Verification request is no longer pending.", 409);
    public static readonly Error RejectionReasonRequired = new("RejectionReasonRequired", "A rejection reason is required.", 400);
    public static readonly Error UnsupportedFileFormat = new("UnsupportedFileFormat", "Uploaded file format is not supported.", 400);
    public static readonly Error FileTooLarge = new("FileTooLarge", "Uploaded file exceeds the maximum allowed size.", 413);

    [JsonIgnore] public string Code { get; } = code;

    [JsonIgnore] public string Message { get; } = message;

    [JsonIgnore] public int HttpStatusCode { get; } = httpStatusCode;

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

    public override string ToString()
    {
        return Code;
    }
}
