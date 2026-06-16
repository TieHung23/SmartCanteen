namespace SC.Infrastructure.DependencyInjection.Options;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public bool Enabled { get; set; }
    public string? ProjectId { get; set; }
    public string? ServiceAccountPath { get; set; }
    public string? ServiceAccountJson { get; set; }
    public string ApiBaseUrl { get; set; } = "https://fcm.googleapis.com";
    public string MessagingScope { get; set; } = "https://www.googleapis.com/auth/firebase.messaging";
}
