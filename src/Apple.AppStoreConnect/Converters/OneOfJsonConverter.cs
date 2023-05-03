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
    private readonly PropertyInfo? _oneOfDiscriminator = typeof(TOneOf).GetProperty("OneOfType");

    private readonly ILogger _logger;
    private readonly IDictionary<string, PropertyInfo> _jsonTypeMap;
    private readonly IDictionary<string, Enum> _oneOfDiscriminators = new Dictionary<string, Enum>();
    private readonly IDictionary<Enum, string> _reversedOneOfDiscriminators = new Dictionary<Enum, string>();

    public OneOfJsonConverter(
        ILogger logger
    )
    {
        _logger = logger;

        _jsonTypeMap = BuildJsonTypeMap();
        BuildOneOfDiscriminatorsMap(_jsonTypeMap);
    }

    private IDictionary<string, PropertyInfo> BuildJsonTypeMap()
    {
        var jsonTypeMap = new Dictionary<string, PropertyInfo>();

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

            jsonTypeMap.Add(fieldTypeValues.ElementAt(0), propertyInfo);
        }

        return jsonTypeMap;
    }

    private void BuildOneOfDiscriminatorsMap(IDictionary<string, PropertyInfo> jsonTypeMap)
    {
        if (_oneOfDiscriminator is null)
        {
            _logger.LogError("The {OneOfType} does not contain 'OneOfType' property", typeof(TOneOf));
            return;
        }

        var oneOfDiscriminatorType = _oneOfDiscriminator.PropertyType.GetNotNullableType();

        foreach (Enum enumValue in oneOfDiscriminatorType.GetEnumValues())
        {
            var enumName = Enum.GetName(oneOfDiscriminatorType, enumValue)!;

            _oneOfDiscriminators.Add(enumName, enumValue);
        }

        foreach (var (typeName, propertyInfo) in jsonTypeMap)
        {
            _reversedOneOfDiscriminators.Add(_oneOfDiscriminators[propertyInfo.Name], typeName);
        }
    }


    private static ITypedReader<TOneOf> CreateTypedReader(PropertyInfo propertyInfo)
    {
        var typedReaderType = typeof(TypedReader<,>).MakeGenericType(
            typeof(TOneOf), propertyInfo.PropertyType
        );

        return (ITypedReader<TOneOf>) Activator.CreateInstance(typedReaderType)!;
    }

    private static ITypedWriter<TOneOf> CreateTypedWriter(PropertyInfo propertyInfo)
    {
        var typedWriterType = typeof(TypedWriter<,>).MakeGenericType(
            typeof(TOneOf), propertyInfo.PropertyType
        );

        return (ITypedWriter<TOneOf>) Activator.CreateInstance(typedWriterType)!;
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

                            CreateTypedReader(propertyInfo).Read(
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
        if (_oneOfDiscriminator is null)
        {
            _logger.LogError("The {OneOfType} does not contain 'OneOfType' property", typeof(TOneOf));
            return;
        }

        var oneOfTypeValue = (Enum?) _oneOfDiscriminator.GetValue(value);

        if (oneOfTypeValue is null)
        {
            _logger.LogError("The 'OneOfType' property in the {OneOfType} bust be set", typeof(TOneOf));
            return;
        }

        if (
            _reversedOneOfDiscriminators.TryGetValue(oneOfTypeValue, out var typeName)
            && _jsonTypeMap.TryGetValue(typeName, out var propertyInfo)
        )
        {
            CreateTypedWriter(propertyInfo).Write(
                value,
                propertyInfo,
                writer, options
            );
        }
    }
}
