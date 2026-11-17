namespace VeterinerKlinigi.Models;

/// <summary>
/// Randevunun hangi aşamada olduğu.
///
/// ÖDEV 4.4 — Randevu "Bekliyor" durumunda oluşur; Admin veya ilgili veteriner
/// onaylar ya da reddeder.
///
/// İzin verilen geçişler (durum makinesi, Services/RandevuServisi.cs):
///   Bekliyor   → Onaylandi | Reddedildi | IptalEdildi (sahip iptal edebilir)
///   Onaylandi  → Tamamlandi | IptalEdildi
///   Reddedildi, Tamamlandi, IptalEdildi → son durum, değişmez
///
/// Veritabanında sayı olarak saklanır (Bekliyor = 0 ...). Sıra değiştirilmemeli.
/// </summary>
public enum RandevuDurumu
{
    Bekliyor = 0,
    Onaylandi = 1,
    Reddedildi = 2,
    Tamamlandi = 3,
    IptalEdildi = 4
}
