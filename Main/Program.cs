using Microsoft.EntityFrameworkCore;
using DataRepositories;

var builder = WebApplication.CreateBuilder(args);

//FIXME:: use app configuration resource in Azure to reference key vault I think
var cosmosConnectionString = builder.Configuration.GetConnectionString("AzureCosmosDBConnection") ?? throw new InvalidOperationException("Connection string 'AzureCosmosDBConnection' not found.");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSingleton(typeof(IDataRepository), new CosmosDataRepository(cosmosConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
