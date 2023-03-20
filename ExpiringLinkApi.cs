using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Identity;
using EON.Models;
using System.Collections.Generic;

namespace EON.Function
{
  public static class ExpiringLinkApi
  {
    const string TABLE_NAME = "ExpiringLinks";
    const string DEFAULT_EXPIRED_HTML = "<p>This link has expired.</p>";

    [FunctionName("create")]
    public static async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] CreateExpiringLinkRequest createExpiringLinkRequest, HttpRequest req,
        ILogger log)
    {
      log.LogInformation("Request to Create Expiring Link.");

      // Validate request body
      var errors = new Dictionary<string, string>();
      if (createExpiringLinkRequest == null)
      {
        return new BadRequestObjectResult("Request Body is required");
      }
      if (String.IsNullOrEmpty(createExpiringLinkRequest.url))
      {
        errors.Add("url", "Url is required");
      }
      if (createExpiringLinkRequest.expiresIn <= 0)
      {
        errors.Add("expiresIn", "Expiration (in minutes) must be greater than 0");
      }

      if (errors.Count > 0)
      {
        return new BadRequestObjectResult(errors);
      }

      // Generate a GUID representing the lookup key for this new expiring url
      string linkIdentifier = Guid.NewGuid().ToString();

      // Generate expirationDate, based on our current time plus the expiresIn minutes supplied
      DateTime expirationDate = DateTime.UtcNow.AddMinutes(createExpiringLinkRequest.expiresIn);

      // Add expiring link to table
      var tableEntity = new ExpiringLinkTableEntity()
      {
        PartitionKey = linkIdentifier,
        RowKey = string.Empty,
        Url = createExpiringLinkRequest.url,
        Expiration = expirationDate,
        ExpiresOnAccess = createExpiringLinkRequest.expiresOnAccess,
        ExpiredRedirectUrl = createExpiringLinkRequest.expiredRedirectUrl
      };

      var tableClient = new TableClient(new Uri(Environment.GetEnvironmentVariable("TABLE_SERVICE_URI")), TABLE_NAME, new DefaultAzureCredential());
      var addEntityResponse = await tableClient.AddEntityAsync(tableEntity);
      if (addEntityResponse.IsError)
      {
        log.LogError($"Error adding table entity: {addEntityResponse.ReasonPhrase}");
        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
      }

      // Return new expiring link, pointing to our GET API
      var response = new CreateExpiringLinkResponse() { url = $"{req.Scheme}://{req.Host.Value}/api/r/{linkIdentifier}" };

      return new OkObjectResult(response);
    }

    [FunctionName("get")]
    public static async Task<IActionResult> Get(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "r/{linkIdentifierGuid:guid}")] HttpRequest req, Guid linkIdentifierGuid,
    ILogger log)
    {
      string linkIdentifier = linkIdentifierGuid.ToString();
      log.LogInformation($"Request to Get Expiring Link: {linkIdentifier}");

      var tableClient = new TableClient(new Uri(Environment.GetEnvironmentVariable("TABLE_SERVICE_URI")), TABLE_NAME, new DefaultAzureCredential());
      var getEntityResponse = await tableClient.GetEntityAsync<ExpiringLinkTableEntity>(linkIdentifier, string.Empty);
      if (getEntityResponse == null || getEntityResponse.Value == null)
      {
        log.LogError($"Error retrieving table entity with primaryKey: {linkIdentifier}. Response: {getEntityResponse.ToString()}");

        log.LogInformation("Redirecting user to default redirect url");
        return new ContentResult
        {
          Content = DEFAULT_EXPIRED_HTML,
          ContentType = "text/html"
        };
      }

      // URL has expired, redirect the user
      if (DateTime.UtcNow > getEntityResponse.Value.Expiration)
      {
        if (!string.IsNullOrEmpty(getEntityResponse.Value.ExpiredRedirectUrl))
        {
          log.LogInformation("Link is expired, redirecting user to configured ExpiredRedirectUrl");
          return new RedirectResult(getEntityResponse.Value.ExpiredRedirectUrl);
        }
        else
        {
          log.LogInformation("Link is expired, redirecting user to default redirect url");

          return new ContentResult
          {
            Content = DEFAULT_EXPIRED_HTML,
            ContentType = "text/html"
          };
        }
      }

      if (getEntityResponse.Value.ExpiresOnAccess)
      {

        log.LogInformation("Link is configured to expireOnAccess.  Marking link as expired");
        // Update the table entity with 000 expiry date
        getEntityResponse.Value.Expiration = DateTime.UnixEpoch;
        var updateEntityResponse = await tableClient.UpdateEntityAsync(getEntityResponse.Value, Azure.ETag.All);
        if (updateEntityResponse.IsError)
        {
          log.LogError($"Error updating table entity with linkIdentifier: {linkIdentifier}: Reason: {updateEntityResponse.ReasonPhrase}");
        }
      }


      log.LogInformation("Redirecting user to configured url");
      return new RedirectResult(getEntityResponse.Value.Url);
    }


    [FunctionName("wake")]
    public static async Task<IActionResult> Wake(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "w")] HttpRequest req,
    ILogger log)
    {
      log.LogInformation($"Request to Wake Expiring Link Service");

      return new OkResult();
    }
  }


}
