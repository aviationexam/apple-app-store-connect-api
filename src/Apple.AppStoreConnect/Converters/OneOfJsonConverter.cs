using Apple.AppStoreConnect.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apple.AppStoreConnect.Converters;

public class OneOfJsonConverter<TOneOf> : JsonConverter<TOneOf>
    where TOneOf : OneOf, new()
{
    private readonly PropertyInfo? _oneOfDiscriminator;

    private readonly ILogger _logger;
    private readonly IDictionary<string, PropertyInfo> _jsonTypeMap = new Dictionary<string, PropertyInfo>();
    private readonly IDictionary<string, Enum> _oneOfDiscriminators = new Dictionary<string, Enum>();

    public OneOfJsonConverter(
        ILogger logger
    )
    {
        _logger = logger;
        _oneOfDiscriminator = typeof(TOneOf).GetProperty("OneOfType");

        BuildJsonTypeMap();
    }

    private void BuildJsonTypeMap()
    {
        if (_oneOfDiscriminator is null)
        {
            _logger.LogError("The {OneOfType} does not contain 'OneOfType' property", typeof(TOneOf));
            return;
        }

        var oneOfDiscriminatorType = _oneOfDiscriminator.PropertyType.GetNotNullableType();

        foreach (Enum enumValue in oneOfDiscriminatorType.GetEnumValues())
        {
            _oneOfDiscriminators.Add(
                Enum.GetName(oneOfDiscriminatorType, enumValue)!,
                enumValue
            );
        }

        foreach (var propertyInfo in typeof(TOneOf).GetProperties())
        {
            var propertyInfoType = propertyInfo.PropertyType.GetNotNullableType();

            if (propertyInfoType.IsEnum)
            {
                continue;
            }

            var typePropertyInfo = propertyInfoType.GetProperty("Type");

            if (typePropertyInfo is null)
            {
                continue;
            }

            var typePropertyInfoType = typePropertyInfo.PropertyType;
            var typeEnumFields = typePropertyInfoType.GetFields();
            var fieldTypeValues = new List<string>(1);
            foreach (var typeEnumField in typeEnumFields)
            {
                if (typeEnumField.FieldType.IsEnum && typeEnumField.FieldType == typePropertyInfoType)
                {
                    var enumMemberAttribute = typeEnumField.GetCustomAttribute<EnumMemberAttribute>();
                    if (enumMemberAttribute is null)
                    {
                        continue;
                    }

                    fieldTypeValues.Add(enumMemberAttribute.Value!);
                }
            }

            if (fieldTypeValues.Count > 1)
            {
                _logger.LogError(
                    "It is expected that {OneOfType} is single value enum, {EnumValue} found",
                    typePropertyInfoType,
                    fieldTypeValues.ToArray()
                );

                continue;
            }

            _jsonTypeMap.Add(fieldTypeValues.ElementAt(0), propertyInfo);
        }
    }

    public override TOneOf? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        var jsonReaderClone = reader;

        var depth = 0;
        ReadOnlySpan<byte> lastProperty = default;
        while (jsonReaderClone.Read())
        {
            if (jsonReaderClone.CurrentDepth <= reader.CurrentDepth)
            {
                break;
            }

            switch (jsonReaderClone.TokenType)
            {
                case JsonTokenType.StartArray:
                case JsonTokenType.StartObject:
                    depth++;
                    break;

                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                    depth--;
                    lastProperty = default;
                    break;

                case JsonTokenType.PropertyName:
                    lastProperty = jsonReaderClone.ValueSpan;
                    break;

                case JsonTokenType.String:
                    if (depth == 0 && lastProperty.SequenceEqual("type"u8))
                    {
                        var typeName = Encoding.UTF8.GetString(jsonReaderClone.ValueSpan);

                        if (_jsonTypeMap.TryGetValue(typeName, out var propertyInfo))
                        {
                            var oneOfEnvelope = new TOneOf();

                            var typedReaderType = typeof(TypedReader<,>).MakeGenericType(
                                typeof(TOneOf), propertyInfo.PropertyType
                            );
                            var typedReader = (TypedReader<TOneOf>) Activator.CreateInstance(typedReaderType)!;

                            typedReader.Read(
                                oneOfEnvelope,
                                propertyInfo,
                                ref reader, options
                            );

                            if (
                                _oneOfDiscriminators.TryGetValue(
                                    propertyInfo.Name,
                                    out var oneOfDiscriminator
                                )
                            )
                            {
                                _oneOfDiscriminator?.SetValue(oneOfEnvelope, oneOfDiscriminator);
                            }
                            else
                            {
                                _logger.LogError(
                                    "There is no discriminator mapping for {TypeName} item",
                                    propertyInfo.Name
                                );
                            }

                            return oneOfEnvelope;
                        }

                        _logger.LogError("There is no mapping for received {TypeName}", typeName);
                    }

                    break;
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, TOneOf value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
