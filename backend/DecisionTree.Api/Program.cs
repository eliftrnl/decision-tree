using Microsoft.EntityFrameworkCore;
using DecisionTree.Api.Data;
using DecisionTree.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register services
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<JsonBuilderService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<JobApplicationSeedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (Angular için)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", p =>
        p.WithOrigins(
             // Angular (4200)
             "http://localhost:4200",
             "https://localhost:4200",
             "http://127.0.0.1:4200",
             "https://127.0.0.1:4200",

             // (Varsa) Angular / başka frontend portu
             "http://localhost:59443",
             "https://localhost:59443",
             "http://127.0.0.1:59443",
             "https://127.0.0.1:59443"
         )
         .AllowAnyHeader()
         .AllowAnyMethod()
    );
});

var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs)));

var app = builder.Build();

// Seed Job Application Data
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var seedService = scope.ServiceProvider.GetRequiredService<JobApplicationSeedService>();
        await seedService.SeedDataAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS middleware: Authorization ve MapControllers'dan önce olmalı
app.UseCors("dev");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();


