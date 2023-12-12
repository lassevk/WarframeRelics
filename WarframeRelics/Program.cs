using LVK.Extensions.Bootstrapping.Console;

using Microsoft.Extensions.Hosting;

using WarframeRelics;

await HostEx.CreateApplication<ModuleBootstrapper>(args).RunAsync();