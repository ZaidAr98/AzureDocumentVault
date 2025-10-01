@description('App Service name')
param appServiceName string
@description('Application Insights instrumentation key')
param appInsightsInstrumentationKey string
@description('Cosmos DB endpoint')
param cosmosDbEndpoint string
@description('Cosmos DB key')
param cosmosDbKey string
@description('Storage account name')
param storageAccountName string
@description('Storage account key')
@secure()
param storageAccountKey string
@description('Function App host name')
param functionAppHostName string
@description('Location for all resources')
param location string
@description('Tags for the resources')
param tags object
@description('CDN endpoint URL')
param cdnEndpoint string = ''
@description('Enable CDN')
param enableCdn bool = true
@description('CDN security key')
param cdnSecurityKey string = 'claude'
@description('CDN cache TTL in minutes')
param cdnCacheTtlMinutes int = 60
@description('Container Registry login server')
param containerRegistryLoginServer string

@description('Container image name')
param containerImageName string = 'docvault-web'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${appServiceName}-plan'
  location: location
  tags: tags
  sku: {
    name: 'P1V3'
    tier: 'PremiumV3'
  }
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistryLoginServer}/${containerImageName}:latest'
      acrUseManagedIdentityCreds: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'Logging_LogLevel_Default'
          value: 'Information'
        }
        {
          name: 'Logging_LogLevel_Microsoft.AspNetCore'
          value: 'Warning'
        }
        {
          name: 'AllowedHosts'
          value: '*'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'Azure_CosmosDB_ConnectionString'
          value: 'AccountEndpoint=${cosmosDbEndpoint};AccountKey=${cosmosDbKey};'
        }
        {
          name: 'Azure_CosmosDB_DatabaseName'
          value: 'DocumentVaultDB'
        }
        {
          name: 'Azure_CosmosDB_ContainerName'
          value: 'documents'
        }
        {
          name: 'Azure_BlobStorage_ConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccountKey}'
        }
        {
          name: 'Azure_BlobStorage_ContainerName'
          value: 'documents'
        }
        {
          name: 'Cdn_EnableCdn'
          value: string(enableCdn)
        }
        {
          name: 'Cdn_CdnEndpoint'
          value: cdnEndpoint
        }
        {
          name: 'Cdn_BaseUrl'
          value: 'https://${cdnEndpoint}'
        }
        {
          name: 'Cdn_SecurityKey'
          value: cdnSecurityKey
        }
        {
          name: 'Cdn_DefaultCacheTtlMinutes'
          value: string(cdnCacheTtlMinutes)
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsightsInstrumentationKey}'
        }
        {
          name: 'ApiSettings_FunctionBaseUrl'
          value: 'https://${functionAppHostName}'
        }
        {
  name: 'WEBSITES_PORT'
  value: '8080'
}
{
  name: 'DOCKER_REGISTRY_SERVER_URL'
  value: 'https://${containerRegistryLoginServer}'
}
{
  name: 'DOCKER_CUSTOM_IMAGE_NAME'
  value: 'DOCKER|${containerRegistryLoginServer}/${containerImageName}:latest'
}
      ]
    }
    httpsOnly: true
  }
}

resource storageAccountResource 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageAccountName
}

// Grant app service access to storage account
resource appServiceStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroup().id, appService.name, 'StorageBlobDataContributor')
  scope: storageAccountResource
  properties: {
    principalId: appService.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe') // Storage Blob Data Contributor
    principalType: 'ServicePrincipal'
  }
}
resource containerRegistryResource 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: split(containerRegistryLoginServer, '.')[0]
}

resource appServiceAcrRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(resourceGroup().id, appService.name, 'AcrPull')
  scope: containerRegistryResource
  properties: {
    principalId: appService.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // AcrPull
    principalType: 'ServicePrincipal'
  }
}

output id string = appService.id
output name string = appService.name
output hostName string = appService.properties.defaultHostName