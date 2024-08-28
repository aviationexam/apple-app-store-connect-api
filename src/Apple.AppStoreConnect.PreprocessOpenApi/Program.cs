// See https://aka.ms/new-console-template for more information

if (args is [{ } source, { } target])
{
    new OpenApiPreprocessor(source, target).Preprocess();
}
else
{
    Console.WriteLine("Usage: <source> <target>");
}
