using System.Text.Json;

namespace Apple.AppStoreConnect.PreprocessOpenApi;

public sealed record TreeItem(
    JsonTokenType JsonTokenType,
    string? PropertyName
);
