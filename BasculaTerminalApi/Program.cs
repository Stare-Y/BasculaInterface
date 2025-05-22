using BasculaTerminalApi;
using BasculaTerminalApi.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<BasculaService>();

var app = builder.Build();

app.MapHub<SerialPortHub>("/basculaSocket");

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2)});
    
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
