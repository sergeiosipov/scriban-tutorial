using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ScribanTutorial;
using ScribanTutorial.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient is singleton in WASM (single scope, single tab). This keeps it compatible
// with the singleton ContentService — DI strict scope validation otherwise refuses
// a singleton consumer of a scoped dependency.
builder.Services.AddSingleton(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.AddSingleton<ContentService>();
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<TemplateCache>();

builder.Services.AddSingleton(new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});

await builder.Build().RunAsync();
