using MinimalKafka;
using MinimalKafka.Stream;
namespace Examples.Join;

public record Aanvrager(string BcNummer);
public record Uitgangspunt(string Id, string EersteAanvragerId, string TweedeAanvragerId, Aanvrager? EersteAanvrager, Aanvrager? TweedeAanvrager);

public static class JoinExample
{

    public static void MapJoinExample(this IApplicationBuilder builder)
    {
        builder.MapStream<string, Aanvrager>("aanvragers")
            .Join<string, Uitgangspunt>("uitgangspunten")
            .On((aanvrager, uitgangspunt) =>
                uitgangspunt.EersteAanvragerId == aanvrager.BcNummer ||
                uitgangspunt.TweedeAanvragerId == aanvrager.BcNummer)
            .Into(async (c, result) =>
            {

                var (aanvrager, uitgangspunt) = result;

                var store = c.GetTopicStore("aanvragers");

                var eersteAanvrager = await store.FindByKey<string, Aanvrager>(uitgangspunt.EersteAanvragerId);
                var tweedeAamvrager = await store.FindByKey<string, Aanvrager>(uitgangspunt.TweedeAanvragerId);

                await c.ProduceAsync("uitgangspunten-met-aanvragers", uitgangspunt.Id, uitgangspunt with
                {
                    EersteAanvrager = eersteAanvrager,
                    TweedeAanvrager = tweedeAamvrager
                });
            });
    }
}
