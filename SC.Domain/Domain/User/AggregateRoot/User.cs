using System.Net.Mail;
using System.Text.RegularExpressions;
using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.User.Enum;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.User;

public class User : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private static readonly string[] FptEmailDomains = { "@fpt.edu.vn", "@fe.edu.vn" };
    private const string LecturerEmailDomain = "@fe.edu.vn";

    // FPT student emails embed the student code (2 letters + 6 digits) before '@',
    // e.g. minhthtse183449@fpt.edu.vn -> SE183449.
    private static readonly Regex StudentIdPattern =
        new(@"[A-Za-z]{2}\d{6}$", RegexOptions.Compiled);

    private User()
    {
    }

    public required string Name { get; set; }
    public required string Email { get; set; }
    // Null for accounts created via an external provider (e.g. Google) that have no password.
    public string? PasswordHash { get; set; }

    // Google "sub" claim — the stable account identifier set when the user links Google sign-in.
    public string? GoogleSubjectId { get; set; }
    public string? ImgUrl { get; set; }
    public required Role Role { get; set; } = Role.User;
    public required UserCategory Category { get; set; } = UserCategory.Student;
    public required AccountStatus Status { get; set; } = AccountStatus.PendingEmailVerification;
    public bool EmailVerified { get; set; }
    public string? StudentId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? MajorOrClass { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Gender? Gender { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public Money Balance { get; set; } = Money.Create(0);
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public static User Register(
        string name,
        string email,
        string passwordHash,
        UserCategory category,
        string? studentId = null,
        DateOnly? dateOfBirth = null,
        string? majorOrClass = null,
        string? phoneNumber = null,
        string? address = null,
        Gender? gender = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = Role.User,
            Category = category,
            Status = AccountStatus.PendingEmailVerification,
            StudentId = studentId,
            DateOfBirth = dateOfBirth,
            MajorOrClass = majorOrClass,
            PhoneNumber = phoneNumber,
            Address = address,
            Gender = gender,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void ConfirmEmail()
    {
        if (EmailVerified) return;
        EmailVerified = true;
        Status = IsFptEmail() ? AccountStatus.Active : AccountStatus.PendingIdentityVerification;
    }

    public void ActivateAfterIdentityApproved()
    {
        if (Status != AccountStatus.PendingIdentityVerification)
        {
            throw new InvalidOperationException(
                "Cannot activate: account is not awaiting identity verification approval.");
        }

        Status = AccountStatus.Active;
    }

    public void Suspend()
    {
        Status = AccountStatus.Suspended;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public bool IsFptEmail() => IsFptEmail(Email);

    /// <summary>
    /// True when the email belongs to a trusted FPT University domain. Static overload so
    /// callers can check an email before a <see cref="User"/> instance exists.
    /// </summary>
    public static bool IsFptEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && FptEmailDomains.Any(d => email.EndsWith(d, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an account from a verified Google (FPT) sign-in. The Google identity already
    /// proves email ownership and the FPT domain is trusted, so the account is active immediately
    /// with no password and no email-verification step.
    /// </summary>
    public static User RegisterWithGoogle(
        string name,
        string email,
        string googleSubjectId,
        UserCategory category,
        string? studentId,
        string? imgUrl)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = null,
            GoogleSubjectId = googleSubjectId,
            ImgUrl = imgUrl,
            Role = Role.User,
            Category = category,
            Status = AccountStatus.Active,
            EmailVerified = true,
            StudentId = studentId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Links a Google identity to an existing account (e.g. one originally registered with a
    /// password). No-op when the account is already linked.
    /// </summary>
    public void LinkGoogle(string googleSubjectId)
    {
        if (string.IsNullOrWhiteSpace(googleSubjectId))
            throw new ArgumentException("Google subject id is required.", nameof(googleSubjectId));

        GoogleSubjectId ??= googleSubjectId;
    }

    /// <summary>
    /// Extracts the FPT student code embedded in an institutional email address.
    /// Returns false for non-student emails (lecturers/staff have no embedded code).
    /// </summary>
    public static bool TryExtractStudentId(string email, out string? studentId)
    {
        studentId = null;
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return false;

        var localPart = email[..atIndex];
        var match = StudentIdPattern.Match(localPart);
        if (!match.Success)
            return false;

        studentId = match.Value.ToUpperInvariant();
        return true;
    }

    /// <summary>
    /// Resolves the user category for a Google FPT sign-in: <c>@fe.edu.vn</c> is a lecturer,
    /// an email carrying a student code is a student, anything else is staff.
    /// </summary>
    public static UserCategory ResolveCategoryFromFptEmail(string email, bool hasStudentCode)
    {
        if (!string.IsNullOrWhiteSpace(email)
            && email.EndsWith(LecturerEmailDomain, StringComparison.OrdinalIgnoreCase))
        {
            return UserCategory.Lecturer;
        }

        return hasStudentCode ? UserCategory.Student : UserCategory.Staff;
    }

    public void UpdateBalance(Money amount)
    {
        Balance = Balance.Add(amount);
    }
}
