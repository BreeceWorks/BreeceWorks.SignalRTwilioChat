using BreeceWorks.TwilioSignalRApp.Data;
using BreeceWorks.TwilioSignalRApp.Hubs;
using BreeceWorks.TwilioSignalRApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ISMSService, SMSService>(client =>
{
    client.BaseAddress = new Uri(configuration["SMSBaseUrl"]);
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
    //options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    //options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme,
      options =>
      {
          options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.Authority = "https://demo.duendesoftware.com";
          options.ClientId = "interactive.confidential";
          options.ClientSecret = "secret";
          //options.Authority = "https://localhost:44333";
          //options.ClientId = "bethanyspieshophr";
          //options.ClientSecret = "108B7B4F-BEFC-4DD2-82E1-7F025F0F75D0";
          options.ResponseType = "code";
          options.Scope.Clear();
          options.Scope.Add("openid");
          options.Scope.Add("profile");
          options.Scope.Add("api");

          //options.Scope.Add("openid");
          //options.Scope.Add("profile");
          //options.Scope.Add("email");
          //options.Scope.Add("bethanyspieshophrapi");
          options.Scope.Add("offline_access");
          //options.CallbackPath = ...
          options.SaveTokens = true;
          options.GetClaimsFromUserInfoEndpoint = true;
          options.TokenValidationParameters.NameClaimType = "given_name";
      })
    .AddJwtBearer(options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        //options.Authority = "https://localhost:44333";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
        options.RequireHttpsMetadata = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/chathub"))
                {
                    // Attempt to get a token from a query sting used by WebSocket
                    var accessToken = context.Request.Query["access_token"];

                    // If not present, extract the token from Authorization header
                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        accessToken = context.Request.Headers["Authorization"]
                            .ToString()
                            .Replace("Bearer ", "");
                    }

                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<TokenManager>();
builder.Services.AddSignalR(
    s =>
    {
        s.EnableDetailedErrors = true;
    });

builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapFallbackToPage("/_Host");

app.Run();
