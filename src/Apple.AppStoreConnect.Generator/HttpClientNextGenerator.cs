using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Apple.AppStoreConnect.Generator;

[Generator]
public class HttpClientNextGenerator : IIncrementalGenerator
{
    public const string Id = "HCNG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(static ((AdditionalText textFile, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider) x) =>
                !string.IsNullOrEmpty(
                    x.analyzerConfigOptionsProvider.GetOption(x.textFile, "HttpClientNext_OpenApi")
                )
            )
            .Select(static (x, _) => x.Item1)
            .Combine(context.AnalyzerConfigOptionsProvider
                .Select(static (x, _) =>
                    x.GetRequiredGlobalOption("HttpClientNext_Namespace")
                )
            )
            .SelectAndReportExceptions(GetSourceCode, context, Id)
            .AddSource(context);
    }

    private static EquatableArray<FileWithName> GetSourceCode(
        (AdditionalText textFile, string targetNamespace) source,
        CancellationToken cancellationToken
    )
    {
        var documentOptions = new JsonReaderOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
        };

        var jsonReadOnlySpan = File.ReadAllBytes(source.textFile.Path).AsSpan().TrimBom();

        var jsonReader = new Utf8JsonReader(jsonReadOnlySpan, documentOptions);

        ImmutableArray<FileWithName> files;
        try
        {
            files = JsonIterator.ProcessJson(
                    ref jsonReader,
                    source.targetNamespace.AsSpan()
                )
                .ToImmutableArray();
        }
        catch (Exception e)
        {
            throw new Exception(
                e.StackTrace
            );
        }

        return files;
    }
}
