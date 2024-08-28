using Apple.AppStoreConnect.PreprocessOpenApi;

if (args is [{ } source, { } target])
{
    new OpenApiPreprocessor(source, target).Preprocess();
}
else
{
    Console.WriteLine("Usage: <source> <target>");
}
