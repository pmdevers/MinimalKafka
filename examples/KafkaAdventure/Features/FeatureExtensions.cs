namespace KafkaAdventure.Features;

public static class FeatureExtensions
{
    extension(WebApplication app)
    {
        public WebApplication MapFeatures()
        {
            app.MapLocationsApi();
            app.MapLocations();
            app.MapInput();
            app.MapCommand();
            app.MapGo();
            app.MapHelp();
            app.MapLook();
            app.MapOutput();

            return app;
        }
    }
}
