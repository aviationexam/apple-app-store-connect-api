using Apple.AppStoreConnect.GeneratorCommon;
using Apple.AppStoreConnect.GeneratorCommon.Extensions;
using H.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Apple.AppStoreConnect.Generator.Processors;

public static class GetNextProcessor
{
    public static FileWithName TryProcessItems(
        ReadOnlySpan<char> targetNamespace,
        IReadOnlyCollection<PathItem> path,
        ReadOnlySpan<byte> lastProperty,
        ref Utf8JsonReader jsonReader
    )
    {
        if (
            lastProperty.SequenceEqual("get"u8)
            && path.Count == 3
            && path.ElementAt(0).TokenType is JsonTokenType.StartObject
            && path.Skip(1).SequenceEqual(new PathItem[]
            {
                new(JsonTokenType.StartObject, "paths"),
                new(JsonTokenType.StartObject, null),
            })
        )
        {
            var jsonReaderClone = jsonReader;

            var getCollection = "get_collection"u8;
            var getToManyRelated = "get_to_many_related"u8;

            if (
                !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.StartObject
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.PropertyName
                || !jsonReaderClone.ValueSpan.SequenceEqual("tags"u8)
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.StartArray
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.String
            )
            {
                return FileWithName.Empty;
            }

            var routeTag = Encoding.UTF8.GetString(jsonReaderClone.ValueSpan.ToArray());

            if (
                !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.EndArray
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.PropertyName
                || !jsonReaderClone.ValueSpan.SequenceEqual("operationId"u8)
                || !jsonReaderClone.Read()
                || jsonReaderClone.TokenType is not JsonTokenType.String
                || !(
                    (
                        jsonReaderClone.ValueSpan.Length > getCollection.Length
                        && jsonReaderClone.ValueSpan[^getCollection.Length..].SequenceEqual(getCollection)
                    )
                    || (
                        jsonReaderClone.ValueSpan.Length > getToManyRelated.Length
                        && jsonReaderClone.ValueSpan[^getToManyRelated.Length..].SequenceEqual(getToManyRelated)
                    )
                )
            )
            {
                return FileWithName.Empty;
            }

            while (jsonReaderClone.Read())
            {
                if (jsonReaderClone.CurrentDepth <= jsonReader.CurrentDepth)
                {
                    return FileWithName.Empty;
                }

                if (
                    jsonReaderClone.TokenType is JsonTokenType.PropertyName
                    && jsonReaderClone.ValueSpan.SequenceEqual("responses"u8)
                    && JsonDocument.TryParseValue(ref jsonReaderClone, out var jsonDocument)
                )
                {
                    var content200 = jsonDocument.RootElement.GetProperty("200"u8)
                        .GetProperty("content"u8);

                    if (!content200.TryGetProperty("application/json"u8, out var jsonResponse))
                    {
                        break;
                    }

                    var responseReference = jsonResponse
                        .GetProperty("schema"u8)
                        .GetProperty("$ref"u8)
                        .GetString();

                    if (responseReference is null)
                    {
                        break;
                    }

                    var route = path.ElementAt(0).PropertyName.AsSpan();
                    var slashIndex = route.LastIndexOf('/');
                    var lastPathSegmentSpan = route[(slashIndex + 1)..];

                    if (char.IsLower(lastPathSegmentSpan[0]))
                    {
                        var span = lastPathSegmentSpan.ToArray().AsSpan();
                        span[0] = char.ToUpperInvariant(span[0]);
                        lastPathSegmentSpan = span;
                    }

                    var lastPathSegment = lastPathSegmentSpan.ToString();
                    var componentName = responseReference.GetComponentName().ToString();

                    return new FileWithName(
                        Name: $"{routeTag}.{lastPathSegment}.g.cs",
                        Text: $$"""
#pragma warning disable 612 // Disable "CS0612 '...' is obsolete"
#nullable enable

namespace {{targetNamespace.ToString()}};

public partial interface I{{routeTag}}Client
{
    System.Threading.Tasks.Task<{{componentName}}> {{lastPathSegment}}Async(
        string next,
        System.Threading.CancellationToken cancellationToken = default
    );
}

public partial class {{routeTag}}Client
{
    public virtual async System.Threading.Tasks.Task<{{componentName}}> {{lastPathSegment}}Async(
        string next,
        System.Threading.CancellationToken cancellationToken
    )
    {
        using var request_ = await CreateHttpRequestMessageAsync(cancellationToken).ConfigureAwait(false);

        request_.Method = new System.Net.Http.HttpMethod("GET");
        request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

        request_.RequestUri = new System.Uri(next, System.UriKind.Absolute);

        using var response_ = await _httpClient.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        var headers_ = System.Linq.Enumerable.ToDictionary(response_.Headers, h_ => h_.Key, h_ => h_.Value);
        if (response_.Content != null && response_.Content.Headers != null)
        {
            foreach (var item_ in response_.Content.Headers)
                headers_[item_.Key] = item_.Value;
        }

        var status_ = (int)response_.StatusCode;
        if (status_ == 400)
        {
            var objectResponse_ = await ReadObjectResponseAsync<ErrorResponse>(response_, headers_, cancellationToken).ConfigureAwait(false);
            if (objectResponse_.Object == null)
            {
                throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            throw new ApiException<ErrorResponse>("Parameter error(s)", status_, objectResponse_.Text, headers_, objectResponse_.Object, null);
        }
        else if (status_ == 403)
        {
            var objectResponse_ = await ReadObjectResponseAsync<ErrorResponse>(response_, headers_, cancellationToken).ConfigureAwait(false);
            if (objectResponse_.Object == null)
            {
                throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            throw new ApiException<ErrorResponse>("Forbidden error", status_, objectResponse_.Text, headers_, objectResponse_.Object, null);
        }
        else if (status_ == 404)
        {
            var objectResponse_ = await ReadObjectResponseAsync<ErrorResponse>(response_, headers_, cancellationToken).ConfigureAwait(false);
            if (objectResponse_.Object == null)
            {
                throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            throw new ApiException<ErrorResponse>("Not found error", status_, objectResponse_.Text, headers_, objectResponse_.Object, null);
        }
        else if (status_ == 200)
        {
            var objectResponse_ = await ReadObjectResponseAsync<{{componentName}}>(response_, headers_, cancellationToken).ConfigureAwait(false);
            if (objectResponse_.Object == null)
            {
                throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            return objectResponse_.Object;
        }
        else
        {
            var responseData_ = response_.Content == null ? null : await response_.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }
    }
}

#pragma warning restore  612
"""
                    );
                }
            }
        }

        return FileWithName.Empty;
    }
}
