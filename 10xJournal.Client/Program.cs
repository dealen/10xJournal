using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using _10xJournal.Client;
using _10xJournal.Client.Features.Authentication.Models;
using _10xJournal.Client.Features.Authentication.Register;
using _10xJournal.Client.Features.Authentication.Services;
using _10xJournal.Client.Features.Authentication.Logout;
using _10xJournal.Client.Features.JournalEntries.WelcomeEntry;
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
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<WelcomeEntryService>();

builder.Services.AddScoped(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var jsRuntime = provider.GetRequiredService<IJSRuntime>();
    
    var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Configuration value 'Supabase:Url' is missing.");
    var supabaseKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Configuration value 'Supabase:AnonKey' is missing.");

    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false,
        AutoRefreshToken = true
    };

    var client = new Supabase.Client(supabaseUrl, supabaseKey, options);
    
    // Set up session persistence using browser localStorage
    var sessionPersistence = new BlazorSessionPersistence(jsRuntime);
    client.Auth.SetPersistence(sessionPersistence);
    
    return client;
});

var app = builder.Build();

// Initialize Supabase client and restore session
var supabaseClient = app.Services.GetRequiredService<Supabase.Client>();
var jsRuntime = app.Services.GetRequiredService<IJSRuntime>();

// Load persisted session from localStorage
var sessionPersistence = new BlazorSessionPersistence(jsRuntime);
var session = await sessionPersistence.LoadSessionAsync();
if (session != null && !string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
{
    await supabaseClient.Auth.SetSession(session.AccessToken, session.RefreshToken);
}

// Initialize Supabase client to process any auth tokens in URL
await supabaseClient.InitializeAsync();

await app.RunAsync();
