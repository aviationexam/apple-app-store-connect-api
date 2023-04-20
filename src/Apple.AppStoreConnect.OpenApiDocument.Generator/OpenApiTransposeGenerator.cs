﻿using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }

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

        var jsonReader = new Utf8JsonReader(jsonReadOnlySpan, documentOptions);

        try
        {
            JsonIterator.ProcessJson(ref jsonReader, jsonWriter);
        }
        catch (Exception e)
        {
            throw new Exception(
                e.StackTrace
            );
        }

        jsonWriter.Flush();
        destinationFileStream.Flush(flushToDisk: true);

        yield break;
    }
}
