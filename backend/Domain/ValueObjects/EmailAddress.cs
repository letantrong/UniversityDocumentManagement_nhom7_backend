using System.Text.RegularExpressions;
using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record EmailAddress
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        var trimmed = email.Trim().ToLowerInvariant();
        if (!EmailRegex.IsMatch(trimmed))
            throw new DomainException($"Invalid email format: '{email}'.");

        return new EmailAddress(trimmed);
    }

    public override string ToString() => Value;
}
