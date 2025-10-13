using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using _10xJournal.Client;
using _10xJournal.Client.Features.Authentication.Models;
using _10xJournal.Client.Features.Authentication.Register;
using _10xJournal.Client.Features.Authentication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var devUserOptions = builder.Configuration.GetSection("DevUser").Get<DevUserOptions>() ?? new DevUserOptions();
builder.Services.AddSingleton(Options.Create(devUserOptions));
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<IAuthService, SupabaseAuthService>();

builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Configuration value 'Supabase:Url' is missing.");
    var supabaseKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Configuration value 'Supabase:AnonKey' is missing.");

    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false,
        AutoRefreshToken = true
    };

    return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

var app = builder.Build();

// Initialize Supabase client to process any auth tokens in URL
var supabaseClient = app.Services.GetRequiredService<Supabase.Client>();
await supabaseClient.InitializeAsync();

await app.RunAsync();
