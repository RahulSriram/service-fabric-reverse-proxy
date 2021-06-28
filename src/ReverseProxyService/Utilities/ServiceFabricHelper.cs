using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using ReverseProxyService.Enums;

namespace ReverseProxyService.Utilities
{
  public static class ServiceFabricHelper
  {
    public static (string fabricUri, string serviceRequestPath) GetFabricUriAndServiceRequestPath(this Uri requestUri)
    {
      var uriBuilder = new UriBuilder(requestUri);
      return uriBuilder.Path.GetFabricUriAndServiceRequestPath();
    }

    public static (string fabricUri, string serviceRequestPath) GetFabricUriAndServiceRequestPath(this string requestPath)
    {
      var requestPathComponents = requestPath.Split('/', 4);
      // request uri paths start with a /, so requestPathComponents[0] is always empty
      var fabricApplicationName = requestPathComponents.ElementAtOrDefault(1);
      var fabricServiceName = requestPathComponents.ElementAtOrDefault(2);
      var serviceRequestPath = requestPathComponents.ElementAtOrDefault(3);

      if (string.IsNullOrEmpty(fabricApplicationName)
        || string.IsNullOrEmpty(fabricServiceName))
      {
        return (null, null);
      }

      return ($"fabric:/{fabricApplicationName}/{fabricServiceName}", serviceRequestPath);
    }

    public static ServicePartitionKey GetServicePartitionKey(string partitionKind, string partitionKey)
    {
      if (Enum.TryParse<PartitionKind>(partitionKind, out var partitionKindEnum))
      {
        switch (partitionKindEnum)
        {
          case PartitionKind.Int64Range:
            return long.TryParse(partitionKey, out var intPartitionKey) ? new ServicePartitionKey(intPartitionKey) : null;
          case PartitionKind.Named:
            return new ServicePartitionKey(partitionKey);
        }
      }

      return null;
    }
  }
}
