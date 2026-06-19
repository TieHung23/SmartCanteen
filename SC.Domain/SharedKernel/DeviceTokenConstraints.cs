namespace SC.Domain.SharedKernel;

public static class DeviceTokenConstraints
{
    public const int TokenMaxLength = 4096;
    public const int TokenHashMaxLength = 64;
    public const int PlatformMaxLength = 20;
    public const int DeviceIdMaxLength = 256;
    public const int AppVersionMaxLength = 64;
}
