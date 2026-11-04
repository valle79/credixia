namespace Lending.Api.Configuration;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = "Platform Administrator";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}
