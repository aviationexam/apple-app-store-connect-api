using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

public record PathItem(JsonTokenType TokenType, string? PropertyName)
{
    public IReadOnlyDictionary<string, string> Properties => _properties;

    private Dictionary<string, string> _properties = new();

    public void AddUsefulProperty(ReadOnlySpan<byte> property, ReadOnlySpan<byte> value)
    {
        if (
            property.SequenceEqual("tags"u8)
            || property.SequenceEqual("name"u8)
        )
        {
            var propertyString = Encoding.UTF8.GetString(property.ToArray());
            var propertyValue = Encoding.UTF8.GetString(value.ToArray());

            _properties.Add(propertyString, propertyValue);
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
