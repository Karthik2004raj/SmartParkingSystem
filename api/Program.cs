using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartParking.API.Data;
using SmartParking.API.Services;
using System.Text;

// ===================================================================
// APPLICATION ENTRY POINT
// This is a top-level statements file (no explicit Main method).
// It builds the web application, configures services, middleware, and starts the server.
// ===================================================================

try
{
    // Build the web application builder
    var builder = WebApplication.CreateBuilder(args);

    // -----------------------------------------------------------------
    // 1. SERVICE CONFIGURATION
    // Register services into the dependency injection container.
    // -----------------------------------------------------------------

    // Add controllers (API endpoints)
    builder.Services.AddControllers();

    // Enable Swagger/OpenAPI for API documentation (only in development)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // -----------------------------------------------------------------
    // 2. DATABASE CONFIGURATION
    // Configure Entity Framework Core to use SQLite with the connection string from appsettings.json
    // -----------------------------------------------------------------
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));

    // -----------------------------------------------------------------
    // 3. JWT AUTHENTICATION CONFIGURATION
    // Configure JWT bearer authentication with secret key, issuer, and audience.
    // The secret key is read from configuration; fallback to a default for development.
    // -----------------------------------------------------------------
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        // Fallback key (only for development; in production, always use a secure key from configuration)
        jwtKey = "YourSuperSecretKeyForJWTTokenGeneration2024!@#$%SuperSecretKey";
    }
    var key = Encoding.ASCII.GetBytes(jwtKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Allow HTTP in development (should be true in production)
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero // No tolerance for token expiration time differences
        };
    });

    // -----------------------------------------------------------------
    // 4. CROSS-ORIGIN RESOURCE SHARING (CORS)
    // Allow the Angular frontend (running on http://localhost:4200) to call this API.
    // -----------------------------------------------------------------
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularApp", policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular dev server
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Allow sending cookies / Authorization headers
        });
    });

    // -----------------------------------------------------------------
    // 5. REGISTER CUSTOM SERVICES
    // Register our business logic services (scoped per request).
    // -----------------------------------------------------------------
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IParkingService, ParkingService>();

    // -----------------------------------------------------------------
    // 6. BUILD THE APPLICATION
    // -----------------------------------------------------------------
    var app = builder.Build();

    // -----------------------------------------------------------------
    // 7. MIDDLEWARE PIPELINE
    // Configure the HTTP request pipeline (order matters!).
    // -----------------------------------------------------------------

    // Enable Swagger UI only in development
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Redirect HTTP to HTTPS (in production, you would have a valid certificate)
    app.UseHttpsRedirection();

    // Add CORS middleware (must be before Authentication/Authorization)
    app.UseCors("AllowAngularApp");

    // Add Authentication middleware (validates JWT tokens)
    app.UseAuthentication();

    // Add Authorization middleware (checks user permissions)
    app.UseAuthorization();

    // Map controller routes
    app.MapControllers();

    // -----------------------------------------------------------------
    // 8. DATABASE INITIALIZATION
    // Ensure the database is created (and migrations are applied if any).
    // Wrapped in try-catch to handle creation errors gracefully.
    // -----------------------------------------------------------------
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            dbContext.Database.EnsureCreated();
            // Optionally, you can log success here (but logger is not yet available in this scope).
            // For simple logging, we can use Console.WriteLine or ILogger after building the app.
        }
        catch (Exception ex)
        {
            // Log the error using the logger (if available) or write to console.
            // Since we are in the startup phase, we can use the built-in logger from the host.
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Failed to create or ensure the database schema.");
            throw; // Re-throw to prevent the application from starting with a broken DB
        }
    }

    // -----------------------------------------------------------------
    // 9. START THE APPLICATION
    // This call blocks until the application is shut down.
    // -----------------------------------------------------------------
    app.Run();
}
catch (Exception ex)
{
    // Global startup exception handler – catches any unhandled exception during app building/startup.
    // Log the error to the console (since we don't have ILogger yet in this scope).
    Console.WriteLine("Application startup failed: " + ex.Message);
    Console.WriteLine(ex.StackTrace);
    // Re-throw to let the process exit with an error code.
    throw;
}