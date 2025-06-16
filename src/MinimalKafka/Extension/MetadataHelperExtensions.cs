using MinimalKafka.Metadata;

namespace MinimalKafka.Extension;

internal static class MetadataHelperExtensions 
{
    public static string ClientId(this IReadOnlyList<object> metadata)
    {
        var meta = metadata.GetMetaData<IClientIdMetadata>()!;
        return meta.ClientId;
    }

    public static string GroupId(this IReadOnlyList<object> metadata)
    {
        var meta = metadata.GetMetaData<IGroupIdMetadata>()!;
        return meta.GroupId;
    }

    private static T? GetMetaData<T>(this IReadOnlyList<object> metaData)
        => metaData.OfType<T>().FirstOrDefault();

    public static bool IsAutoCommitEnabled(this IReadOnlyList<object> metaData)
        => metaData.GetMetaData<IAutoCommitMetaData>()?.Enabled ?? false;
}



