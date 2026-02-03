using Microsoft.EntityFrameworkCore;
using GersoPersonalizados.Api.Data.Models;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", p =>
        p.AllowAnyHeader()
         .AllowAnyMethod()
         .AllowAnyOrigin());
});

builder.Services.AddDbContext<GersoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GersoDb")));

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("dev");

app.UseAuthorization();

app.MapControllers();


app.Run();
