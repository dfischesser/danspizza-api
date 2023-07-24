using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using Pizza;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure.Identity;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("SQLConnString"));
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyPolicy",
        policy =>
        {
            //policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            policy.WithOrigins("http://localhost:3000").AllowCredentials().WithHeaders(new string[] { "content-type", "Authentication" });
        });
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

SecretClientOptions options = new SecretClientOptions()
{
    Retry =
        {
            Delay= TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(16),
            MaxRetries = 5,
            Mode = RetryMode.Exponential
         }
};
var client = new SecretClient(new Uri("https://pizzakeys.vault.azure.net/"), new DefaultAzureCredential(), options);

KeyVaultSecret CSsecret = await client.GetSecretAsync("ConnString");
SqlTools.conStr = CSsecret.Value;

KeyVaultSecret JWTsecret = await client.GetSecretAsync("JWT");
SqlTools.JWT = JWTsecret.Value;

KeyVaultSecret AIsecret = await client.GetSecretAsync("AppInsights");
builder.Services.AddApplicationInsightsTelemetry(AIsecret.Value);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = "Dan's Pizza - danspizza.dev",
        ValidAudience = "Dan's Pizza User - danspizza.dev",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SqlTools.JWT))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            Debug.WriteLine(context.Request.Cookies["token"]);
            context.Token = context.Request.Cookies["token"];
            return Task.CompletedTask;
        }
    };
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MyPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
