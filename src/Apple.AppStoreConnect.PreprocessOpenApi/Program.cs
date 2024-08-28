using Apple.AppStoreConnect.PreprocessOpenApi;

if (args is [{ } source, { } target, { } versionFile])
{
    new OpenApiPreprocessor(
        source, target, versionFile
    ).Preprocess();
}
else
{
    Console.WriteLine("Usage: <source> <target>");
}
