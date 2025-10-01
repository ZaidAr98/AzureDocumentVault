using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DocumentVault.Functions.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Application Insights - YES, this is configured correctly
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Cosmos DB Client
        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration["CosmosDB_ConnectionString"];
            return new CosmosClient(connectionString);
        });

        // Your service
        services.AddScoped<IDeleteDownloadLink, DeleteDownloadLink>();
    })
    .Build();

host.Run();