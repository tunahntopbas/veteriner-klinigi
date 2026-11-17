using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// 1) SERVİS KAYDI — builder.Build()'dan ÖNCE olmak zorunda.
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---- Veritabanı: PostgreSQL + EF Core ----
// Bağlantı dizesi appsettings.json'da DEĞİL, "user-secrets" içinde durur
// (parola depoya girmesin diye). Ayar adı: ConnectionStrings:VarsayilanBaglanti
// ÖDEV GitHub-Gizlilik — bağlantı dizesi parolaları depoya konmamalı.
var baglantiDizesi = builder.Configuration.GetConnectionString("VarsayilanBaglanti")
    ?? throw new InvalidOperationException(
        "Bağlantı dizesi bulunamadı. README'deki 'dotnet user-secrets set' adımını yapın.");

// AddDbContext → DbContext'i SCOPED kaydeder: her HTTP isteği kendi DbContext'ini alır.
builder.Services.AddDbContext<UygulamaDbContext>(options => options.UseNpgsql(baglantiDizesi));

// ---- Kimlik (ASP.NET Core Identity) ----
// ÖDEV Beklenen-Roller — Kullanici + IdentityRole: tek kullanıcı tablosu, rollerle ayrım.
builder.Services
    .AddIdentity<Kullanici, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;               // aynı e-postayla iki hesap olmaz
        options.SignIn.RequireConfirmedAccount = false;        // e-posta onayı yok (mail sunucusu yok)
        options.Lockout.MaxFailedAccessAttempts = 5;           // 5 hatalı denemede kilit
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        // Parola kuralları Identity'nin varsayılanı: en az 6 karakter, büyük/küçük harf, rakam, sembol.
    })
    .AddEntityFrameworkStores<UygulamaDbContext>()   // kullanıcılar bizim DbContext'te (PostgreSQL)
    .AddDefaultTokenProviders();

// Giriş çerezi (cookie) ayarları.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Hesap/Giris";                 // giriş yapmamış → buraya
    options.AccessDeniedPath = "/Hesap/ErisimEngellendi"; // rolü yetmiyor → buraya
    options.ReturnUrlParameter = "donusAdresi";
    options.Cookie.HttpOnly = true;                      // JavaScript çerezi okuyamaz (XSS'e karşı)
    options.Cookie.SameSite = SameSiteMode.Lax;          // başka sitelerden gelen POST'larda çerez gitmez (CSRF'e karşı)
    options.SlidingExpiration = true;

    // DİKKAT: REST API'ye giriş yapmadan istek gelirse HTML giriş sayfasına
    // YÖNLENDİRME (302) yapma; doğru durum kodunu dön: 401 / 403.
    // ÖDEV 5.3 — doğru HTTP durum kodları.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// ---- Servis katmanı (iş kuralları) ----
// ÖDEV 6.3 — MVC controller'ları, REST API ve MCP araçları AYNI servisleri kullanır.
//
// Yaşam döngüsü (DI lifetime):
//   AddScoped    → her HTTP isteği için BİR nesne. DbContext scoped olduğu için onu
//                  kullanan servisler de scoped olmalı (Singleton bir servis scoped
//                  DbContext'i tutarsa istekler arası veri karışır → hata).
//   AddSingleton → uygulama boyunca TEK nesne. FotografServisi durum tutmaz, sadece
//                  klasör yolunu bilir → tek nesne yeterli.
builder.Services.AddSingleton<IFotografServisi, FotografServisi>();
builder.Services.AddScoped<IKlinikServisi, KlinikServisi>();
builder.Services.AddScoped<IHizmetServisi, HizmetServisi>();
builder.Services.AddScoped<IUzmanlikServisi, UzmanlikServisi>();
builder.Services.AddScoped<IVeterinerServisi, VeterinerServisi>();
builder.Services.AddScoped<IEvcilHayvanServisi, EvcilHayvanServisi>();
builder.Services.AddScoped<IAsiServisi, AsiServisi>();

var app = builder.Build();

// ---------------------------------------------------------------
// Uygulama açılırken: migration'ları uygula + rolleri, admin'i, örnek verileri ekle.
// ---------------------------------------------------------------
await VeriTohumlayici.TohumlaAsync(app.Services);

// ---------------------------------------------------------------
// 2) MIDDLEWARE HATTI — SIRA ÖNEMLİ.
// ---------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// DOĞRU SIRA: Routing → Authentication → Authorization
//   UseRouting        : isteğin hangi endpoint'e gideceğini belirler
//   UseAuthentication : çerezi (veya MCP anahtarını) okuyup "bu kim?" sorusunu cevaplar → HttpContext.User
//   UseAuthorization  : "[Authorize] bu kişiye izin veriyor mu?" kararını verir
// Authentication'ı unutursak herkes "anonim" görünür ve korumalı sayfalar hep giriş ister.
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Areas (Admin, Veteriner) için rota. Genel rotadan ÖNCE tanımlanmalı.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Panel}/{action=Index}/{id:int?}");

// {id:int?} → id varsa sayı olmak zorunda; /Randevu/Detay/abc temiz bir 404 verir.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id:int?}")
    .WithStaticAssets();

app.Run();
