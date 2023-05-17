using Apple.AppStoreConnect.GeneratorCommon;
using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Apple.AppStoreConnect.OpenApiDocument.Generator;

[Generator]
public class OpenApiTransposeGenerator : IIncrementalGenerator
{
    public const string Id = "OATG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.AdditionalTextsProvider
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
            .SelectAndReportExceptions(GetSourceCode, context, Id)
            .AddSource(context);
    }

    private static FileWithName GetSourceCode(
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

#pragma warning disable RS1035
        Directory.CreateDirectory(source.resultOpenApiDestination);
#pragma warning restore RS1035

        // This is a hack until source-generator support embedded resources
        using var destinationFileStream = new FileStream(
            Path.Combine(source.resultOpenApiDestination, Path.GetFileName(source.textFile.Path)),
            FileMode.Create, FileAccess.Write, FileShare.Write
        );

        using var jsonWriter = new Utf8JsonWriter(destinationFileStream, options: writerOptions);

        if (source.textFile.GetText() is var fileContent && fileContent is null)
        {
            return FileWithName.Empty;
        }

        var jsonReadOnlySpan = Encoding.UTF8.GetBytes(fileContent.ToString()).AsSpan().TrimBom();

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

        return FileWithName.Empty;
    }
}
