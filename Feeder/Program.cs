using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LavelyIO.Feeder;
using LavelyIO.Feeder.Models;
using LavelyIO.Feeder.Services;
using Microsoft.Extensions.Configuration;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "JAGC Worker Service";
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Obtain Credentials, Source Uri, and packagelist to check
        IConfiguration configuration = hostContext.Configuration;
        ServiceConfig config = configuration
            .GetSection("External")
            .Get<ServiceConfig>();

        ArtifactConfig artifactConfig = configuration
            .GetSection("InternalFeed")
            .Get<ArtifactConfig>();

        // Expose our config to our service's constructors

        services.AddSingleton(config);
        services.AddSingleton(artifactConfig);

        // Register our primary loop/ service
        services.AddHostedService<NugetBackgroundService>();

        // Register our Services and Utilities here
        services.AddHttpClient<NugetService>();
    })
    .Build();

await host.RunAsync();