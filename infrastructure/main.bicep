// ============================================================================
// Azure Static Web App - Bicep Template
// ============================================================================
// This template creates all Azure resources needed for 10xJournal deployment
// ============================================================================

@description('Environment name (e.g., dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'prod'

@description('Location for all resources')
param location string = resourceGroup().location

@description('GitHub repository URL (e.g., https://github.com/username/10xJournal)')
param repositoryUrl string

@description('GitHub repository branch to deploy')
param repositoryBranch string = 'main'

@description('GitHub Personal Access Token for deployment (optional - can be added later)')
@secure()
param repositoryToken string = ''

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

@description('Tags to apply to all resources')
param tags object = {
  Project: '10xJournal'
  Environment: environment
  ManagedBy: 'Bicep'
}

// ============================================================================
// Variables
// ============================================================================

var resourcePrefix = '10xjournal'
var staticWebAppName = '${resourcePrefix}-swa-${environment}'
var appInsightsName = '${resourcePrefix}-ai-${environment}'

// ============================================================================
// Resources
// ============================================================================

// Application Insights (for monitoring)
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: null
    IngestionMode: 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Static Web App
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: repositoryBranch
    repositoryToken: repositoryToken
    buildProperties: {
      appLocation: '/10xJournal.Client'
      apiLocation: ''
      outputLocation: 'wwwroot'
      appBuildCommand: ''
      apiBuildCommand: ''
      skipGithubActionWorkflowGeneration: true // We use our own workflows
    }
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// Link Application Insights to Static Web App (for monitoring)
resource staticWebAppSettings 'Microsoft.Web/staticSites/config@2023-01-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.properties.ConnectionString
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Static Web App default hostname')
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname

@description('Static Web App name')
output staticWebAppName string = staticWebApp.name

@description('Static Web App resource ID')
output staticWebAppId string = staticWebApp.id

@description('Application Insights Instrumentation Key')
output appInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey

@description('Application Insights Connection String')
output appInsightsConnectionString string = applicationInsights.properties.ConnectionString

@description('Deployment token for GitHub Actions (retrieve separately using Azure CLI)')
output deploymentTokenNote string = 'Run: az staticwebapp secrets list --name ${staticWebAppName} --query "properties.apiKey" -o tsv'
