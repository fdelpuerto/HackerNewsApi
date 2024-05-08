using HackerNewsAPI.Common;
using HackerNewsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS
const string AllowAllPolicy = "AllowAll";

builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowAllPolicy,
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IHackerNewsService, HackerNewsService>();
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
builder.Services.Configure<HackerNewsApiSettings>(builder.Configuration.GetSection("HackerNewsApi"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(AllowAllPolicy);
app.UseAuthorization();

app.MapControllers();

app.Run();

