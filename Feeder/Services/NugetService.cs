using LavelyIO.Feeder.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Feed.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ILogger = NuGet.Common.ILogger;
using NullLogger = NuGet.Common.NullLogger;

namespace LavelyIO.Feeder.Services
{
    public class NugetService
    {
        private readonly HttpClient _httpClient;
        private readonly ServiceConfig _config;
        private readonly ArtifactConfig _artifactConfig;
        private readonly PackageSource _packageSource;
        private List<SourcePackage> _sources = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="config"></param>
        /// <param name="artifactConfig"></param>
        /// <param name="svcLogger"></param>
        public NugetService(HttpClient httpClient, ServiceConfig config, ArtifactConfig artifactConfig)
        {
            Console.WriteLine($"{typeof(Program).Assembly.FullName} has initialized.");
            // Setup Basic Auth for our http calls to external Nuget
            ICredentials creds = new NetworkCredential(config.UserName, config.Password);
            httpClient.BaseAddress = new Uri(config.SourceUri);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "basic",
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{config.UserName}:{config.Password}")));
            ;

            _httpClient = httpClient;
            _config = config;
            _artifactConfig = artifactConfig;
            _packageSource = new PackageSource(config.SourceUri)
            {
                Credentials = new PackageSourceCredential(
                    config.SourceUri,
                    config.UserName,
                    config.Password,
                    true,
                    null)
            };
        }

        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<List<SourcePackage>> CheckExternalMetadataTaskAsync()
        {
            _sources = await GetDevOpsFeedAsync().ConfigureAwait(true);
            Console.WriteLine("Checking Packages: " + _sources.Count.ToString());
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            List<SourcePackage> updateList = new();
            SourceRepository repository = Repository.Factory.GetCoreV2(_packageSource);

            PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();

            foreach (var pkg in _sources)
            {
                Console.WriteLine($"Checking {pkg}");
                IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
                    pkg.Name,
                    includePrerelease: true,
                    includeUnlisted: false,
                    cache,
                    logger,
                    cancellationToken);

                foreach (IPackageSearchMetadata package in packages)
                {
                    var oldPkg = _sources.FirstOrDefault(p => p.Name == package.Identity.Id);
                    var latestVer = package.Identity.Version.ToString();
                    bool markForUpdate = latestVer != oldPkg.Version;

                    if (markForUpdate)
                    {
                        updateList.Add(
                            new SourcePackage(
                                oldPkg.Name,
                                oldPkg.Version,
                                latestVer,
                                true));

                        Console.WriteLine($"Upstream Package, {oldPkg.Name}v{latestVer}, is marked for updating from {oldPkg.Version}");
                    }

                    Console.WriteLine($"Package: {package.Title} is up-to-date.");
                }
            }
            return updateList;
        }

        /// <summary>
        /// Gets the dev ops feed asynchronous.
        /// </summary>
        /// <returns></returns>
        public async Task<List<SourcePackage>> GetDevOpsFeedAsync()
        {
            Uri uri = new Uri(_artifactConfig.DevOpsBaseUrl);
            var feedId = _artifactConfig.FeedSourceId.ToString();
            VssBasicCredential credentials;
            VssConnection connection;
            List<SourcePackage> sources = new();

            try
            {
                // Since we're using a PAT, we don't need the username here
                credentials = new VssBasicCredential("", _artifactConfig.DevOpsPAT);
                connection = new VssConnection(uri, credentials);
                sources = new List<SourcePackage>();

                try
                {
                    using FeedHttpClient client2 = connection.GetClient<FeedHttpClient>();
                    List<Package> pkgs = await client2.GetPackagesAsync(feedId: feedId);

                    // If set to download all missing versions, grab whats not found in your feed
                    if (_config.AllVersions)
                    {
                        pkgs.ForEach((pk) =>
                        {
                            SourcePackage pkToUpdate = BuildVersionRef(pk);
                            sources.Add(pkToUpdate);
                        });
                    }
                    // Otherwise just get the latest
                    else
                    {
                        Package pk = pkgs[0];
                        SourcePackage pkToUpdate = BuildVersionRef(pk);
                        sources.Add(pkToUpdate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting packages: {ex.Message} : {ex.StackTrace}");

                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to Setup Vss Credentials and Connection. {msg} : {stack}", ex.Message, ex.StackTrace);
                throw;
            }

            return sources;
        }

        /// <summary>
        /// Updates the package asynchronous.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        public Task UpdatePackageAsync(string packageId, string version)
        {
            return DownloadPackageAsync(packageId, version);
        }

        /// <summary>
        /// Downloads the package asynchronous.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The version.</param>
        private async Task DownloadPackageAsync(string packageId, string version)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV2(_packageSource);
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false);
            NuGetVersion packageVersion = new NuGetVersion(version);

            string filePath = Path.Join("c:", "temp", $"{packageId}.{packageVersion.Version.ToString()}.nupkg");
            using (FileStream packageStream = File.Open(filePath, FileMode.OpenOrCreate))
            {
                await resource.CopyNupkgToStreamAsync(
                    packageId,
                    packageVersion,
                    packageStream,
                    cache,
                    logger,
                    cancellationToken);

                Console.WriteLine($"Downloaded package {packageId} {packageVersion} to {filePath}");
            }

            if (packageId.Contains("Reporting"))
            {
                Console.WriteLine($"Pushing Telerik Reporting");
                await PushUpdatedPackageAsync(filePath).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Pushes the updated package asynchronous.
        /// </summary>
        /// <param name="packagePath">The package path.</param>
        private async Task PushUpdatedPackageAsync(string packagePath)
        {
            ILogger logger = NullLogger.Instance;
            SourceRepository repository = Repository.Factory.GetCoreV3(_artifactConfig.FeedUrl);
            PackageUpdateResource resource = await repository.GetResourceAsync<PackageUpdateResource>();

            string apiKey = "az";

            await resource.Push(
                packagePath,
                null,
                5 * 60,
                false,
                packageSource => apiKey,
                packageSource => null,
                false,
                null,
                logger).ConfigureAwait(true);
        }

        /// <summary>
        /// Builds the version reference.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns></returns>
        private SourcePackage? BuildVersionRef(Package package)
        {
            var minPkgVer = package.Versions.ToList();
            var minVer = minPkgVer.FirstOrDefault();
            if (minVer != null)
            {
                return new SourcePackage(package.Name, minVer.Version);
            }
            else
            {
                Console.WriteLine("Unable to get version information from {pkg}", package.Name);
                return null;
            }
        }
    }
}