@description('Container App name')
param containerAppName string

@description('Container App Environment name')
param containerAppEnvName string

@description('Location for all resources')
param location string

@description('Container Registry login server')
param containerRegistryLoginServer string

@description('Container Registry admin username')
param containerRegistryUsername string

@description('Container Registry admin password')
@secure()
param containerRegistryPassword string

@description('Container image name')
param containerImageName string = ''

@description('Application Insights instrumentation key')
@secure()
param appInsightsInstrumentationKey string

@description('Cosmos DB endpoint')
param cosmosDbEndpoint string

@description('Cosmos DB key')
@secure()
param cosmosDbKey string

@description('Storage account name')
param storageAccountName string

@description('Storage account key')
@secure()
param storageAccountKey string

@description('Function App hostname')
param functionAppHostName string

@description('CDN endpoint URL')
param cdnEndpoint string = ''

@description('CDN security key')
@secure()
param cdnSecurityKey string = 'claude'

@description('CDN default cache TTL in minutes')
param cdnCacheTtlMinutes int = 60

@description('Tags for the resources')
param tags object

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${containerAppName}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: containerAppEnvName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: containerAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        transport: 'auto'
      }
      secrets: [
        {
          name: 'cosmos-connection-string'
          value: 'AccountEndpoint=${cosmosDbEndpoint};AccountKey=${cosmosDbKey};'
        }
        {
          name: 'storage-connection-string'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey};EndpointSuffix=core.windows.net'
        }
        {
          name: 'appinsights-key'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'registry-password'
          value: containerRegistryPassword
        }
        {
          name: 'cdn-security-key'
          value: cdnSecurityKey
        }
      ]
      registries: [
        {
          server: containerRegistryLoginServer
          username: containerRegistryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web-app'
            image: !empty(containerImageName) ? '${containerRegistryLoginServer}/${containerImageName}:latest' : 'mcr.microsoft.com/dotnet/samples:aspnetapp'          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'Azure_CosmosDB_ConnectionString'
              secretRef: 'cosmos-connection-string'
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
              secretRef: 'storage-connection-string'
            }
            {
              name: 'Azure_BlobStorage_ContainerName'
              value: 'documents'
            }
            {
              name: 'FunctionApp_HostName'
              value: 'https://${functionAppHostName}'
            }
            {
              name: 'ApplicationInsights_InstrumentationKey'
              secretRef: 'appinsights-key'
            }
            {
              name: 'Cdn_EnableCdn'
              value: !empty(cdnEndpoint) ? 'true' : 'false'
            }
            {
              name: 'Cdn_CdnEndpoint'
              value: cdnEndpoint
            }
            {
              name: 'Cdn_BaseUrl'
              value: !empty(cdnEndpoint) ? 'https://${cdnEndpoint}' : ''
            }
            {
              name: 'Cdn_SecurityKey'
              secretRef: 'cdn-security-key'
            }
            {
              name: 'Cdn_DefaultCacheTtlMinutes'
              value: string(cdnCacheTtlMinutes)
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          {
            name: 'http-scale-rule'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn