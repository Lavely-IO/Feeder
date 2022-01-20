using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LavelyIO.Feeder.Models
{
    public class SourcePackage
    {
        public SourcePackage(string pkName, string minVerVersion, string? updateToVersion = null, bool shouldUpdate = false)
        {
            Name = pkName;
            Version = minVerVersion;
            UpdateToVersion = updateToVersion;
            ShouldUpdate = shouldUpdate;
        }

        public string Name { get; set; }

        public string Version { get; set; }

        public string UpdateToVersion { get; set; }

        public bool ShouldUpdate { get; set; }
    }
}