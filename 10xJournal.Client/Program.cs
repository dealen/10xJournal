using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using _10xJournal.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Configuration value 'Supabase:Url' is missing.");
    var supabaseKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Configuration value 'Supabase:AnonKey' is missing.");

    return new Supabase.Client(supabaseUrl, supabaseKey);
});

await builder.Build().RunAsync();
