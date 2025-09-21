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

// Add Azure services
builder.Services.AddSingleton<CosmosClient>(provider =>
    new CosmosClient(builder.Configuration["Azure:CosmosDB:ConnectionString"]));

builder.Services.AddSingleton<BlobServiceClient>(provider =>
    new BlobServiceClient(builder.Configuration["Azure:BlobStorage:ConnectionString"]));

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