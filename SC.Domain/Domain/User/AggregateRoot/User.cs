using System.Net.Mail;
using System.Text.RegularExpressions;
using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.User.Enum;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.User;

public class User : AggregateRoot<Guid>, IAuditableEntity<Guid>, ISoftDeletable
{
    private static readonly string[] FptEmailDomains = { "@fpt.edu.vn", "@fe.edu.vn" };
    private const string LecturerEmailDomain = "@fe.edu.vn";
    private static readonly Regex StudentIdPattern =
        new(@"[A-Za-z]{2}\d{6}$", RegexOptions.Compiled);

    private User() { }

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? GoogleSubjectId { get; private set; }
    public string? ImgUrl { get; private set; }
    public Role Role { get; private set; } = Role.User;
    public UserCategory Category { get; private set; } = UserCategory.Student;
    public AccountStatus Status { get; private set; } = AccountStatus.PendingEmailVerification;
    public string? StatusReason { get; private set; }
    public bool EmailVerified { get; private set; }
    public string? StudentId { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? MajorOrClass { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }
    public Gender? Gender { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public Money Balance { get; private set; } = Money.Create(0);
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }

    /// <summary>
    /// Self sign-up is limited to the categories the canteen onboards itself. Staff and
    /// external accounts are provisioned by a manager instead.
    /// </summary>
    public static bool CanSelfRegister(UserCategory category)
        => category is UserCategory.Student or UserCategory.Lecturer;

    /// <summary>
    /// Only students confirm their address with an emailed code; lecturer accounts are
    /// created with the email already confirmed.
    /// </summary>
    public static bool RequiresEmailVerificationCode(UserCategory category)
        => category == UserCategory.Student;

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
        var now = DateTimeOffset.UtcNow;
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
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void ConfirmEmail()
    {
        if (EmailVerified) return;
        EmailVerified = true;
        StatusReason = null;
        Status = IsFptEmail() ? AccountStatus.Active : AccountStatus.PendingIdentityVerification;
    }

    public void ActivateAfterIdentityApproved()
    {
        if (Status != AccountStatus.PendingIdentityVerification)
            throw new InvalidOperationException("Cannot activate: account is not awaiting identity verification approval.");
        StatusReason = null;
        Status = AccountStatus.Active;
    }

    public void Suspend(string reason)
    {
        StatusReason = reason;
        Status = AccountStatus.Suspended;
    }

    public void Ban(string reason)
    {
        StatusReason = reason;
        Status = AccountStatus.Banned;
    }

    public void Reactivate()
    {
        StatusReason = null;
        Status = AccountStatus.Active;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void UpdateProfile(
        string name,
        string? imgUrl,
        DateOnly? dateOfBirth,
        string? majorOrClass,
        string? phoneNumber,
        string? address,
        Gender? gender)
    {
        Name = name;
        ImgUrl = imgUrl;
        DateOfBirth = dateOfBirth;
        MajorOrClass = majorOrClass;
        PhoneNumber = phoneNumber;
        Address = address;
        Gender = gender;
    }

    public bool IsFptEmail() => IsFptEmail(Email);

    public static bool IsFptEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && FptEmailDomains.Any(d => email.EndsWith(d, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Institutional mailboxes carry the category: staff addresses live on the lecturer
    /// domain, every other FPT address belongs to a student.
    /// </summary>
    public static UserCategory ResolveCategoryFromEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && email.EndsWith(LecturerEmailDomain, StringComparison.OrdinalIgnoreCase)
                ? UserCategory.Lecturer
                : UserCategory.Student;
    }

    public static bool IsValidEmail(string email)
    {
        try { var addr = new MailAddress(email); return addr.Address == email; }
        catch { return false; }
    }

    public static User RegisterWithGoogle(
        string name,
        string email,
        string googleSubjectId,
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
            Category = ResolveCategoryFromEmail(email),
            Status = AccountStatus.Active,
            EmailVerified = true,
            StudentId = studentId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void LinkGoogle(string googleSubjectId)
    {
        if (string.IsNullOrWhiteSpace(googleSubjectId))
            throw new ArgumentException("Google subject id is required.", nameof(googleSubjectId));
        GoogleSubjectId ??= googleSubjectId;
    }

    public static bool TryExtractStudentId(string email, out string? studentId)
    {
        studentId = null;
        if (string.IsNullOrWhiteSpace(email)) return false;
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        var localPart = email[..atIndex];
        var match = StudentIdPattern.Match(localPart);
        if (!match.Success) return false;
        studentId = match.Value.ToUpperInvariant();
        return true;
    }

    public void UpdateBalance(Money amount)
    {
        Balance = Balance.Add(amount);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
