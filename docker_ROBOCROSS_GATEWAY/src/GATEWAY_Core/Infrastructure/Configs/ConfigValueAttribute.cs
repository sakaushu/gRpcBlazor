namespace GATEWAYCore.Infrastructure.Configs;

[AttributeUsage(AttributeTargets.Field)]
public class ConfigValueAttribute : Attribute
{
    public string Value { get; }

    public ConfigValueAttribute(string value)
    {
        Value = value;
    }
}
