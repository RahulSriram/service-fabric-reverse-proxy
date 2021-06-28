using System.Fabric;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;

namespace ReverseProxyService
{
  public class Startup
  {
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton<FabricClient>();
      services.AddSingleton<RecyclableMemoryStreamManager>();
      services.AddHttpClient();
    }

    public void Configure(IApplicationBuilder app)
    {
      app.UseDeveloperExceptionPage();
      app.UseMiddleware<ReverseProxyMiddleware>();
    }
  }
}