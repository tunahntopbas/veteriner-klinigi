using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Sisteme giriş yapan HER kişi: Admin, Veteriner ve Üye (hayvan sahibi).
///
/// IdentityUser'dan türer → Id, Email, UserName, PasswordHash gibi alanlar hazır gelir,
/// biz sadece AdSoyad ekledik. Tablo adı: AspNetUsers.
///
/// ÖDEV Beklenen-Roller — Admin ve üyeler için AYRI TABLO YOK.
///   Kim olduğu, AspNetRoles (rol listesi) ve AspNetUserRoles (kullanıcı ↔ rol)
///   tablolarıyla belirlenir. Böylece giriş, parola, kilitleme gibi işlemler
///   tek bir yerde ve herkes için aynı şekilde çalışır.
/// </summary>
public class Kullanici : IdentityUser
{
    [Required, StringLength(100)]
    public string AdSoyad { get; set; } = "";

    // ÖDEV 3.1 — Bir sahip birden fazla hayvan kaydedebilir (1 → çok).
    public List<EvcilHayvan> EvcilHayvanlar { get; set; } = [];
}
