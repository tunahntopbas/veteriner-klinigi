using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Data;

/// <summary>
/// Uygulama ilk açıldığında veritabanını hazırlar (seed / tohumlama):
///   1) Bekleyen migration'ları uygular (tablolar yoksa oluşturur)
///   2) Üç rolü oluşturur (Admin, Veteriner, Uye)
///   3) Admin hesabını oluşturur
///   4) Örnek klinik, hizmet, uzmanlık, hekim, üye, hayvan, aşı ve randevu ekler
///
/// Her adım "zaten var mı?" diye bakar → uygulama her açıldığında güvenle çalışır,
/// kayıtları ikinci kez eklemez.
///
/// Program.cs'te app.Run()'dan önce çağrılır.
/// </summary>
public static class VeriTohumlayici
{
    // ÖDEV Beklenen-Roller — Admin girişi ödevde verildiği gibi: ogrencinumarasi@sakarya.edu.tr / sau
    public const string AdminEposta = "g221210024@sakarya.edu.tr";
    private const string AdminSifre = "sau";

    // Örnek (deneme) hesapların ortak parolaları. README'de "Deneme hesapları" bölümünde yazılı.
    private const string VeterinerSifre = "Veteriner1!";
    private const string UyeSifre = "Uye12345!";

    public static async Task TohumlaAsync(IServiceProvider servisler)
    {
        // Scoped servisleri (DbContext, UserManager) uygulama başlarken kullanabilmek için
        // geçici bir "scope" açıyoruz. Normalde her HTTP isteği kendi scope'unu açar.
        using var scope = servisler.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UygulamaDbContext>();
        var kullaniciYoneticisi = scope.ServiceProvider.GetRequiredService<UserManager<Kullanici>>();
        var rolYoneticisi = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // 1) dotnet ef database update komutunun kod içindeki karşılığı.
        await db.Database.MigrateAsync();

        // 2) Roller
        foreach (var rol in new[] { Roller.Admin, Roller.Veteriner, Roller.Uye })
        {
            if (!await rolYoneticisi.RoleExistsAsync(rol))
                await rolYoneticisi.CreateAsync(new IdentityRole(rol));
        }

        // 3) Admin
        await AdminOlusturAsync(kullaniciYoneticisi);

        // 4) Örnek veriler — klinik zaten varsa hiçbirini tekrar ekleme.
        if (await db.Klinikler.AnyAsync())
            return;

        await OrnekVerileriEkleAsync(db, kullaniciYoneticisi);
    }

    /// <summary>
    /// DİKKAT — "sau" parolası Identity'nin parola kuralına (en az 6 karakter, büyük harf,
    /// rakam, sembol) TAKILIR. Kuralı herkes için gevşetmek yerine, YALNIZCA bu hesabın
    /// parolasını PasswordHasher ile hash'leyip doğrudan yazıyoruz. Kayıt sayfasından gelen
    /// üyeler yine güçlü parola kuralına tabi.
    /// </summary>
    private static async Task AdminOlusturAsync(UserManager<Kullanici> kullaniciYoneticisi)
    {
        if (await kullaniciYoneticisi.FindByEmailAsync(AdminEposta) is not null)
            return;

        var admin = new Kullanici
        {
            UserName = AdminEposta,
            Email = AdminEposta,
            EmailConfirmed = true,
            AdSoyad = "Klinik Yöneticisi"
        };

        // Parolanın kendisi değil, hash'i saklanır (geri çevrilemez).
        admin.PasswordHash = new PasswordHasher<Kullanici>().HashPassword(admin, AdminSifre);

        // Parola vermeden CreateAsync → parola kuralı çalışmaz, hash olduğu gibi kaydedilir.
        await kullaniciYoneticisi.CreateAsync(admin);
        await kullaniciYoneticisi.AddToRoleAsync(admin, Roller.Admin);
    }

