using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record DocumentNumber
{
    public string Value { get; }

    private DocumentNumber(string value)
    {
        Value = value;
    }

    public static DocumentNumber Create(string prefix, int year, int sequence)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new DomainException("Prefix cannot be empty.");

        if (sequence <= 0)
            throw new DomainException("Sequence must be greater than zero.");

        var formatted = $"{prefix.Trim().ToUpperInvariant()}-{year}-{sequence:D4}";
        return new DocumentNumber(formatted);
    }

    public static DocumentNumber FromExisting(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Document number cannot be empty.");

        return new DocumentNumber(value.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;
}
