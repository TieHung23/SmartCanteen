using SC.Domain.Abstraction.Entities;

namespace SC.Domain.Domain.User;

public class User : Entity<Guid>, IAuditableEntity<Guid>
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string ImgUrl { get; set; }
    public required Role Role { get; set; } = Role.User;
    public Money Balance { get; set; } = Money.Create(0, "VND");
    
    
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    
    private User() { }
    
    public static User Create(string name, string email, string passwordHash, string imgUrl, Role role = Role.User)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            ImgUrl = imgUrl,
            Role = role,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
    
    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
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
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}