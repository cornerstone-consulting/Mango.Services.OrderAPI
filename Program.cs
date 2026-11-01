using AutoMapper;
using DotNetEnv;
using Mango.MessageBus;
using Mango.Services.OrderAPI;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Extensions;
using Mango.Services.OrderAPI.Service;
using Mango.Services.OrderAPI.Service.IService;
using Mango.Services.OrderAPI.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;

bool isRunningInContainer =
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

if (!isRunningInContainer)
{
    Env.Load(".env");
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var mangoOptions = new MangoOptions
{
    Secret =
        builder.Configuration["ApiSettings:Secret"]
        ?? string.Empty,

    Issuer =
        builder.Configuration["ApiSettings:Issuer"]
        ?? string.Empty,

    Audience =
        builder.Configuration["ApiSettings:Audience"]
        ?? string.Empty,

    DefaultConnection =
    (isRunningInContainer
        ? builder.Configuration["Docker:ConnectionStrings:DefaultConnection"]
        : builder.Configuration["Http:ConnectionStrings:DefaultConnection"])
    ?? string.Empty,

    ProductAPI =
    (isRunningInContainer
        ? builder.Configuration["Docker:ServiceUrls:ProductAPI"]
        : builder.Configuration["Http:ServiceUrls:ProductAPI"])
    ?? string.Empty
};

builder.Services.AddSingleton(
    Microsoft.Extensions.Options.Options.Create(mangoOptions));


// Add services to the container.
// TODO: Update auto-mapper

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(mangoOptions.DefaultConnection));


builder.Services.AddMessageBus(builder.Configuration);


IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BackendApiAuthenticationHttpClientHandler>();
/// Another way to configure Httpclient service.
/// The other way is shown in Web project
/// Later, BackednApiAuthentcaitionHttpClientHandler was added.
/// May be to acheive this in simple way, this way Httpclient was configured
builder.Services.AddHttpClient("Product", (serviceProvider, client) =>
{
    var settings = serviceProvider
        .GetRequiredService<IOptions<MangoOptions>>()
        .Value;

    client.BaseAddress = new Uri(settings.ProductAPI);
})
.AddHttpMessageHandler<BackendApiAuthenticationHttpClientHandler>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme, securityScheme: new OpenApiSecurityScheme()
    {
        //This name is shown in Authorize pop up of CouponAPI Swagger
        Name = "Authorization",
        Description = "Enter the Bearer Authorization string as following: `Bearer Generated-JWT-Token`",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {Type= ReferenceType.SecurityScheme,
                Id=JwtBearerDefaults.AuthenticationScheme}
            }, new string[] {} //TODO: Why this is empty here
        }
    });
});

//Adding Authentication
// Adding Authentication
var key = Encoding.ASCII.GetBytes(mangoOptions.Secret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateIssuer = true,
        ValidIssuer = mangoOptions.Issuer,

        ValidateAudience = true,
        ValidAudience = mangoOptions.Audience
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

ApplyMigration();

app.Run();

void ApplyMigration()
{
    using (var scope = app.Services.CreateScope())
    {
        var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}

public class MangoOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string DefaultConnection { get; set; } = string.Empty;
    public string ProductAPI { get; set; } = string.Empty;
}
