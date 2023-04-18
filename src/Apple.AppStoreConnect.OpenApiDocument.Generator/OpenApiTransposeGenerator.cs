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

        var documentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
        };

        Directory.CreateDirectory(source.resultOpenApiDestination);

        // This is a hack until source-generator support embedded resources
        using var destinationFile = new FileStream(
            Path.Combine(source.resultOpenApiDestination, Path.GetFileName(source.textFile.Path)),
            FileMode.Create, FileAccess.Write, FileShare.Write
        );

        using var sourceJson = File.OpenRead(source.textFile.Path);
        using var writer = new Utf8JsonWriter(destinationFile, options: writerOptions);

        using var documentReader = JsonDocument.Parse(sourceJson, documentOptions);

        var root = documentReader.RootElement;

        if (root.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
        }
        else
        {
            yield break;
        }


        foreach (var property in root.EnumerateObject())
        {
            foreach (
                var processComponent in ProcessSchema(
                    property, writer,
                    ProcessRoot
                ))
            {
                yield return $"  {processComponent}";
            }
        }

        writer.WriteEndObject();

        writer.Flush();
        destinationFile.Flush(flushToDisk: true);
    }

    private static IEnumerable<string> ProcessRoot(JsonProperty component, Utf8JsonWriter writer)
    {
        yield return component.Name;

        if (component.Name == "components")
        {
            foreach (
                var processComponent in ProcessSchema(
                    component, writer,
                    ProcessComponents
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

    private static IEnumerable<string> ProcessComponents(JsonProperty component, Utf8JsonWriter writer)
    {
        yield return component.Name;

        if (component.Name == "schemas")
        {
            foreach (
                var processComponent in ProcessSchema(
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
        yield return component.Name;
        component.WriteTo(writer);
    }

    private static IEnumerable<string> ProcessSchema(
        JsonProperty component,
        Utf8JsonWriter writer,
        Func<JsonProperty, Utf8JsonWriter, IEnumerable<string>> callback
    )
    {
        if (component.Value.ValueKind == JsonValueKind.Object)
        {
            writer.WritePropertyName(component.Name);
            writer.WriteStartObject();
        }
        else
        {
            yield break;
        }

        foreach (var property in component.Value.EnumerateObject())
        {
            yield return property.Name;

            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
