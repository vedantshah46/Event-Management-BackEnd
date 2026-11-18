using EventProject.Modules.Events;
using EventProject.Modules.Events.Infrastructure;
using System.Text.Json.Serialization;
using EventProject.Modules.Organizations;
using EventProject.Modules.Organizations.Infrastructure;
using Microsoft.OpenApi;
using EventProject.Configuration;
using System.Threading.RateLimiting;
using EventProject.Modules.Identity;
using EventProject.Modules.Identity.Infrastructure;
using EventProject.Modules.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;

var provisionAdmin = args.Contains("--provision-admin", StringComparer.Ordinal);
var hostArgs = args.Where(x => x != "--provision-admin").ToArray();
var builder = WebApplication.CreateBuilder(hostArgs);

builder.Services.AddControllers()
    .AddApplicationPart(IdentityModule.Assembly)
    .AddApplicationPart(OrganizationsModule.Assembly)
    .AddApplicationPart(EventsModule.Assembly)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EventProject API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the accessToken returned by Admin or Organizer login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.OperationFilter<AnonymousOperationFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddOrganizationsModule();
builder.Services.AddEventsModule();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await Results.Problem(statusCode: 429, title: "Too many login attempts. Try again later.")
            .ExecuteAsync(context.HttpContext);
    };
    options.AddFixedWindowLimiter("organization-registration", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("organizer-login", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("admin-login", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

var app = builder.Build();

// Explicit operator command only. Never creates or migrates the database.
if (provisionAdmin)
{
    await using var scope = app.Services.CreateAsyncScope();
    var configuration = app.Configuration.GetRequiredSection("AdminProvisioning");
    await scope.ServiceProvider.GetRequiredService<AdminProvisioner>().CreateAsync(
        configuration["Email"] ?? string.Empty,
        configuration["FullName"] ?? string.Empty,
        configuration["Password"] ?? string.Empty);
    Console.WriteLine("Admin account created. Remove AdminProvisioning secrets now.");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "EventProject API v1"));
}
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
