namespace MinimalKafka.Serializers;

internal static class Utf8Constants
{
     public static ReadOnlySpan<byte> BOM => [0xEF, 0xBB, 0xBF];
}

