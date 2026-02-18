//gerekli kütüphaneleri import ediyoruz
using Microsoft.EntityFrameworkCore; //veritabanı işlemleri
using DecisionTree.Api.Data;// veritabanı bağlantısı
using DecisionTree.Api.Services;//iş mantığı 

var builder = WebApplication.CreateBuilder(args); //yeni bir ASP.NET Core uygulaması hazırlıyoruz


builder.Services.AddControllers() // API kontrolleri ekliyoruz(bunlar HTTP isteklerini işler)
    .AddJsonOptions(options =>
    {
        // Frontend'den gelen camelCase property'leri C# PascalCase'e map et
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Register services, //
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<JsonBuilderService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<JobApplicationSeedService>();
builder.Services.AddScoped<ConversionService>();

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


