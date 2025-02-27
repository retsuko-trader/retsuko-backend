using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMvc()
  .AddJsonOptions(options => {
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });

var app = builder.Build();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
