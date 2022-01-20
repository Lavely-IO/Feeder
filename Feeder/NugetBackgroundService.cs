using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LavelyIO.Feeder.Models;
using LavelyIO.Feeder.Services;

namespace LavelyIO.Feeder
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" />
    public sealed class NugetBackgroundService : BackgroundService
    {
        private readonly NugetService _nugetService;
        private readonly ILogger<NugetBackgroundService> _logger;
        private ServiceConfig _config;
        private ArtifactConfig _artifactConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetBackgroundService"/> class on startup.
        /// </summary>
        /// <param name="nugetService">The nuget service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="artifactConfig">The artifact configuration.</param>
        /// <param name="config">The configuration.</param>
        public NugetBackgroundService(
            NugetService nugetService,
            ILogger<NugetBackgroundService> logger, ArtifactConfig artifactConfig, ServiceConfig config) =>
            (_nugetService, _logger, _artifactConfig, _config) = (nugetService, logger, artifactConfig, config);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<SourcePackage> updateList =
                        await _nugetService
                            .CheckExternalMetadataTaskAsync()
                            .ConfigureAwait(true);

                    if (updateList.Count > 0)
                    {
                        _logger.LogInformation($"A Total of {updateList.Count} packages will be updated");
                        foreach (SourcePackage update in updateList)
                        {
                            await _nugetService
                                .UpdatePackageAsync(update.Name, update.UpdateToVersion)
                                .ConfigureAwait(true);
                        }

                        _logger.LogInformation("Packages updated!");
                    }
                    else
                    {
                        _logger.LogInformation("Nothing to do!");
                    }

                    // TODO: Expose Timespan in appsettings
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Error: {ex.Message} :: {ex.StackTrace}");
                    _logger.LogError(ex, ex.Message);
                    break;
                }
            }
        }
    }
}