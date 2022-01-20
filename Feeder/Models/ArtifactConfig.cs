using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LavelyIO.Feeder.Models
{
    /// <summary>
    /// The DevOps configuration Details
    /// </summary>
    public class ArtifactConfig
    {
        public ArtifactConfig()
        { }

        public ArtifactConfig(ArtifactConfig config)
        {
            DevOpsPAT = config.DevOpsPAT;
            PublishSecret = config.PublishSecret;
            PublishUrl = config.PublishUrl;
            Collection = config.Collection;
            Project = config.Project;
            BuildId = config.BuildId;
            ArtifactName = config.ArtifactName;
            ServerName = config.ServerName;
            FeedUrl = config.FeedUrl;
            FeedSourceId = config.FeedSourceId;
        }

        public string DevOpsPAT { get; set; } = String.Empty;
        public string PublishSecret { get; set; } = String.Empty;
        public string PublishUrl { get; set; } = String.Empty;
        public string Collection { get; set; } = String.Empty;
        public string Project { get; set; } = String.Empty;
        public string BuildId { get; set; } = String.Empty;
        public string ArtifactName { get; set; } = String.Empty;
        public string ServerName { get; set; } = String.Empty;
        public string FeedUrl { get; set; } = String.Empty;
        public string FeedSourceId { get; set; } = String.Empty;

        public string DevOpsBaseUrl
        {
            get => $"https://{ServerName}/{Collection}";
        }

        public string FeedPackagesUrl
        {
            get => $"{DevOpsBaseUrl}/{Project}/_apis/Packaging/Feeds/{FeedSourceId}/Packages";
        }

        public string LatestArtifactsUrl
        {
            get => $"{DevOpsBaseUrl}/{Project}/_apis/build/builds/{BuildId}/artifacts?artifactName={ArtifactName}&api-version=5.0";
        }
    }
}