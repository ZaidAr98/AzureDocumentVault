#!/bin/bash
set -e

# Default values
LOCATION="polandcentral"
RESOURCE_GROUP_NAME="documentvault-rg-as"
GITHUB_REPO_OWNER="ZaidAr98" 
GITHUB_REPO_NAME="AzureDocumentVault"
GITHUB_BRANCH="master"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --github-owner)
      GITHUB_REPO_OWNER="$2"
      shift 2
      ;;
    --github-repo)
      GITHUB_REPO_NAME="$2"
      shift 2
      ;;
    --github-branch)
      GITHUB_BRANCH="$2"
      shift 2
      ;;
    --rg)
      RESOURCE_GROUP_NAME="$2"
      shift 2
      ;;
    --location)
      LOCATION="$2"
      shift 2
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Validate required parameters
if [[ -z "$GITHUB_REPO_OWNER" || -z "$GITHUB_REPO_NAME" ]]; then
  echo "ERROR: GitHub repository owner and name are required."
  echo "Usage: $0 --github-owner <owner> --github-repo <repo-name> [--github-branch <branch>] [--rg <resource-group>] [--location <location>]"
  exit 1
fi

# Create resource group if it doesn't exist
if ! az group show --name $RESOURCE_GROUP_NAME &>/dev/null; then
  echo "Creating resource group $RESOURCE_GROUP_NAME in $LOCATION..."
  az group create --name $RESOURCE_GROUP_NAME --location $LOCATION
else
  echo "Resource group $RESOURCE_GROUP_NAME already exists."
fi

echo "Deploying infrastructure..."
DEPLOYMENT_NAME="deployment-$(date +%Y%m%d%H%M%S)"

# Deploy with correct parameters (removed environmentName, uniqueSuffix is auto-generated)
az deployment group create \
  --resource-group $RESOURCE_GROUP_NAME \
  --template-file bicep/main.bicep \
  --parameters location=$LOCATION \
              githubRepositoryOwner=$GITHUB_REPO_OWNER \
              githubRepositoryName=$GITHUB_REPO_NAME \
              githubBranch=$GITHUB_BRANCH \
  --name $DEPLOYMENT_NAME \
  --verbose

# Get deployment outputs
echo "Getting deployment outputs..."
APP_SERVICE_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs.appServiceName.value" \
  --output tsv)

FUNCTION_APP_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs.functionAppName.value" \
  --output tsv)

CONTAINER_REGISTRY_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs.containerRegistryName.value" \
  --output tsv)

STORAGE_ACCOUNT_NAME=$(az deployment group show \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs.storageAccountName.value" \
  --output tsv)

CDN_ENDPOINT_URL=$(az deployment group show \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $DEPLOYMENT_NAME \
  --query "properties.outputs.cdnEndpointUrl.value" \
  --output tsv)

echo "======= DEPLOYMENT SUMMARY ======="
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "Location: $LOCATION"
echo "App Service: $APP_SERVICE_NAME"
echo "Function App: $FUNCTION_APP_NAME"
echo "Container Registry: $CONTAINER_REGISTRY_NAME"
echo "Storage Account: $STORAGE_ACCOUNT_NAME"
echo "CDN Endpoint: $CDN_ENDPOINT_URL"
echo "=================================="

echo ""
echo "======= NEXT STEPS ======="
echo "1. Configure GitHub Actions workflows with these values:"
echo "   - AZURE_TENANT_ID: $(az account show --query tenantId -o tsv)"
echo "   - AZURE_SUBSCRIPTION_ID: $(az account show --query id -o tsv)"
echo "   - AZURE_RESOURCE_GROUP: $RESOURCE_GROUP_NAME"
echo "   - AZURE_APP_SERVICE_NAME: $APP_SERVICE_NAME"
echo "   - AZURE_FUNCTION_APP_NAME: $FUNCTION_APP_NAME"
echo ""
echo "2. Set up GitHub Actions secrets (if using service principal authentication):"
echo "   - Create a service principal with contributor access to the resource group"
echo "   - Add AZURE_CREDENTIALS secret to your GitHub repository"
echo ""
echo "3. Push your application code and workflows to trigger deployments"
echo "========================="