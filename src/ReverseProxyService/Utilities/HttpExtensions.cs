using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace ReverseProxyService.Utilities
{
  public static class HttpExtensions
  {
    public static async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(this HttpRequest request, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
    {
      var uriBuilder = new UriBuilder
      {
        Scheme = request.Scheme,
        Host = request.Host.Host,
        Port = request.Host.Port.GetValueOrDefault(80),
        Path = request.Path.ToString(),
        Query = request.QueryString.ToString()
      };

      var clone = new HttpRequestMessage(new HttpMethod(request.Method), uriBuilder.Uri)
      {
        Content = await request.Body.CreateHttpContentAsync(recyclableMemoryStreamManager)
      };

      foreach (var header in request.Headers)
      {
        clone.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
      }

      return clone;
    }

    public static async Task<HttpContent> CreateHttpContentAsync(this Stream content, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
    {
      if (content == null)
      {
        return null;
      }

      var ms = recyclableMemoryStreamManager.GetStream();
      await content.CopyToAsync(ms).ConfigureAwait(false);
      ms.Position = 0;

      return new StreamContent(ms);
    }

    public static void CopyFromResponseMessage(this HttpResponse response, HttpResponseMessage serviceResponse)
    {
      response.StatusCode = (int)serviceResponse.StatusCode;
      foreach (var header in serviceResponse.Headers)
      {
        response.Headers[header.Key] = header.Value.ToArray();
      }

      foreach (var header in serviceResponse.Content.Headers)
      {
        response.Headers[header.Key] = header.Value.ToArray();
      }

      response.Headers.Remove("transfer-encoding");
    }
  }
}