
# SearchProxy Web API (ASP.NET Core)

This example hosts **SearchProxy** as a **.NET 8 Azure Functions (isolated)**. It forwards search requests to **Sitecore Search**, applies **OrderCloud visibility filters**, and returns **user-specific pricing**.

## Configuration

Update your `local.settings.json` file

```json
{
    "IsEncrypted": false,
    "Values": {
      "SearchProxySettings:SearchDomainId": "",
      "SearchProxySettings:SearchBaseApiUrl": "",
      "SearchProxySettings:SearchApiKey": "",
      "SearchProxySettings:OrderCloudMarketplaceId": "",

      "OrderCloudSettings:ApiUrl": "",
      "OrderCloudSettings:AuthUrl": "",
      "OrderCloudSettings:ClientId": "",
      "OrderCloudSettings:ClientSecret": "",

      "AzureWebJobsStorage": "UseDevelopmentStorage=true",
      "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
    }
}
```

| Property                                    | Description                                                                                                                    | Find In                                                                                | Example                                              |
|---------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------|------------------------------------------------------|
| SearchProxySettings:SearchDomainId          | Sitecore Search domain ID                                                                                                      | CEC → Administration → Domain Settings → Domain Information                            | 111111111                                            |
| SearchProxySettings:SearchBaseApiUrl        | Base API URL for discover endpoints                                                                                            | CEC → Developer Resources → API Access → API Hosts                                     | https://discover.sitecorecloud.io                    |
| SearchProxySettings:SearchApiKey            | The API key for your discover endpoints                                                                                        | CEC → Developer Resources → API Access → API Keys                                      | 01-xxxxxxxx-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx |
| SearchProxySettings:OrderCloudMarketplaceId | Optional. If provided, all requests must originate from this marketplace; otherwise, an UnauthorizedAccessException is thrown. | OrderCloud Portal >  API Console >  Top right popup                                    | my-marketplace-id                                    |
| OrderCloudSettings:ApiUrl                   | The base API URL for OrderCloud API                                                                                            | https://ordercloud.io/knowledge-base/ordercloud-supported-regions                      | https://useast-production.ordercloud.io              |
| OrderCloudSettings:AuthUrl                  | The base Auth URL for OrderCloud API                                                                                            | https://ordercloud.io/knowledge-base/ordercloud-supported-regions                      | https://useast-production.ordercloud.io              |
| OrderCloudSettings:ClientId                 | The API ClientID of your middleware                                                                                            | OrderCloud Portal > API Console > API Clients > [Middleware API CLient] > ID           | xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx                 |
| OrderCloudSettings:ClientSecret             | The API ClientSecret of your middleware                                                                                        | OrderCloud Portal > API Console > API Clients > [Middleware API CLient] > ClientSecret | xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx                 |
| FUNCTIONS_WORKER_RUNTIME                    | Azure Functions runtime identifier                                                                                             | Not applicable                                                                         | dotnet-isolated                                      |
| AzureWebJobsStorage                         | Connection string to your Azure storage account                                                                                | Azure Portal → Storage Accounts → [Account] → Access Keys                              | UseDevelopmentStorage=true                           |



## Running Locally

1. Ensure `local.settings.json` exists at the project root and includes the configuration shown above.
2. Run the project. If everything is configured correctly, you should see the host listening at something like:
    ```
    POST http://localhost:{port}/api/search
    ```


## HTTP endpoint

### Summary

- Route: POST /api/search
- Purpose: Proxies a Sitecore Search request and augments filters/results using OrderCloud (visibility + user-specific pricing).
- Authentication: Expects an OrderCloud UserInfo token (JWT) provided via the Authorization header.
- Content-Type: application/json
- RequestBody: Any valid Sitcore search request body

| Header        | Required | Description                                                                       |
|---------------|----------|-----------------------------------------------------------------------------------|
| Authorization | Yes      | Bearer {userinfo JWT}. An OrderCloud [UserInfo token](#ordercloud-userinfo-token) for the current user/session |

### OrderCloud UserInfo token

A UserInfo token contains user context required to return correct search results. This includes company membership, and assigned user groups that influence product visibility and pricing.

### How to retrieve a UserInfo Token

To obtain a UserInfo token, call the following endpoint using a valid OrderCloud access token for the user

```
POST {orderCloudApiUrl}/oauth/userinfo
```

The resulting JWT should be passed to this API via the Authorization header as a Bearer token
