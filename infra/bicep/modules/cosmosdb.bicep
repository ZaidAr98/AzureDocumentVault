@description('Cosmos DB account name')
param cosmosAccountName string

@description('Location for all resources')
param location string

@description('Tags for the resources')
param tags object

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: cosmosAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    // Removed serverless capability to match CLI commands
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-08-15' = {
  parent: cosmosAccount
  name: 'DocumentVaultDB'
  properties: {
    resource: {
      id: 'DocumentVaultDB'
    }
  }
}

resource documentsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: cosmosDatabase
  name: 'documents'
  properties: {
    resource: {
      id: 'documents'
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}


resource linksContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: cosmosDatabase
  name: 'links'  
  properties: {
    resource: {
      id: 'links' 
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
      defaultTtl: 2592000 // 30 days - automatic cleanup!
    }
  }
}

output name string = cosmosAccount.name
output id string = cosmosAccount.id
output endpoint string = cosmosAccount.properties.documentEndpoint
@description('Reference to the Cosmos DB account resource for accessing keys')
output accountResourceId string = cosmosAccount.id
output primaryMasterKey string = cosmosAccount.listKeys().primaryMasterKey
