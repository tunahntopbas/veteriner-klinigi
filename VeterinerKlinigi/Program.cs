using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// 1) SERVİS KAYDI — builder.Build()'dan ÖNCE olmak zorunda.
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

// Veritabanı: PostgreSQL + EF Core.
// Bağlantı dizesi appsettings.json'da DEĞİL, "user-secrets" içinde durur
// (parola depoya girmesin diye). Ayar adı: ConnectionStrings:VarsayilanBaglanti
// ÖDEV GitHub-Gizlilik — bağlantı dizesi parolaları depoya konmamalı.
var baglantiDizesi = builder.Configuration.GetConnectionString("VarsayilanBaglanti")
    ?? throw new InvalidOperationException(
        "Bağlantı dizesi bulunamadı. README'deki 'dotnet user-secrets set' adımını yapın.");

builder.Services.AddDbContext<UygulamaDbContext>(options => options.UseNpgsql(baglantiDizesi));

var app = builder.Build();

// ---------------------------------------------------------------
// 2) MIDDLEWARE HATTI — SIRA ÖNEMLİ.
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id:int?}")
    .WithStaticAssets();

app.Run();
