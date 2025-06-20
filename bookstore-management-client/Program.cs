using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using bookstore_management_client;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(static sp => new HttpClient { BaseAddress = new Uri("http://localhost:5220/") });
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
