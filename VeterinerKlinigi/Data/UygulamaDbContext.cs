using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Data;

/// <summary>
/// Uygulamanın veritabanıyla konuşan tek sınıf (EF Core).
///
/// IdentityDbContext&lt;Kullanici&gt;'dan türer: Identity'nin hazır tabloları
/// (AspNetUsers, AspNetRoles, AspNetUserRoles ...) otomatik gelir,
/// biz kendi tablolarımızı DbSet olarak ekleriz.
///
/// Code First: Tabloları SQL yazarak değil, bu sınıflardan "migration" üreterek oluşturuyoruz:
///   dotnet ef migrations add IlkOlusturma
///   dotnet ef database update
/// </summary>
public class UygulamaDbContext : IdentityDbContext<Kullanici>
{
    // Constructor injection: bağlantı ayarlarını (PostgreSQL, bağlantı dizesi)
    // Program.cs'teki AddDbContext verir; biz burada bilmek zorunda değiliz.
    public UygulamaDbContext(DbContextOptions<UygulamaDbContext> options) : base(options)
    {
    }

    // Her DbSet = veritabanında bir tablo.
    public DbSet<Klinik> Klinikler => Set<Klinik>();
    public DbSet<CalismaSaati> CalismaSaatleri => Set<CalismaSaati>();
    public DbSet<Hizmet> Hizmetler => Set<Hizmet>();
    public DbSet<Uzmanlik> Uzmanliklar => Set<Uzmanlik>();
    public DbSet<Veteriner> Veterinerler => Set<Veteriner>();
    public DbSet<VeterinerMusaitlik> VeterinerMusaitlikleri => Set<VeterinerMusaitlik>();
    public DbSet<EvcilHayvan> EvcilHayvanlar => Set<EvcilHayvan>();
    public DbSet<AsiKaydi> AsiKayitlari => Set<AsiKaydi>();
    public DbSet<Randevu> Randevular => Set<Randevu>();
    public DbSet<MuayeneNotu> MuayeneNotlari => Set<MuayeneNotu>();
    public DbSet<McpErisimAnahtari> McpErisimAnahtarlari => Set<McpErisimAnahtari>();

    /// <summary>
    /// Tüm DateTime alanları için ortak kural.
    /// Npgsql varsayılan olarak DateTime'ı "timestamp with time zone" yapar ve UTC ister.
    /// Randevu saatleri kliniğin yerel saatidir ("14:30'da gel"), saat dilimi dönüşümü
    /// istemiyoruz → hepsi "timestamp without time zone".
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");
    }

    /// <summary>
    /// İlişkiler, benzersizlik kuralları ve silme davranışları (Fluent API).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // DİKKAT: Identity tablolarının kurulumu için base çağrısı ŞART.
        base.OnModelCreating(builder);

        // ---- Veteriner ----
        builder.Entity<Veteriner>(e =>
        {
            // Bir kullanıcı hesabı en fazla bir hekime bağlanır (1 ↔ 1).
            e.HasIndex(v => v.KullaniciId).IsUnique();
            e.HasOne(v => v.Kullanici).WithMany().HasForeignKey(v => v.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            // ÖDEV 2.2 — Çok ↔ çok ilişkiler. Ara tablo adlarını biz veriyoruz.
            e.HasMany(v => v.Uzmanliklar).WithMany(u => u.Veterinerler)
                .UsingEntity(j => j.ToTable("VeterinerUzmanliklari"));
            e.HasMany(v => v.Hizmetler).WithMany(h => h.Veterinerler)
                .UsingEntity(j => j.ToTable("VeterinerHizmetleri"));
        });

        // ---- Klinik çalışma saati: her gün için tek satır ----
        builder.Entity<CalismaSaati>()
            .HasIndex(c => new { c.KlinikId, c.Gun }).IsUnique();

        // ---- Uzmanlık adı tekrar etmesin ----
        builder.Entity<Uzmanlik>().HasIndex(u => u.Ad).IsUnique();

        // ---- Evcil hayvan → sahip ----
        builder.Entity<EvcilHayvan>()
            .HasOne(h => h.Sahip).WithMany(k => k.EvcilHayvanlar)
            .HasForeignKey(h => h.SahipId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- Randevu ----
        builder.Entity<Randevu>(e =>
        {
            // Hizmet veya hekim silinmek istenirse, randevusu varsa İZİN VERME (Restrict).
            // Geçmiş randevular ve ciro raporu kaybolmamalı.
            e.HasOne(r => r.Hizmet).WithMany().HasForeignKey(r => r.HizmetId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Veteriner).WithMany(v => v.Randevular).HasForeignKey(r => r.VeterinerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.EvcilHayvan).WithMany(h => h.Randevular).HasForeignKey(r => r.EvcilHayvanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Çakışma sorgusu hekim + zaman aralığına bakar → bu sütunlara indeks.
            e.HasIndex(r => new { r.VeterinerId, r.BaslangicZamani });
        });

        // ---- Muayene notu: randevu başına en fazla bir tane ----
        builder.Entity<MuayeneNotu>()
            .HasOne(m => m.Randevu).WithOne(r => r.MuayeneNotu)
            .HasForeignKey<MuayeneNotu>(m => m.RandevuId)
            .OnDelete(DeleteBehavior.Cascade);

        // ---- MCP anahtarı: hash ile hızlı ve benzersiz arama ----
        builder.Entity<McpErisimAnahtari>(e =>
        {
            e.HasIndex(a => a.AnahtarHash).IsUnique();
            e.HasOne(a => a.Kullanici).WithMany().HasForeignKey(a => a.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
