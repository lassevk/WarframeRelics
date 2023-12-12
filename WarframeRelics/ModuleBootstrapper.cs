using System.Text;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

using LVK.Extensions.Bootstrapping;
using LVK.Extensions.Bootstrapping.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WarframeRelics;

public class ModuleBootstrapper : IModuleBootstrapper<HostApplicationBuilder, IHost>
{
    public void Bootstrap(IHostBootstrapper<HostApplicationBuilder, IHost> bootstrapper, HostApplicationBuilder builder)
    {
        builder.Services.AddMainEntrypoint<MainEntrypoint>();
        builder.Configuration.AddUserSecrets<Program>();

        builder.Services.AddSingleton(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            string? authJsonEncoding = configuration["Google:Auth"];
            if (authJsonEncoding == null)
                throw new InvalidOperationException();

            string authJson = Encoding.UTF8.GetString(Convert.FromBase64String(authJsonEncoding));
            return GoogleCredential.FromJson(authJson);
        });

        builder.Services.AddSingleton(sp =>
        {
            GoogleCredential credential = sp.GetRequiredService<GoogleCredential>();
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential, ApplicationName = "Warframe Relics"
            });
        });

        builder.Services.AddSingleton(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            string? id = configuration["Google:SpreadsheetId"];
            if (id == null)
                throw new InvalidOperationException();

            return new SpreadsheetId(id);
        });

        builder.Services.AddSingleton<WriteLimiter>();
        builder.Services.AddSingleton<WarframeRelicService>();
    }
}