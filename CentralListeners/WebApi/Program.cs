using WebApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.
    AddJsonFile("appsettings.json", optional: false).
    AddEnvironmentVariables();

builder.Services.Configure<MarqueeText>(builder.Configuration.GetSection("MarqueeText"));

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
