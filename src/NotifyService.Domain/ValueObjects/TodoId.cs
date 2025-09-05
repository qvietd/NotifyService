namespace NotifyService.Domain.ValueObjects;

public record TodoId(Guid Value)
{
    public static TodoId New() => new(Guid.NewGuid());
    public static TodoId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}

public enum Priority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}