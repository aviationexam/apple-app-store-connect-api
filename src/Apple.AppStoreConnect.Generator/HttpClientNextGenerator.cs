using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;

namespace Apple.AppStoreConnect.Generator;

[Generator]
public class HttpClientNextGenerator : IIncrementalGenerator
{
    public const string Id = "HCNG";

    private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".nswag", StringComparison.InvariantCultureIgnoreCase))
            .SelectAndReportExceptions(GetSourceCode, context, Id)
            .AddSource(context);
    }

    private static EquatableArray<FileWithName> GetSourceCode(
        AdditionalText textFile,
        CancellationToken cancellationToken
    )
    {
        ReadOnlySpan<byte> jsonReadOnlySpan = File.ReadAllBytes(textFile.Path);

        if (jsonReadOnlySpan.StartsWith(Utf8Bom))
        {
            jsonReadOnlySpan = jsonReadOnlySpan[Utf8Bom.Length..];
        }

        ImmutableArray<FileWithName> files;
        try
        {
            files = JsonIterator.ProcessJson(jsonReadOnlySpan)
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
