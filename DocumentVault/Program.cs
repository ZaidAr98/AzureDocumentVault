using Azure.Storage.Blobs;
using DocumentVault.Services.BlobStorageService;
using DocumentVault.Services.CosmosDbService;
using DocumentVault.Services.DocumentService;
using DocumentVault.Services.LinkService;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();

// Add Azure Cosmos DB client
builder.Services.AddSingleton<CosmosClient>(provider =>
{
    var endpoint = builder.Configuration["CosmosDb:Endpoint"];
    var key = builder.Configuration["CosmosDb:Key"];

    var cosmosClientOptions = new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        HttpClientFactory = () => new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
    };

    return new CosmosClient(endpoint, key, cosmosClientOptions);
});

// Add Azure Blob Storage client
builder.Services.AddSingleton<BlobServiceClient>(provider =>
{
    var connectionString = builder.Configuration["Azure:BlobStorage:ConnectionString"];

  

    return new BlobServiceClient(connectionString);
});

// Add application services
builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ILinkService, LinkService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.MapControllers();

app.Run();