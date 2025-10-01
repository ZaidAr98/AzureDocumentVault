@description('Function App name')
param functionAppName string

@description('Application Insights resource name')
param appInsightsName string

@description('Resource group containing Application Insights')
param appInsightsResourceGroup string = resourceGroup().name

@description('Existing Cosmos DB account name')
param existingCosmosAccountName string

@description('Resource group containing the Cosmos DB (if different from current)')
param cosmosResourceGroupName string = resourceGroup().name

@description('Location for all resources')
param location string

@description('Tags for the resources')
param tags object

// Reference existing Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
  scope: resourceGroup(appInsightsResourceGroup)
}

// Reference existing Cosmos DB account
resource existingCosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' existing = {
  name: existingCosmosAccountName
  scope: resourceGroup(cosmosResourceGroupName)
}

var functionStorageName = 'zfn${take(uniqueString(resourceGroup().id, functionAppName), 5)}'

// Create minimal storage account required by Azure Functions runtime
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: functionStorageName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${functionAppName}-plan'
  location: location
  tags: tags
  sku: {
    name: 'EP1' 
    tier: 'ElasticPremium'
  }
  properties: {
    reserved: true
    maximumElasticWorkerCount: 20
  }
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0' 
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      cors: {
        allowedOrigins: [
          '*'
        ]
      }
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString  
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'  
        }
        {
          name: 'CosmosDB_DatabaseName'
          value: 'DocumentVaultDB'
        }
        {
          name: 'CosmosDB_ContainerName'
          value: 'links'
        }
        {
          name: 'CosmosDB_ConnectionString'
          value: 'AccountEndpoint=${existingCosmosAccount.properties.documentEndpoint};AccountKey=${existingCosmosAccount.listKeys().primaryMasterKey};'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'AzureFunctionsJobHost__logging__logLevel__default'
          value: 'Information'
        }
      ]
    }
    httpsOnly: true
  }
}

output functionAppId string = functionApp.id
output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output cosmosEndpoint string = existingCosmosAccount.properties.documentEndpoint
output appInsightsConnectionString string = appInsights.properties.ConnectionString