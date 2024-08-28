// See https://aka.ms/new-console-template for more information

if (args is [{ } source, { } target])
{
    File.Copy(source, target);
}
else
{
    Console.WriteLine("Usage: <source> <target>");
}
