
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StellaLearningBackend.API;
using StellaLearningBackend.Data;
using StellaLearningBackend.Models;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500 MB global limit
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500 MB
    // You might also need to increase ValueLengthLimit, KeyLengthLimit depending on your forms
});
// Add services to the container.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true, // Validate token expiration
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)) // Use ! to assert Key is not null
    };
});

builder.Services.AddHttpClient("GeminiClient", client =>
{
    // Set base address if applicable, or default headers
    // client.BaseAddress = new Uri("...");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (StellaLearningBackend/1.0)");
    client.Timeout = TimeSpan.FromSeconds(120); // Example: Set a timeout
});

// The DI container will automatically provide IConfiguration, ILogger<LargeLanguageManager>,
// and IHttpClientFactory to its constructor.
builder.Services.AddSingleton<LargeLanguageManager>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configure identity options if needed (e.g., password requirements)
    options.SignIn.RequireConfirmedAccount = true; // Set to true if email confirmation is needed
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders(); // Needed for things like password reset tokens

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => // Add JWT Auth to Swagger UI
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Use Http for Bearer
        BearerFormat = "JWT",
        Scheme = "Bearer" // Scheme is "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }});
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stella Learning API V1");
            options.RoutePrefix = string.Empty;
        });
}

//Used for real deployment
//app.UseHttpsRedirection();
//app.UseRouting();
// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>(); // Get a logger

    string[] roleNames = { "Admin", "User" }; // Add roles as needed
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            // Create the roles and seed them to the database
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Created role '{RoleName}' successfully.", roleName);
            }
            else
            {
                logger.LogError("Failed to create role '{RoleName}'. Errors: {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}

app.Run();
