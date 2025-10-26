
# Deploy infrastructure
./infrastructure/deploy.sh prod

# Delete infrastructure  
./infrastructure/cleanup.sh prod

# Redeploy (fresh start)
./infrastructure/cleanup.sh prod && ./infrastructure/deploy.sh prod



dealen@deb13j:~/Dev/10xDevs/10xJournal$ az group create --name rg-10xJournal-prod --location westeurope
{
  "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod",
  "location": "westeurope",
  "managedBy": null,
  "name": "rg-10xJournal-prod",
  "properties": {
    "provisioningState": "Succeeded"
  },
  "tags": null,
  "type": "Microsoft.Resources/resourceGroups"
}

dealen@deb13j:~/Dev/10xDevs/10xJournal$ az deployment group create --resource-group rg-10xJournal-prod --template-file infrastructure/main.bicep --parameters infrastructure/main.parameters.json 
{
  "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Resources/deployments/main",
  "location": null,
  "name": "main",
  "properties": {
    "correlationId": "1f312a72-317f-4dd7-8431-f0c11af4769b",
    "debugSetting": null,
    "dependencies": [
      {
        "dependsOn": [
          {
            "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Insights/components/10xjournal-ai-prod",
            "resourceGroup": "rg-10xJournal-prod",
            "resourceName": "10xjournal-ai-prod",
            "resourceType": "Microsoft.Insights/components"
          },
          {
            "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Web/staticSites/10xjournal-swa-prod",
            "resourceGroup": "rg-10xJournal-prod",
            "resourceName": "10xjournal-swa-prod",
            "resourceType": "Microsoft.Web/staticSites"
          },
          {
            "apiVersion": "2020-02-02",
            "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Insights/components/10xjournal-ai-prod",
            "resourceGroup": "rg-10xJournal-prod",
            "resourceName": "10xjournal-ai-prod",
            "resourceType": "Microsoft.Insights/components"
          }
        ],
        "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Web/staticSites/10xjournal-swa-prod/config/appsettings",
        "resourceGroup": "rg-10xJournal-prod",
        "resourceName": "10xjournal-swa-prod/appsettings",
        "resourceType": "Microsoft.Web/staticSites/config"
      }
    ],
    "diagnostics": null,
    "duration": "PT14.7024589S",
    "error": null,
    "extensions": null,
    "mode": "Incremental",
    "onErrorDeployment": null,
    "outputResources": [
      {
        "apiVersion": null,
        "extension": null,
        "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Insights/components/10xjournal-ai-prod",
        "identifiers": null,
        "resourceGroup": "rg-10xJournal-prod",
        "resourceType": null
      },
      {
        "apiVersion": null,
        "extension": null,
        "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Web/staticSites/10xjournal-swa-prod",
        "identifiers": null,
        "resourceGroup": "rg-10xJournal-prod",
        "resourceType": null
      },
      {
        "apiVersion": null,
        "extension": null,
        "id": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Web/staticSites/10xjournal-swa-prod/config/appsettings",
        "identifiers": null,
        "resourceGroup": "rg-10xJournal-prod",
        "resourceType": null
      }
    ],
    "outputs": {
      "appInsightsConnectionString": {
        "type": "String",
        "value": "InstrumentationKey=cbdbb47d-a7cc-45bf-97d6-db87d44b7755;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=d1ee7e06-5518-49ee-aa06-ffe6479c2569"
      },
      "appInsightsInstrumentationKey": {
        "type": "String",
        "value": "cbdbb47d-a7cc-45bf-97d6-db87d44b7755"
      },
      "deploymentTokenNote": {
        "type": "String",
        "value": "Run: az staticwebapp secrets list --name 10xjournal-swa-prod --query \"properties.apiKey\" -o tsv"
      },
      "staticWebAppDefaultHostname": {
        "type": "String",
        "value": "ambitious-sea-071c98303.3.azurestaticapps.net"
      },
      "staticWebAppId": {
        "type": "String",
        "value": "/subscriptions/049980ca-52f9-4e22-b3c1-3eade26bde66/resourceGroups/rg-10xJournal-prod/providers/Microsoft.Web/staticSites/10xjournal-swa-prod"
      },
      "staticWebAppName": {
        "type": "String",
        "value": "10xjournal-swa-prod"
      }
    },
    "parameters": {
      "environment": {
        "type": "String",
        "value": "prod"
      },
      "location": {
        "type": "String",
        "value": "westeurope"
      },
      "repositoryBranch": {
        "type": "String",
        "value": "main"
      },
      "repositoryToken": {
        "type": "SecureString"
      },
      "repositoryUrl": {
        "type": "String",
        "value": "https://github.com/YOUR_USERNAME/10xJournal"
      },
      "sku": {
        "type": "String",
        "value": "Free"
      },
      "tags": {
        "type": "Object",
        "value": {
          "Environment": "prod",
          "ManagedBy": "Bicep",
          "Owner": "Jakub Marcickiewicz",
          "Project": "10xJournal"
        }
      }
    },
    "parametersLink": null,
    "providers": [
      {
        "id": null,
        "namespace": "Microsoft.Insights",
        "providerAuthorizationConsentState": null,
        "registrationPolicy": null,
        "registrationState": null,
        "resourceTypes": [
          {
            "aliases": null,
            "apiProfiles": null,
            "apiVersions": null,
            "capabilities": null,
            "defaultApiVersion": null,
            "locationMappings": null,
            "locations": [
              "westeurope"
            ],
            "properties": null,
            "resourceType": "components",
            "zoneMappings": null
          }
        ]
      },
      {
        "id": null,
        "namespace": "Microsoft.Web",
        "providerAuthorizationConsentState": null,
        "registrationPolicy": null,
        "registrationState": null,
        "resourceTypes": [
          {
            "aliases": null,
            "apiProfiles": null,
            "apiVersions": null,
            "capabilities": null,
            "defaultApiVersion": null,
            "locationMappings": null,
            "locations": [
              "westeurope"
            ],
            "properties": null,
            "resourceType": "staticSites",
            "zoneMappings": null
          },
          {
            "aliases": null,
            "apiProfiles": null,
            "apiVersions": null,
            "capabilities": null,
            "defaultApiVersion": null,
            "locationMappings": null,
            "locations": [
              null
            ],
            "properties": null,
            "resourceType": "staticSites/config",
            "zoneMappings": null
          }
        ]
      }
    ],
    "provisioningState": "Succeeded",
    "templateHash": "21352898911055200",
    "templateLink": null,
    "timestamp": "2025-10-26T12:28:33.289976+00:00",
    "validatedResources": null,
    "validationLevel": null
  },
  "resourceGroup": "rg-10xJournal-prod",
  "tags": null,
  "type": "Microsoft.Resources/deployments"
}
dealen@deb13j:~/Dev/10xDevs/10xJournal$ 