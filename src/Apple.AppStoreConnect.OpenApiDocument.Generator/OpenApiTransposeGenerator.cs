using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

[Generator]
public class OpenApiTransposeGenerator : IIncrementalGenerator
{
    public const string Id = "OATG";

    private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(static ((AdditionalText textFile, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider) x) =>
                !string.IsNullOrEmpty(
                    x.analyzerConfigOptionsProvider.GetOption(x.textFile, "OpenApi")
                )
            )
            .Select(static (x, _) => x.Item1)
            .Combine(context.AnalyzerConfigOptionsProvider
                .Select(static (x, _) =>
                    x.GetRequiredGlobalOption("ResultOpenApiDestination")
                )
            )
            .SelectAndReportExceptions(GetSourceCode, context, Id);

        context.RegisterSourceOutput(source, static (context, properties) =>
        {
            foreach (var property in properties)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        $"{Id}_0001",
                        "Processed property",
                        $"Processed property {property}",
                        "Apple.AppStoreConnect.OpenApiDocument.Generator",
                        DiagnosticSeverity.Error,
                        true
                    ), null
                ));
            }
        });
    }

    private static IEnumerable<string> GetSourceCode(
        (AdditionalText textFile, string resultOpenApiDestination) source,
        CancellationToken cancellationToken = default
    )
    {
        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };

        var documentOptions = new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
        };

        Directory.CreateDirectory(source.resultOpenApiDestination);

        // This is a hack until source-generator support embedded resources
        using var destinationFileStream = new FileStream(
            Path.Combine(source.resultOpenApiDestination, Path.GetFileName(source.textFile.Path)),
            FileMode.Create, FileAccess.Write, FileShare.Write
        );

        using var jsonWriter = new Utf8JsonWriter(destinationFileStream, options: writerOptions);

        ReadOnlySpan<byte> jsonReadOnlySpan = File.ReadAllBytes(source.textFile.Path);

        if (jsonReadOnlySpan.StartsWith(Utf8Bom))
        {
            jsonReadOnlySpan = jsonReadOnlySpan[Utf8Bom.Length..];
        }

        var jsonReader = new Utf8JsonReader(jsonReadOnlySpan, isFinalBlock: false, state: default);

        try
        {
            ReadJson(ref jsonReader, jsonWriter);
        }
        catch (Exception e)
        {
            throw new Exception(
                e.StackTrace
            );
        }

        /*
        using var documentReader = JsonDocument.Parse(sourceJson, documentOptions);

        var root = documentReader.RootElement;

        writer.WriteStartObject();

        foreach (var property in root.EnumerateObject())
        {
            foreach (
                var processComponent in ProcessJsonObject(
                    property, writer,
                    ProcessRoot
                ))
            {
                yield return $"{processComponent}";
            }
        }

        writer.WriteEndObject();
*/
        jsonWriter.Flush();
        destinationFileStream.Flush(flushToDisk: true);

        yield break;
    }

    private static void ReadJson(
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter
    )
    {
        while (jsonReader.Read())
        {
            var tokenType = jsonReader.TokenType;

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    jsonWriter.WriteStartObject();

                    jsonWriter.WriteStartArray("customJsonFormatting");

                    jsonWriter.WriteStartObject();

                    jsonWriter.WritePropertyName("value");
                    jsonWriter.WriteRawValue("""
"abc"
""");
                    //ReadObject(ref jsonReader, jsonWriter);

                    jsonWriter.WriteEndObject();

                    jsonWriter.WriteEndArray();

                    jsonWriter.WriteEndObject();

                    break;
                case JsonTokenType.PropertyName:

                    break;
            }
        }
    }

    private static void ReadObject(
        ref Utf8JsonReader jsonReader, Utf8JsonWriter jsonWriter
    )
    {
        while (jsonReader.Read())
        {
        }
    }

    private static IEnumerable<string> ProcessRoot(JsonProperty component, Utf8JsonWriter writer)
    {
        yield return component.Name + component.GetType().Name;

        if (component.Name == "components")
        {
            foreach (
                var processComponent in ProcessJsonObject(
                    component, writer,
                    ProcessComponents
                ))
            {
                yield return $"  {processComponent}";
            }
        }
        else if (component.Name == "paths")
        {
            using var nullWriter = new Utf8JsonWriter(Stream.Null);
            component.WriteTo(nullWriter);
        }
        else
        {
            using var nullWriter = new Utf8JsonWriter(Stream.Null);
            component.WriteTo(nullWriter);
            //component.WriteTo(writer);
        }
    }

    private static IEnumerable<string> ProcessComponents(JsonProperty component, Utf8JsonWriter writer)
    {
        if (component.Name == "schemas")
        {
            yield return component.Name;

            foreach (
                var processComponent in ProcessJsonObject(
                    component, writer,
                    ProcessComponentsSchema
                ))
            {
                yield return $"  {processComponent}";
            }
        }
        else
        {
            component.WriteTo(writer);
        }
    }

    private static IEnumerable<string> ProcessComponentsSchema(JsonProperty component, Utf8JsonWriter writer)
    {
        if (component.Name == "SubscriptionStatusUrlVersion")
        {
            yield return component.Name;

            foreach (
                var processComponent in ProcessJsonObject(
                    component, writer,
                    ProcessComponentsSchema
                ))
            {
                yield return $"  {processComponent}";
            }
        }
        else
        {
            component.WriteTo(writer);
        }
    }

    private static IEnumerable<string> ProcessJsonObject(
        JsonProperty component,
        Utf8JsonWriter writer,
        Func<JsonProperty, Utf8JsonWriter, IEnumerable<string>> callback
    )
    {
        if (component.Value.ValueKind is JsonValueKind.String)
        {
            component.WriteTo(writer);

            yield break;
        }

        if (component.Value.ValueKind is JsonValueKind.Object)
        {
            writer.WritePropertyName(component.Name);
            writer.WriteStartObject();

            using var nullWriter = new Utf8JsonWriter(Stream.Null);
            component.WriteTo(nullWriter);
            //foreach (var property in component.Value.EnumerateObject())
            //{
            //    foreach (
            //        var processComponent in callback(
            //            property,
            //            writer
            //        )
            //    )
            //    {
            //        yield return $"  {processComponent}";
            //    }
            //}

            writer.WriteEndObject();
        }
    }

    private static IEnumerable<string> ProcessJsonArray(
        JsonProperty component,
        Utf8JsonWriter writer,
        Func<JsonProperty, Utf8JsonWriter, IEnumerable<string>> callback
    )
    {
        if (component.Value.ValueKind is JsonValueKind.Array)
        {
            writer.WritePropertyName(component.Name);
            writer.WriteStartArray();

            foreach (var property in component.Value.EnumerateObject())
            {
                foreach (
                    var processComponent in callback(
                        property,
                        writer
                    )
                )
                {
                    yield return $"  {processComponent}";
                }
            }

            writer.WriteEndArray();
        }
    }
}
