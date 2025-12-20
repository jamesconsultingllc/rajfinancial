using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using RajFinancial.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure MSAL authentication with Entra External ID
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("email");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("offline_access");
});

// Configure authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    // Admin-only features (user management, system configuration)
    options.AddPolicy("RequireAdministrator", policy =>
        policy.RequireRole("Administrator"));

    // Advisor features (client management, portfolio operations)
    // Administrators can also perform advisor tasks
    options.AddPolicy("RequireAdvisor", policy =>
        policy.RequireRole("Advisor", "Administrator"));

    // Client features (view own portfolio, make transactions)
    // All authenticated users with any role can access client features
    options.AddPolicy("RequireClient", policy =>
        policy.RequireRole("Client", "Advisor", "Administrator"));

    // Any authenticated user
    options.AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
