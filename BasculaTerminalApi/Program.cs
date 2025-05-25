using BasculaTerminalApi.Config;
using BasculaTerminalApi.Controllers;
using BasculaTerminalApi.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<BasculaService>();
builder.Services.AddSingleton<PrintSettings>(sp =>
{
    var printService = new PrintSettings();
    printService.ReadSettings();
    return printService;
});

//cors allow all
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});



builder.Services.AddScoped<PrintService>();

var app = builder.Build();

//cors
app.UseCors("AllowAll");

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
