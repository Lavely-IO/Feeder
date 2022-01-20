using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LavelyIO.Feeder.Models
{
    /// <summary>
    /// External Sources Configuration Details
    /// </summary>
    public class ServiceConfig
    {
        public string UserName { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public string SourceUri { get; set; } = String.Empty;
        public bool AllVersions { get; set; } = true;
        public List<string> PackageList { get; set; } = new();
    }
}