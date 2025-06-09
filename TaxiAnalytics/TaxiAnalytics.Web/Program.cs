using TaxiAnalytics.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register taxi data services
builder.Services.AddScoped<ITaxiDataService>(provider =>
{
    var httpContext = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var database = httpContext?.Request.Query["db"].ToString() ?? "clickhouse";
    
    if (database.ToLower() == "postgresql")
    {
        return new PostgreSqlTaxiDataService(provider.GetRequiredService<IConfiguration>());
    }
    return new ClickHouseTaxiDataService(provider.GetRequiredService<IConfiguration>());
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
