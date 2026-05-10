using SC.Domain.Abstraction.Aggregates;

namespace SC.Domain.Domain.Setting.AggregateRoot;

public class Setting : AggregateRoot<Guid>
{
    private Setting()
    {
    }

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public static Setting Create(string key, string value)
    {
        return new Setting
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value
        };
    }

    public void Update(string value)
    {
        Value = value;
    }
}