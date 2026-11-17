namespace VeterinerKlinigi.Data;

/// <summary>
/// Sistemdeki üç rolün adı. Tek bir yerde sabit (const) olarak tutuluyor ki
/// [Authorize(Roles = Roller.Admin)] gibi kullanımlarda yazım hatası yapılmasın.
///
/// ÖDEV Beklenen-Roller — En az 3 rol:
///   Admin     → her şeyi yönetir (klinik, hizmet, hekim, tüm randevular, rapor)
///   Veteriner → kendi randevularını onaylar/reddeder, muayene notu girer
///   Uye       → hayvan sahibi; kendi hayvanları, aşıları ve randevuları
/// </summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Veteriner = "Veteriner";
    public const string Uye = "Uye";

    // Admin + Veteriner birlikte yetkili olduğunda: [Authorize(Roles = Roller.AdminVeyaVeteriner)]
    public const string AdminVeyaVeteriner = Admin + "," + Veteriner;
}
