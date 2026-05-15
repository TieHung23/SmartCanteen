using System.Net.Mail;
using SC.Domain.Abstraction.Aggregates;
using SC.Domain.Abstraction.Entities;
using SC.Domain.Domain.User.Enum;
using SC.Domain.SharedKernel.ValueObjects;

namespace SC.Domain.Domain.User;

public class User : AggregateRoot<Guid>, IAuditableEntity<Guid>
{
    private static readonly string[] FptEmailDomains = { "@fpt.edu.vn" };

    private User()
    {
    }

    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
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

    public bool IsFptEmail()
    {
        return FptEmailDomains.Any(d => Email.EndsWith(d, StringComparison.OrdinalIgnoreCase));
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

    public void UpdateBalance(Money amount)
    {
        Balance = Balance.Add(amount);
    }
}
