using FileHostingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Services and controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<FileMetadataService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Disabled HTTPS redirection for test host stability
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

namespace FileHostingApi { public partial class Program {} }
