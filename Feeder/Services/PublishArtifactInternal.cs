using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LavelyIO.Feeder.Models;

namespace LavelyIO.Feeder.Services
{
    public class PublishArtifactInternal
    {
        private readonly HttpClient _httpClient;
        private readonly ArtifactConfig _config;

        public PublishArtifactInternal(HttpClient httpClient, ArtifactConfig config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task RunAsync()
        {
            await Task.Delay(3000).ConfigureAwait(true);
        }
    }
}