    private static async Task<Kullanici> KullaniciOlusturAsync(
        UserManager<Kullanici> kullaniciYoneticisi, string eposta, string adSoyad, string sifre, string rol)
    {
        var kullanici = new Kullanici { UserName = eposta, Email = eposta, EmailConfirmed = true, AdSoyad = adSoyad };
        var sonuc = await kullaniciYoneticisi.CreateAsync(kullanici, sifre);
        if (!sonuc.Succeeded)
            throw new InvalidOperationException($"{eposta} oluşturulamadı: " +
                string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        await kullaniciYoneticisi.AddToRoleAsync(kullanici, rol);
        return kullanici;
    }

    private static async Task OrnekVerileriEkleAsync(UygulamaDbContext db, UserManager<Kullanici> kullaniciYoneticisi)
    {
        // ---------------- ÖDEV 1.1 / 1.2 — Klinik ve çalışma saatleri ----------------
        var klinik = new Klinik
        {
            Ad = "Pati Veteriner Kliniği",
            Adres = "Serdivan, Sakarya",
            Telefon = "0264 000 00 00"
        };
        for (var gun = DayOfWeek.Monday; gun <= DayOfWeek.Friday; gun++)
            klinik.CalismaSaatleri.Add(new CalismaSaati { Gun = gun, AcilisSaati = new(9, 0), KapanisSaati = new(18, 0) });
        klinik.CalismaSaatleri.Add(new CalismaSaati { Gun = DayOfWeek.Saturday, AcilisSaati = new(10, 0), KapanisSaati = new(16, 0) });
        klinik.CalismaSaatleri.Add(new CalismaSaati { Gun = DayOfWeek.Sunday, AcilisSaati = new(0, 0), KapanisSaati = new(0, 0), Kapali = true });

        // ---------------- ÖDEV 1.2 — Hizmetler (süre + ücret) ----------------
        Hizmet H(string ad, int sure, decimal ucret, string aciklama) =>
            new() { Ad = ad, SureDakika = sure, Ucret = ucret, Aciklama = aciklama, Klinik = klinik };

        var muayene = H("Genel Muayene", 30, 600, "Genel sağlık kontrolü ve fizik muayene");
        var karmaAsi = H("Karma Aşı", 20, 450, "Kedi/köpek karma aşı uygulaması");
        var kuduzAsi = H("Kuduz Aşısı", 15, 350, "Yıllık kuduz aşısı");
        var kisirDisi = H("Kısırlaştırma (Dişi)", 120, 4500, "Ovariohisterektomi operasyonu");
        var kisirErkek = H("Kısırlaştırma (Erkek)", 60, 3000, "Kastrasyon operasyonu");
        var dis = H("Diş Temizliği", 60, 2000, "Ultrasonik diş taşı temizliği");
        var tiras = H("Tıraş ve Bakım", 90, 1200, "Yıkama, tıraş, tırnak kesimi");
        var derma = H("Dermatoloji Muayenesi", 40, 900, "Deri ve tüy problemleri");
        var egzotik = H("Egzotik Hayvan Muayenesi", 40, 800, "Kuş, sürüngen, kemirgen muayenesi");
        db.Hizmetler.AddRange(muayene, karmaAsi, kuduzAsi, kisirDisi, kisirErkek, dis, tiras, derma, egzotik);

        // ---------------- ÖDEV 2.2 — Uzmanlık alanları ----------------
        var kucukHayvan = new Uzmanlik { Ad = "Küçük Hayvan" };
        var egzotikU = new Uzmanlik { Ad = "Egzotik Hayvan" };
        var cerrahi = new Uzmanlik { Ad = "Cerrahi" };
        var dermatoloji = new Uzmanlik { Ad = "Dermatoloji" };
        var disHekimligi = new Uzmanlik { Ad = "Diş Hekimliği" };
        db.Uzmanliklar.AddRange(kucukHayvan, egzotikU, cerrahi, dermatoloji, disHekimligi);

        // ---------------- ÖDEV 2.1 / 2.2 / 2.3 — Hekimler ----------------
        var elifK = await KullaniciOlusturAsync(kullaniciYoneticisi, "elif.kaya@patiklinik.com", "Elif Kaya", VeterinerSifre, Roller.Veteriner);
        var canK = await KullaniciOlusturAsync(kullaniciYoneticisi, "can.demir@patiklinik.com", "Can Demir", VeterinerSifre, Roller.Veteriner);
        var zeynepK = await KullaniciOlusturAsync(kullaniciYoneticisi, "zeynep.arslan@patiklinik.com", "Zeynep Arslan", VeterinerSifre, Roller.Veteriner);

        var elif = new Veteriner
        {
            Klinik = klinik, KullaniciId = elifK.Id, AdSoyad = "Elif Kaya", Unvan = "Uzm. Vet. Hek.",
            Uzmanliklar = [kucukHayvan, cerrahi],
            Hizmetler = [muayene, karmaAsi, kuduzAsi, kisirDisi, kisirErkek]
        };
        // Öğle arası olsun diye aynı gün İKİ aralık: 09-12 ve 13-17
        for (var gun = DayOfWeek.Monday; gun <= DayOfWeek.Friday; gun++)
        {
            elif.Musaitlikler.Add(new VeterinerMusaitlik { Gun = gun, BaslangicSaati = new(9, 0), BitisSaati = new(12, 0) });
            elif.Musaitlikler.Add(new VeterinerMusaitlik { Gun = gun, BaslangicSaati = new(13, 0), BitisSaati = new(17, 0) });
        }

        var can = new Veteriner
        {
            Klinik = klinik, KullaniciId = canK.Id, AdSoyad = "Can Demir", Unvan = "Vet. Hek.",
            Uzmanliklar = [egzotikU, dermatoloji],
            Hizmetler = [muayene, karmaAsi, derma, egzotik]
        };
        foreach (var gun in new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday })
            can.Musaitlikler.Add(new VeterinerMusaitlik { Gun = gun, BaslangicSaati = new(10, 0), BitisSaati = new(16, 0) });

        var zeynep = new Veteriner
        {
            Klinik = klinik, KullaniciId = zeynepK.Id, AdSoyad = "Zeynep Arslan", Unvan = "Vet. Hek.",
            Uzmanliklar = [kucukHayvan, disHekimligi],
            Hizmetler = [muayene, dis, tiras, kuduzAsi]
        };
        foreach (var gun in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
            zeynep.Musaitlikler.Add(new VeterinerMusaitlik { Gun = gun, BaslangicSaati = new(9, 0), BitisSaati = new(18, 0) });
        zeynep.Musaitlikler.Add(new VeterinerMusaitlik { Gun = DayOfWeek.Saturday, BaslangicSaati = new(10, 0), BitisSaati = new(14, 0) });

        db.Veterinerler.AddRange(elif, can, zeynep);

        // ---------------- ÖDEV 3.1 / 3.2 / 3.3 — Üyeler, hayvanlar, aşılar ----------------
        var ayse = await KullaniciOlusturAsync(kullaniciYoneticisi, "ayse@ornek.com", "Ayşe Yılmaz", UyeSifre, Roller.Uye);
        var mehmet = await KullaniciOlusturAsync(kullaniciYoneticisi, "mehmet@ornek.com", "Mehmet Öz", UyeSifre, Roller.Uye);

        var bugun = DateOnly.FromDateTime(DateTime.Today);
        var simdi = DateTime.Now;

        var pamuk = new EvcilHayvan
        {
            SahipId = ayse.Id, Ad = "Pamuk", Tur = "Kedi", Irk = "Tekir",
            DogumTarihi = new DateOnly(2022, 4, 10), Kilo = 4.2m, KayitZamani = simdi,
            AsiKayitlari =
            [
                new AsiKaydi { AsiAdi = "Karma Aşı", YapildigiTarih = bugun.AddDays(-60), SonrakiDozTarihi = bugun.AddDays(305) },
                new AsiKaydi { AsiAdi = "Kuduz Aşısı", YapildigiTarih = bugun.AddDays(-20), SonrakiDozTarihi = bugun.AddDays(345) },
                new AsiKaydi { AsiAdi = "İç Parazit", YapildigiTarih = bugun.AddDays(-70), SonrakiDozTarihi = bugun.AddDays(20), Notlar = "3 ayda bir tekrar" }
            ]
        };
        var boncuk = new EvcilHayvan
        {
            SahipId = ayse.Id, Ad = "Boncuk", Tur = "Köpek", Irk = "Golden Retriever",
            DogumTarihi = new DateOnly(2020, 9, 1), Kilo = 28.5m, KayitZamani = simdi,
            AsiKayitlari = [new AsiKaydi { AsiAdi = "Karma Aşı", YapildigiTarih = bugun.AddDays(-340), SonrakiDozTarihi = bugun.AddDays(25) }]
        };
        var karabas = new EvcilHayvan
        {
            SahipId = mehmet.Id, Ad = "Karabaş", Tur = "Köpek", Irk = "Kangal",
            DogumTarihi = new DateOnly(2019, 1, 15), Kilo = 45m, KayitZamani = simdi,
            AsiKayitlari = [new AsiKaydi { AsiAdi = "Kuduz Aşısı", YapildigiTarih = bugun.AddDays(-100), SonrakiDozTarihi = bugun.AddDays(265) }]
        };
        db.EvcilHayvanlar.AddRange(pamuk, boncuk, karabas);

        // ---------------- ÖDEV 4.x / 3.4 — Örnek randevular ----------------
        // Geçmişte tamamlanmış bir randevu + muayene notu
        var gecmisGun = GunBul(DateTime.Today.AddDays(-7), DayOfWeek.Monday, geriye: true);
        var gecmis = RandevuYap(pamuk, muayene, elif, gecmisGun.AddHours(10), RandevuDurumu.Tamamlandi, simdi);
        gecmis.MuayeneNotu = new MuayeneNotu
        {
            Teshis = "Genel durumu iyi",
            Tedavi = "Diyet mamasına geçiş önerildi",
            Notlar = "Kilo takibi yapılmalı. 6 ay sonra kontrol.",
            YazilmaZamani = gecmisGun.AddHours(11)
        };

        // Gelecekte onay bekleyen ve onaylanmış randevular
        var ileriCarsamba = GunBul(DateTime.Today.AddDays(1), DayOfWeek.Wednesday, geriye: false);
        var ileriSali = GunBul(DateTime.Today.AddDays(1), DayOfWeek.Tuesday, geriye: false);
        var bekleyen = RandevuYap(boncuk, dis, zeynep, ileriCarsamba.AddHours(11), RandevuDurumu.Bekliyor, simdi);
        bekleyen.SahipNotu = "Ağız kokusu var.";
        var onayli = RandevuYap(karabas, muayene, elif, ileriSali.AddHours(14), RandevuDurumu.Onaylandi, simdi);

        db.Randevular.AddRange(gecmis, bekleyen, onayli);

        await db.SaveChangesAsync();
    }

    // Randevu oluştururken süre ve ücret hizmetten KOPYALANIR (ÖDEV 4.3).
    private static Randevu RandevuYap(EvcilHayvan hayvan, Hizmet hizmet, Veteriner hekim,
        DateTime baslangic, RandevuDurumu durum, DateTime simdi) => new()
    {
        EvcilHayvan = hayvan, Hizmet = hizmet, Veteriner = hekim,
        BaslangicZamani = baslangic,
        BitisZamani = baslangic.AddMinutes(hizmet.SureDakika),
        SureDakika = hizmet.SureDakika,
        Ucret = hizmet.Ucret,
        Durum = durum,
        OlusturmaZamani = simdi
    };

    // Verilen tarihten başlayıp istenen haftanın gününe denk gelen ilk tarihi bulur.
    private static DateTime GunBul(DateTime baslangic, DayOfWeek gun, bool geriye)
    {
        var tarih = baslangic.Date;
        while (tarih.DayOfWeek != gun)
            tarih = tarih.AddDays(geriye ? -1 : 1);
        return tarih;
    }
}
