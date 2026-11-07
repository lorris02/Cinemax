namespace Cinemax.Services
{
    // Runs after startup so the site is available while movies and reviews load.
    public class CatalogueSync : BackgroundService
    {
        private const int Target = 300;
        private const int GrowBelow = 100;
        private const int MaxPages = 25;
        private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(150);
        private readonly IServiceProvider _services;
        private readonly ILogger<CatalogueSync> _logger;

        public CatalogueSync(IServiceProvider services, ILogger<CatalogueSync> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            using var scope = _services.CreateScope();
            if (!scope.ServiceProvider.GetRequiredService<ITmdbClient>().Enabled)
            {
                return;
            }
            var importer = scope.ServiceProvider.GetRequiredService<MovieImporter>();
            var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
            try
            {
                await importer.RefreshDetailsAsync(Pause, stop);
                // A floor below the target keeps deleted movies from returning after a restart.
                if (db.Movies.Count() < GrowBelow)
                {
                    await importer.GrowCatalogueAsync(Target, MaxPages, Pause, stop);
                }
                _logger.LogInformation("Catalogue holds {Count} movies.", db.Movies.Count());
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning("Catalogue sync stopped: {Reason}", e.GetType().Name);
            }
        }
    }
}
