var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient("SatApi", cliente =>
{
    string baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5175/";
    cliente.BaseAddress = new Uri(baseUrl);
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run("http://localhost:5176");
