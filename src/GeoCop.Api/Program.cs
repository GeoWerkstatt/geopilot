﻿using Asp.Versioning;
using GeoCop.Api;
using GeoCop.Api.Validation;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddApiVersioning(config =>
    {
        config.AssumeDefaultVersionWhenUnspecified = false;
        config.ReportApiVersions = true;
        config.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "1.0",
        Title = $"GeoCop API Documentation",
    });

    // Include existing documentation in Swagger UI.
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

    options.EnableAnnotations();
    options.SupportNonNullableReferenceTypes();
});

builder.Services.AddSingleton<IValidatorService, ValidatorService>();
builder.Services.AddHostedService(services => (ValidatorService)services.GetRequiredService<IValidatorService>());
builder.Services.AddTransient<IValidator, InterlisValidator>();
builder.Services.AddTransient<IFileProvider, PhysicalFileProvider>(x => new PhysicalFileProvider(x.GetRequiredService<IConfiguration>(), "GEOCOP_UPLOADS_DIR"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "GeoCop API v1.0");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
