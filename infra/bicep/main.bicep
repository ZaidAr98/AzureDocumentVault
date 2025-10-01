targetScope = 'resourceGroup'

@description('The Azure region for resources')
param location string = resourceGroup().location

@description('Unique suffix for names (truncated to ensure naming compliance)')
param uniqueSuffix string = take(uniqueString(resourceGroup().id), 4)

@description('GitHub repository owner (organization or username)')
param githubRepositoryOwner string

@description('GitHub repository name')
param githubRepositoryName string

@description('GitHub branch name for the deployments')
param githubBranch string = 'master'

var appName = 'zaiddocvault'
var uniqueAppName = '${appName}${uniqueSuffix}'  

var tags = {
  application: 'Document Vault'
}

// Resource names with length validation
var cosmosAccountName = 'zaidcosmos-${uniqueAppName}'                    
var storageAccountName = 'zaidst${uniqueAppName}'                         
var containerRegistryName = replace('zaidacr${uniqueAppName}', '-', '')  
var functionAppName = 'zaidfunc-${uniqueAppName}'                        
var appServiceName = 'zaidapp-${uniqueAppName}'                       

module appInsights 'modules/appinsights.bicep' = {
  name: 'appInsightsDeploy'
  params: {
    appInsightsName: 'ai-${uniqueAppName}'
    location: location
    tags: tags
  }
}

module containerRegistry 'modules/containerregistry.bicep' = {
  name: 'containerRegistryDeploy'
  params: {
    registryName: containerRegistryName
    location: location
    tags: tags
  }
}

module storageAccount 'modules/storage.bicep' = {
  name: 'storageAccountDeploy'
  params: {
    storageAccountName: storageAccountName
    location: location
    tags: tags
  }
}

module cosmosDb 'modules/cosmosdb.bicep' = {
  name: 'cosmosDbDeploy'
  params: {
    cosmosAccountName: cosmosAccountName
    location: location
    tags: tags
  }
}

module functionApp 'modules/function.bicep' = {
  name: 'functionAppDeploy'
  params: {
    functionAppName: functionAppName
     appInsightsName: appInsights.outputs.name
    appInsightsResourceGroup: resourceGroup().name
    existingCosmosAccountName: cosmosAccountName  
    cosmosResourceGroupName: resourceGroup().name  
    location: location
    tags: tags
  }
  dependsOn: [
    appInsights
    cosmosDb
  ]
}

module appService 'modules/appservice.bicep' = {
  name: 'appServiceDeploy'
  params: {
    appServiceName: appServiceName
    location: location
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    cosmosDbEndpoint: cosmosDb.outputs.endpoint
    cosmosDbKey: cosmosDb.outputs.primaryMasterKey 
    storageAccountName: storageAccount.outputs.name
  storageAccountKey: storageAccount.outputs.primaryKey  
  functionAppHostName: functionApp.outputs.functionAppHostName  
      containerRegistryLoginServer: containerRegistry.outputs.loginServer 
   containerImageName: 'docvault-web' 
    tags: tags
  }
  dependsOn: [
    appInsights
    cosmosDb
    storageAccount
    functionApp
      containerRegistry 
  ]
}

module cdn 'modules/cdn.bicep' = {
  name: 'cdnDeploy'
  params: {
    cdnProfileName: 'cdn-${uniqueAppName}'
    cdnEndpointName: 'endpoint-${uniqueAppName}'
    storageAccountHostName: storageAccount.outputs.primaryBlobEndpoint
    tags: tags
  }
  dependsOn: [
    storageAccount
  ]
}

// Outputs
output storageAccountName string = storageAccount.outputs.name
output cosmosDbAccountName string = cosmosDb.outputs.name
output functionAppName string = functionApp.outputs.functionAppName
output appServiceName string = appService.outputs.name
output cdnEndpointUrl string = cdn.outputs.cdnEndpointUrl
output appInsightsName string = appInsights.outputs.name
output containerRegistryName string = containerRegistry.outputs.name
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer