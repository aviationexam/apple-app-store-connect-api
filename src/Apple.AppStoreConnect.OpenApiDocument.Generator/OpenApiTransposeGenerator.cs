using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.IO;
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

        context.RegisterSourceOutput(source, static (context, resultJson) =>
        {
            Directory.CreateDirectory(resultJson.Destination);

            // This is a hack until source-generator support embedded resources
            using var file = new FileStream(
                Path.Combine(resultJson.Destination, resultJson.Filename),
                FileMode.Create, FileAccess.Write, FileShare.Write
            );

            resultJson.Stream.CopyTo(file);
            file.Flush(flushToDisk: true);
        });
    }

    private static ResultJson GetSourceCode(
        (AdditionalText textFile, string resultOpenApiDestination) source,
        CancellationToken cancellationToken = default
    )
    {
        return new ResultJson(
            Destination: source.resultOpenApiDestination,
            Filename: Path.GetFileName(source.textFile.Path),
            Stream: File.OpenRead(source.textFile.Path)
        );
    }

    private record ResultJson(
        string Destination,
        string Filename,
        Stream Stream
    );
}
