using Azure.Storage.Blobs;
using DocumentVault.Services.BlobStorageService;
using DocumentVault.Models;
using DocumentVault.Services.CdnService;
using DocumentVault.Services.CosmosDbService;
using DocumentVault.Services.DocumentService;
using DocumentVault.Services.LinkService;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

// Add Azure services
builder.Services.AddSingleton<CosmosClient>(provider =>
    new CosmosClient(builder.Configuration["Azure_CosmosDB_ConnectionString"]));

builder.Services.AddSingleton<BlobServiceClient>(provider =>
    new BlobServiceClient(builder.Configuration["Azure_BlobStorage_ConnectionString"]));

builder.Services.Configure<CdnOptions>(
    builder.Configuration.GetSection(CdnOptions.sectionName));

builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ILinkService, LinkService>();
builder.Services.AddScoped<ICdnService, CdnService>();

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Only use HTTPS redirection in development (Azure handles SSL termination)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configure CORS based on environment
app.UseCors(policy =>
{
    if (app.Environment.IsDevelopment())
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    }
    else
    {
        // TODO: Replace with your actual frontend domain
        policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    }
});

// Add root endpoint - API health check
app.MapGet("/", () => Results.Ok(new
{
    service = "DocumentVault API",
    status = "running",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        api = "/api"
    }
})).WithName("HealthCheck").WithTags("Health");

// Add a dedicated health endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
})).WithName("Health").WithTags("Health");

app.MapControllers();

app.Run();