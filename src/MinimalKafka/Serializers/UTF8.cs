namespace Pmdevers.MinimalKafka.Serializers;

#pragma warning disable S101 // Types should be named in PascalCase
internal static class UTF8
{
    public static readonly byte[] BOM = [0xEF, 0xBB, 0xBF];
}

#pragma warning restore S101 // Types should be named in PascalCase