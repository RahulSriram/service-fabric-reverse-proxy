using System;
using System.Fabric;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using ReverseProxyService.Utilities;

namespace ReverseProxyService
{
  public class ReverseProxyMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ICommunicationClientFactory<HttpCommunicationClient> _httpCommunicationClientFactory;
    private const string Service_Fabric_Header_Key = "X-ServiceFabric";
    private const string Service_Fabric_Header_404_Value = "ResourceNotFound";
    private const int Default_Timeout_Seconds = 60;

    public ReverseProxyMiddleware(
      RequestDelegate next,
      RecyclableMemoryStreamManager recyclableMemoryStreamManager,
      FabricClient fabricClient,
      HttpClient httpClient)
    {
      _next = next;
      _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
      _httpCommunicationClientFactory = new HttpCommunicationClientFactory(httpClient, new ServicePartitionResolver(() => fabricClient));
    }

    public async Task Invoke(HttpContext context)
    {
      try
      {
        // Get the fabric:/app/service uri
        // and the rest of the request uri from the incoming request
        var (fabricUri, serviceRequestPath) = ServiceFabricHelper.GetFabricUriAndServiceRequestPath(context.Request.Path);
        // If fabric:/.. uri was malformed, then the request is bad
        if (string.IsNullOrEmpty(fabricUri))
        {
          context.Response.StatusCode = StatusCodes.Status400BadRequest;
          return;
        }

        // Get service fabric specific query params
        var queryCollection = context.Request.Query;
        var partitionKind = (queryCollection.TryGetValue("PartitionKind", out var partitionKindQueryValue))
          ? partitionKindQueryValue.ToString()
          : null;
        var partitionKey = (queryCollection.TryGetValue("PartitionKey", out var partitionKeyQueryValue))
          ? partitionKeyQueryValue.ToString()
          : null;
        var timeout = queryCollection.TryGetValue("Timeout", out var timeoutQueryValue)
          ? (int.TryParse(timeoutQueryValue.ToString(), out var timeoutIntValue)
            ? timeoutIntValue
            : Default_Timeout_Seconds)
          : Default_Timeout_Seconds;

        // Setup cancellation token
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

        // Get service partition key from query params
        var servicePartitionKey = ServiceFabricHelper.GetServicePartitionKey(
          partitionKind,
          partitionKey);

        // Perform address resolution with retry using the fabric:/app/service uri
        var servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
          _httpCommunicationClientFactory,
          new Uri(fabricUri),
          servicePartitionKey);

        System.Console.WriteLine($"{DateTime.Now}: InvokeWithRetryAsync before");
        var serviceResponse = await servicePartitionClient.InvokeWithRetryAsync(
          async (client) =>
          {
            // Setup proxy http request
            System.Console.WriteLine($"{DateTime.Now}: InvokeWithRetryAsync begin");
            var proxiedRequest = await context.Request.CreateHttpRequestMessageAsync(_recyclableMemoryStreamManager);
            var uriBuilder = new UriBuilder(client.Url)
            {
              Path = serviceRequestPath,
              Query = context.Request.QueryString.ToString()
            };
            proxiedRequest.RequestUri = uriBuilder.Uri;

            // Forward request
            var response = await client.HttpClient.SendAsync(
              proxiedRequest,
              HttpCompletionOption.ResponseHeadersRead,
              cancellationTokenSource.Token);

            System.Console.WriteLine($"{DateTime.Now}: InvokeWithRetryAsync end");
            if (response.StatusCode == HttpStatusCode.NotFound
              && !(response.Headers.TryGetValues(Service_Fabric_Header_Key, out var serviceFabricHeaderValue)
                && serviceFabricHeaderValue.Contains(Service_Fabric_Header_404_Value)))
            {
              // If the response is a 404,
              // and there is no X-ServiceFabric: ResourceNotFound header,
              // this means service fabric cannot resolve this action
              throw new FabricServiceNotFoundException();
            }

            return response;
          },
          cancellationTokenSource.Token);
        System.Console.WriteLine($"{DateTime.Now}: InvokeWithRetryAsync after");

        // Forward response back to caller
        context.Response.CopyFromResponseMessage(serviceResponse);
        var content = await serviceResponse.Content.ReadAsByteArrayAsync();
        await context.Response.Body.WriteAsync(content);
        return;
      }
      catch (FabricServiceNotFoundException e)
      {
        System.Console.WriteLine($"{DateTime.Now}: FabricServiceNotFoundException: {e}");
        context.Response.Headers.Add(Service_Fabric_Header_Key, Service_Fabric_Header_404_Value);
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
      }
      catch (TaskCanceledException e)
      {
        System.Console.WriteLine($"{DateTime.Now}: TaskCanceledException: {e}");
        context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
        return;
      }
      catch (Exception e)
      {
        System.Console.WriteLine($"{DateTime.Now}: Exception: {e}");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return;
      }
    }
  }
}
