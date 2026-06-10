namespace Exerciser.WebApi.Models;

internal class ApiMetadata
{
    public string? Title { get; init; }
    public string? Version { get; init; }
    public string? Description { get; init; }
    public string? TermsOfService { get; init; }
    public ContactInfo? Contact { get; init; }
    public LicenseInfo? License { get; init; }
}

internal class ContactInfo
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Url { get; init; }
}

internal class LicenseInfo
{
    public string? Name { get; init; }
    public string? Url { get; init; }
}