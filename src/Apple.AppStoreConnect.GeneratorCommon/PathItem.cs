using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.GeneratorCommon;

public record PathItem(JsonTokenType TokenType, string? PropertyName, PathItem? ParentPathItem = null)
{
    public IReadOnlyCollection<KeyValuePair<string, string>> Properties => _properties;

    private readonly List<KeyValuePair<string, string>> _properties = new();

    public void AddUsefulProperty(ReadOnlySpan<byte> property, ReadOnlySpan<byte> value)
    {
        if (
            property.SequenceEqual("name"u8)
            || property.SequenceEqual("operationId"u8)
        )
        {
            var propertyString = Encoding.UTF8.GetString(property.ToArray());
            var propertyValue = Encoding.UTF8.GetString(value.ToArray());

            _properties.Add(new KeyValuePair<string, string>(propertyString, propertyValue));
        }
        else if (ParentPathItem != null && PropertyName == "tags")
        {
            var propertyValue = Encoding.UTF8.GetString(value.ToArray());

            ParentPathItem._properties.Add(new KeyValuePair<string, string>(PropertyName, propertyValue));
        }
    }

    public virtual bool Equals(PathItem? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TokenType == other.TokenType && PropertyName == other.PropertyName;
    }

    public override int GetHashCode() => HashCode.Combine((int) TokenType, PropertyName);
}
