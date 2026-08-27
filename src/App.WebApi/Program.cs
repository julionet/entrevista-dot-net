using Microsoft.EntityFrameworkCore;
using App.Application.Ports.Input;
using App.Application.UseCases;
using App.Infrastructure;
using App.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Wiring hexagonal: liga as portas (interfaces) da Application aos adaptadores concretos.
// Trocar de banco de dados (ver App.Infrastructure.DependencyInjection.AddPersistence) exigiria mudar
// apenas a configuração/adaptador de persistência — nenhuma camada de Domain/Application/WebApi seria afetada.
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
