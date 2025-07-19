using    DotNetCoreOAuth2;
using Microsoft.Identity.Client;
using WebAppTest;
using WebAppTest.Support;

var builder = WebApplication.CreateBuilder(args);

AddOverrideParentConfig(builder);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRazorPages();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<CodeFlowHelper>();
builder.Services.AddSingleton(
    sp => new WellKnownConfigurationHandler(sp.GetService<IHttpClientFactory>()!, "default"));

var oauth2Settings = ConfigurationHelper.ConfigureSetting<OAuth2Settings>(builder.Services, builder.Configuration, "OAuth2");

IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
    .Create(oauth2Settings.ClientId)
    .WithClientSecret(oauth2Settings.ClientSecret)
    .WithAuthority(new Uri(oauth2Settings.Authority))
    .WithRedirectUri("https://localhost:7044/msal-oauth2/get-token")
    .Build();

builder.Services.AddSingleton(cca);

builder.Services.AddHttpClient("default");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.Run();

void AddOverrideParentConfig(WebApplicationBuilder builder)
{
    var directoryToCheck = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent;
    Console.WriteLine("Starting to look override file DotNetCoreOauth2.json starting from directory {0}", AppDomain.CurrentDomain.BaseDirectory);
    while (directoryToCheck != null)
    {
        string overrideFile = Path.Combine(directoryToCheck.FullName, "DotNetCoreOauth2.json");
        if (File.Exists(overrideFile))
        {
            Console.WriteLine("Found override configuration file: {0}", overrideFile);
            builder.Configuration.AddJsonFile(overrideFile);
        }
        directoryToCheck = directoryToCheck.Parent;
    }

    Console.WriteLine("No override configuration file DotNetCoreOauth2.json found starting from directory {0}", AppDomain.CurrentDomain.BaseDirectory);
}