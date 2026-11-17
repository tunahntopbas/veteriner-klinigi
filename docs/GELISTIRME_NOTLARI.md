# Geliştirme Notları

Bu dosya projenin "defteri"dir: ödevin her maddesinin kodda **nerede** karşılandığı, mimarinin özeti, adım adım ne yapıldığı
ve sunumda gelebilecek soruların cevapları burada. Kod içinde de her madde `// ÖDEV 4.2 — ...` biçiminde yorumla işaretlidir;
VS Code'da `Ctrl+Shift+F` → `ÖDEV 4.2` araması ilgili tüm yerleri bulur.

> Dosya yolları `VeterinerKlinigi/` proje klasörüne göredir. Satır numaraları yazıldığı andaki hâlidir; kod değişirse kayabilir,
> o durumda `ÖDEV x.y` araması yine doğru yeri bulur.

---

## 1. Ödev İsterleri Kontrol Listesi

Durum: ✅ tamam · 🔁 kullanıcının süreç içinde yapacağı iş

### 1. Klinik Tanımlamaları

| Madde | İster | Durum | Nerede |
|---|---|---|---|
| 1.1 | Klinikler yetki çerçevesinde tanımlanabilir (tek klinik kabulü) | ✅ | `Models/Klinik.cs` · `Services/KlinikServisi.cs` · `Areas/Admin/Controllers/KlinikController.cs` (yalnız Admin) · `Areas/Admin/Views/Klinik/Index.cshtml` |
| 1.2 | Çalışma saatleri | ✅ | `Models/CalismaSaati.cs` (gün başına bir satır, `Kapali`) · `KlinikController` · seed: `Data/VeriTohumlayici.cs:99` · ana sayfada gösterim `Views/Home/Index.cshtml` |
| 1.2 | Hizmetler, süre ve ücretleri | ✅ | `Models/Hizmet.cs` (`SureDakika`, `Ucret numeric(10,2)`) · `Services/HizmetServisi.cs` · `Areas/Admin/Controllers/HizmetController.cs` · herkese açık liste `HomeController.Hizmetler` |

### 2. Veteriner Hekim Yönetimi

| Madde | İster | Durum | Nerede |
|---|---|---|---|
| 2.1 | Hekimler tanımlanabilir | ✅ | `Models/Veteriner.cs` · `Services/VeterinerServisi.cs` (hekim + giriş hesabı + "Veteriner" rolü tek transaction) · `Areas/Admin/Controllers/VeterinerController.cs` |
| 2.2 | Uzmanlık alanları ve verebildiği hizmetler | ✅ | `Models/Uzmanlik.cs` · çoka çok: `Data/UygulamaDbContext.cs:67` (`VeterinerUzmanliklari`, `VeterinerHizmetleri`) · formda çoklu seçim `Areas/Admin/Views/Veteriner/Form.cshtml` · `UzmanlikController` |
| 2.3 | Müsaitlik saatleri; sahipler yalnız bu aralıklara randevu alır | ✅ | `Models/VeterinerMusaitlik.cs` · ekle/sil `VeterinerController.cs:165` · kontrol `Services/MusaitlikServisi.cs:94` (başlangıç VE bitiş aynı aralığın içinde olmalı) |

### 3. Hayvan Sahibi ve Evcil Hayvan

| Madde | İster | Durum | Nerede |
|---|---|---|---|
| 3.1 | Sahipler üye olur, birden fazla hayvan kaydeder | ✅ | Kayıt `Controllers/HesapController.cs` · `Models/Kullanici.cs:22` (1 → çok) · `Models/EvcilHayvan.cs` (`SahipId`) |
| 3.2 | Tür, ırk, doğum tarihi, kilo, isteğe bağlı fotoğraf | ✅ | `Models/EvcilHayvan.cs` · `ViewModels/UyeVM.cs` (`EvcilHayvanVM.Fotograf : IFormFile`) · `Services/FotografServisi.cs` · `Controllers/EvcilHayvanController.cs` |
| 3.3 | Aşı kayıtları (ad, yapıldığı tarih, sonraki doz) | ✅ | `Models/AsiKaydi.cs` · `Services/AsiServisi.cs` · `Controllers/AsiKaydiController.cs` · `Views/AsiKaydi/Form.cshtml` |
| 3.4 | Tamamlanan randevuya hekim muayene notu | ✅ | `Models/MuayeneNotu.cs` (RandevuId unique → 1:0..1) · `Services/RandevuServisi.cs:233` (yalnız kendi hekimi, yalnız Tamamlandi) · `Areas/Hekim/Controllers/RandevuController.cs` · `Areas/Hekim/Views/Randevu/MuayeneNotu.cshtml` |

### 4. Randevu Sistemi

| Madde | İster | Durum | Nerede |
|---|---|---|---|
| 4.1 | Hayvan, hizmet ve uygun hekim seçerek randevu | ✅ | `Controllers/RandevuController.cs` (Olustur) · `Views/Randevu/Form.cshtml` (tarih+hizmet seçilince API'den boş saat düğmeleri) · `Services/RandevuServisi.cs:69` `OlusturAsync` |
| 4.2 | Müsaitlik + önceki randevular; uygun değilse uyar; hizmet süresi dahil | ✅ | `Services/MusaitlikServisi.cs` `RandevuUygunMuAsync`: bitiş = başlangıç + süre (`:76`), klinik saati, müsaitlik (`:94`), çakışma (`:106`, formül `:117`). Uyarı formda: `RandevuController.cs:85` (`ModelState.AddModelError`) |
| 4.3 | Detaylar saklanır (hizmet, süre, ücret, hekim, hayvan, tarih/saat) | ✅ | `Models/Randevu.cs:36` (süre/ücret kopyası) · `RandevuServisi.cs:104-105` · `Views/Shared/_RandevuBilgileri.cshtml` |
| 4.4 | "Bekliyor" ile oluşur; Admin veya ilgili veteriner onaylar/reddeder | ✅ | `Models/RandevuDurumu.cs` · `RandevuServisi.cs:106` (Bekliyor), `:161` durum makinesi, `:264` `GecisGecerliMi` · Admin: `Areas/Admin/Controllers/RandevuController.cs` · Hekim: `Areas/Hekim/Controllers/RandevuController.cs` |

### 5. Raporlama — REST API

| Madde | İster | Durum | Nerede |
|---|---|---|---|
| 5.1 | REST API ile veritabanı iletişimi | ✅ | `Controllers/Api/*.cs` (5 controller). Okuma + yazma (`POST /api/randevular`). Site içi tüketim: `Views/Home/Hekimler.cshtml:22`, `Views/Randevu/Form.cshtml:80`, `Views/EvcilHayvan/Detay.cshtml:110`, `Areas/Admin/Views/Rapor/Index.cshtml:54` |
| 5.2 | API'de LINQ ile filtreleme — **4 örneğin dördü de var** | ✅ | Tüm hekimler + uzmanlık filtresi `VeterinerlerApiController.cs:42` · Tarih+hizmet için uygun hekimler `VeterinerlerApiController.cs:75` → `MusaitlikServisi.cs:141` · Aşı geçmişi/yaklaşan `EvcilHayvanlarApiController` → `AsiServisi.cs:54` · Hizmete göre sayı + ciro (GroupBy) `RaporlarApiController` → `Services/RaporServisi.cs` |
| 5.3 | Entity yerine DTO; doğru durum kodları | ✅ | `Dtos/ApiDtolari.cs:6`, `Dtos/DtoDonusturucu.cs` · 200/201 `RandevularApiController.cs:76` (`CreatedAtAction`) · 400 `:71` · 404 `:52` · 401/403 `Program.cs:80` · Belgeleme `/scalar` (`Program.cs:179-180`) |

### 6. MCP

| Madde | İster | Durum | Nerede |
|---|---|---|---|
| 6.1 | AI asistanının çağırabileceği MCP sunucusu | ✅ | `Program.cs:121-141` (kayıt), `Program.cs:186` (`MapMcp("/mcp")`) · `Mcp/KlinikAraclari.cs` |
| 6.2 | En az 3 araç, en az biri yazan | ✅ | 6 araç: `hizmetler`, `hayvanlarim`, `randevularim`, `uygun_saatler` (`KlinikAraclari.cs:98`), `asi_takvimi` (`:126`), **`randevu_olustur`** (`:157`, yazan; çakışmada nedeni döner) |
| 6.3 | MCP, MVC ve REST aynı servis katmanı | ✅ | `randevu_olustur` → `RandevuServisi.OlusturAsync` (`KlinikAraclari.cs:184`) = `RandevuController` = `RandevularApiController`. `uygun_saatler` = `/api/veterinerler/uygun` = randevu formu → `MusaitlikServisi.UygunHekimleriGetirAsync`. `asi_takvimi` = `/api/evcil-hayvanlar/{id}/asilar` → `AsiServisi.AsiTakvimiAsync` |
| 6.4 | MCP ucu yetkilendirmeli; yalnız ilgili kullanıcının verisi | ✅ | Bearer anahtar: `Mcp/McpAnahtarKimlikDogrulama.cs` · `Services/McpAnahtarServisi.cs` (yalnız SHA-256 özet saklanır) · `Models/McpErisimAnahtari.cs` · politika `Program.cs:129` · anahtar üretme `Controllers/ProfilController.cs` · veri süzme `KlinikAraclari.cs:22` (`KullaniciBilgisi` → servis) |
| 6.5 | ModelContextProtocol.AspNetCore; MCP Inspector ile test | ✅ | Paket `VeterinerKlinigi.csproj` (2.4.0) · Inspector çıktısı `docs/ekran-goruntuleri/17-mcp-inspector-cli.png` · bağlanma: README §7.3, `.vscode/mcp.json` |

### Projeden Beklenenler

| İster | Durum | Nerede |
|---|---|---|
| Front-End | ✅ | Razor + Bootstrap 5 + jQuery; `Views/Shared/_Layout.cshtml` (role göre menü), `wwwroot/css/site.css` |
| Tam CRUD: Hizmet | ✅ | `Areas/Admin/Controllers/HizmetController.cs` (Index, Detay, Ekle, Duzenle, Sil) |
| Tam CRUD: Veteriner | ✅ | `Areas/Admin/Controllers/VeterinerController.cs` |
| Tam CRUD: Evcil Hayvan | ✅ | `Controllers/EvcilHayvanController.cs` |
| Tam CRUD: Aşı Kaydı | ✅ | `Controllers/AsiKaydiController.cs` (liste/detay hayvan detay sayfasında) |
| Tam CRUD: Randevu | ✅ | Üye: `Controllers/RandevuController.cs` (Olustur, Index, Detay, Duzenle, Iptal) · Admin: `Areas/Admin/Controllers/RandevuController.cs` (+ kalıcı Sil) |
| Validation istemci + sunucu | ✅ | `ViewModels/*.cs` DataAnnotations · sunucu `ModelState.IsValid` (her POST) · istemci `_ValidationScriptsPartial` (jquery.validate + unobtrusive), örn. `Views/Hesap/Kayit.cshtml:61` · servis kuralları `ModelState.AddModelError` ile aynı forma |
| Admin paneli | ✅ | `Areas/Admin/` · `Areas/Admin/Controllers/PanelController.cs` (özet sayılar) |
| Kayıt / üye olma | ✅ | `Controllers/HesapController.cs:18`, `Views/Hesap/Kayit.cshtml`; her zaman `Uye` rolü (`HesapController.cs:76`) |
| En az 3 rol; admin `g221210024@sakarya.edu.tr / sau` | ✅ | `Data/Roller.cs` · seed `Data/VeriTohumlayici.cs:21` (parola hash'li) · `[Authorize(Roles = ...)]` her controller'da |
| Authorization (id değiştirerek başkasınınkini görememe) | ✅ | `Services/YetkiSorgulari.cs:24` (`ErisilebilirHayvanlar`), `:39` (`ErisilebilirRandevular`) · test sonuçları README §8 |
| LINQ kullanan en az bir API | ✅ | 5.2 satırına bakın |
| En az 3 araçlı MCP | ✅ | 6.2 satırına bakın |

### GitHub ve Teslim

| İster | Durum | Nerede |
|---|---|---|
| Public repo | ✅ | https://github.com/tunahntopbas/veteriner-klinigi |
| İlk commit'te README.md | ✅ | commit `91bb0e1` |
| Düzenli commit; **en az 10 farklı günde 10 commit** | 🔁 | Kod tarafı bugün bitti; geri kalan günlerde küçük, gerçek iyileştirmelerle commit atılacak (bkz. §5). **Commit tarihleri asla geriye/ileriye alınmaz.** |
| Bağlantı dizesi parolası / API anahtarı depoda yok | ✅ | `.env` ve `Yuklemeler/` `.gitignore`'da · bağlantı dizesi `dotnet user-secrets`'ta (`Program.cs:46`) · depoda yalnız `.env.example` · MCP anahtarları veritabanında yalnız özet olarak |
| Rapor = kökteki README.md (başlık, no, ad, grup, GitHub en başta) | ✅ | `README.md` |
| Tanıtım, ER diyagramı, ekran görüntüleri | ✅ | README §1, §4 (Mermaid), §5 |
| REST uçları + örnek istek/cevap | ✅ | README §6 (gerçek cevaplardan alındı) |
| MCP araçları + MCP istemcisinden çağrı ekran görüntüsü | ✅ | README §7 · `docs/ekran-goruntuleri/17-mcp-inspector-cli.png` |

---

## 2. Mimari Özeti

```
Tarayıcı ──form POST──▶ MVC Controller'lar ─┐
Tarayıcı ──jQuery AJAX─▶ REST API (/api) ───┼──▶ Servis katmanı ──▶ UygulamaDbContext (EF Core) ──▶ PostgreSQL (Docker)
AI asistanı ──JSON-RPC─▶ MCP (/mcp) ────────┘     (iş kuralları)
```

- **Tek proje**, hocanın KampusKitap örneğindeki gibi sade klasörler. Repository / AutoMapper yok.
- **Models** = entity (tablo). **ViewModels** = form modeli (`XxxVM`). **Dtos** = API'nin dışarı verdiği şekil.
- **Services**: her biri `IXxxServisi` + `XxxServisi`. Controller'lar yalnızca servisi çağırır, sonucu gösterir.
  Servisler `IslemSonucu` döner (`Basarili`, `Hata`, `Bulunamadi`); aynı sonucu
  MVC `ModelState`/`NotFound()` ile, REST `400/404` ile, MCP Türkçe metinle gösterir.
- **Kullanıcı bilgisi**: `KullaniciBilgisi(Id, AdminMi, VeterinerMi)` controller'da `User.Bilgi()` ile, MCP'de
  `HttpContext.User`'dan oluşturulup servise geçer. Servisler `HttpContext` bilmez → üç yoldan da aynı çalışır.
- **Areas**: `Admin` (yönetim), `Hekim` (veteriner paneli). Alanın adı "Veteriner" olsaydı `Models.Veteriner` sınıfıyla
  namespace çakışırdı; bu yüzden "Hekim".
- **Kalıplar**: GET/POST çifti, PRG (Post-Redirect-Get) + `TempData["Mesaj"]`, Tag Helper'lı formlar, partial view'lar
  (`_DurumRozeti`, `_RandevuTablosu`, `_RandevuBilgileri`, `_HayvanKarti`).
- **Açılış**: `Data/VeriTohumlayici.cs` migration'ları uygular, rolleri, admin'i ve örnek verileri ekler (yalnızca boşsa).

---

## 3. Adım Günlüğü

| # | Ne yapıldı | Önemli dosyalar | Commit |
|---|---|---|---|
| 1 | README ile ilk commit, public repo | `README.md` | `91bb0e1` |
| 2 | MVC iskeleti, Docker PostgreSQL, paketler, user-secrets | `docker-compose.yml`, `.env.example`, `.gitignore`, `VeterinerKlinigi.csproj` | `fb7a35e` |
| 3 | Entity'ler, DbContext, ilk migration | `Models/*`, `Data/UygulamaDbContext.cs`, `Migrations/` | `239a125` |
| 4 | Identity, 3 rol, örnek veriler, kayıt/giriş | `Program.cs`, `Data/Roller.cs`, `Data/VeriTohumlayici.cs`, `HesapController.cs` | `d10edbe` |
| 5 | Servis katmanı (klinik, hizmet, uzmanlık, hekim, hayvan, aşı, fotoğraf) | `Services/*` | `527a2f1` |
| 6 | Müsaitlik, randevu (çakışma, durumlar, muayene notu), rapor servisleri | `MusaitlikServisi.cs`, `RandevuServisi.cs`, `RaporServisi.cs` | `01293b9` |
| 7 | REST API + DTO + OpenAPI/Scalar | `Controllers/Api/*`, `Dtos/*` | `2d85cd4` |
| 8 | MCP sunucusu, Bearer anahtar, 6 araç | `Mcp/*`, `McpAnahtarServisi.cs` | `02afa23` |
| 9 | Admin paneli | `Areas/Admin/*` | `9b11eb7` |
| 10 | Üye ve hekim sayfaları | `Controllers/*`, `Views/*`, `Areas/Hekim/*` | `6f1df64` |
| 11 | Uçtan uca tarayıcı testi, hata sayfası, tema düzeltmeleri, ekran görüntüleri | `HomeController.Durum`, `site.css`, `docs/ekran-goruntuleri/` | `d444ab5` |
| 12 | **Hata düzeltmesi:** tamamlanan randevu saati boş sayılıyordu → `DoluSayilanDurumlar`'a eklendi. MCP Inspector çıktısı, SQL betiği | `MusaitlikServisi.cs:37`, `docs/veritabani-olusturma.sql` | `e6967d6` |
| 13 | README raporu ve bu dosya | `README.md`, `docs/GELISTIRME_NOTLARI.md` | (bu commit) |

**Karşılaşılan sorunlar ve çözümleri** (sunumda "zorlandığın yer neresiydi?" sorusuna)
- *Rapor sorgusu SQL'e çevrilemedi*: `GroupBy` içinde record constructor ve filtreli `Sum` EF Core'da çevrilemiyor.
  Önce anonim tipe koşullu `Sum` (`Sum(r => r.Durum == Tamamlandi ? r.Ucret : 0)`) ile projeksiyon, sonra bellekte DTO'ya dönüştürme.
- *Ondalık sayılar*: tr-TR'de ayırıcı virgül, tarayıcı nokta gönderiyor → kültürü kopyalayıp yalnız sayı ayırıcısını değiştirdik (`Program.cs:21`).
- *Area adı çakışması*: "Veteriner" area'sı `Models.Veteriner` ile çakıştı → "Hekim".
- *Tamamlanan randevu çakışması*: Test sırasında bulundu; tamamlanmış randevunun saatine yeni randevu alınabiliyordu. Düzeltildi.

---

## 4. Sunum Soru-Cevapları

**S: Admin ve kullanıcılar için ayrı SQL tablosu var mı?**
Hayır. Tek kullanıcı tablosu `AspNetUsers` (sınıfı `Models/Kullanici.cs`, `IdentityUser`'dan türer). Roller `AspNetRoles`,
kim hangi rolde `AspNetUserRoles` ara tablosunda. Admin, hekim ve üye aynı tabloda; fark yalnızca rol. Hekime özel bilgiler
(unvan, uzmanlık, müsaitlik) ayrı `Veterinerler` tablosunda ve `KullaniciId` ile kullanıcıya bağlı (1:0..1).
Neden? Giriş, parola, kilitleme tek yerde olur; bir kişiye ikinci rol vermek tek satırdır; aynı e-posta iki tabloda olmaz.

**S: REST API nerede ve nasıl kullanılıyor?**
- *Nerede yazıldı:* `Controllers/Api/` — `[ApiController]` + `[Route("api/...")]`. Entity değil DTO döner (`Dtos/ApiDtolari.cs`).
- *Nerede tüketiliyor (site içinde):* jQuery `$.getJSON` ile 4 sayfa: Hekimlerimiz (`/api/veterinerler`), Randevu formu
  (`/api/veterinerler/uygun` → boş saat düğmeleri), Hayvan detayı (`/api/evcil-hayvanlar/{id}/asilar` → yaklaşan aşılar),
  Admin raporu (`/api/raporlar/hizmet-ozeti`).
- *Dışarıdan:* `/scalar` sayfasından denenebilir; Postman/curl ile de çağrılabilir.
- *Kimlik:* aynı giriş çerezi. Giriş yoksa 401, rol yetmiyorsa 403 (`Program.cs:81-96`, yönlendirme yerine durum kodu).

**S: LINQ nerede?**
`VeterinerlerApiController` uzmanlığa göre `Where(v => v.Uzmanliklar.Any(u => u.Ad.Contains(uzmanlik)))`; `MusaitlikServisi` gün + hizmet + müsaitlik +
dolu randevu filtreleri; `AsiServisi.AsiTakvimiAsync` geçmiş/yaklaşan ayrımı ve sıralama; `RaporServisi` `GroupBy` + `Count` + `Sum`.

**S: MCP nedir, nerede tüketiliyor?**
MCP, AI asistanının uygulamadaki araçları çağırmasını sağlayan protokol. Bizim uygulama **sunucu** (`/mcp`), asistan **istemci**.
Tüketen taraf: MCP Inspector (testte kullandık, ekran görüntüsü 17), VS Code Copilot Agent modu (`.vscode/mcp.json`),
Claude Desktop (`mcp-remote` ile). Asistan, kullanıcının sorusuna göre hangi aracı çağıracağına araçların `[Description]`
metinlerine bakarak kendisi karar verir. Ücretli abonelik gerekmez; Inspector ücretsizdir.

**S: MCP'de başkasının verisini görmeyi nasıl engelliyorsun?**
Kullanıcı Profil sayfasında `vk_...` anahtarı üretir; veritabanında yalnız SHA-256 özeti tutulur. Asistan her istekte
`Authorization: Bearer vk_...` gönderir. `McpAnahtarKimlikDogrulama` anahtarın sahibini bulur ve isteği o kullanıcı yapıyormuş
gibi çalıştırır. Araçlar servise o kullanıcının `KullaniciBilgisi`'ni verir; servis sorguları `YetkiSorgulari` ile süzülür.
Anahtarsız istek 401. İptal edilen anahtar anında geçersiz.

**S: Yüklenen fotoğrafları nerede tutuyorsun?**
Dosyanın kendisi **diskte**, `VeterinerKlinigi/Yuklemeler/hayvanlar/` klasöründe, `wwwroot` **dışında**. Veritabanında yalnızca
dosya adı (`EvcilHayvanlar.FotografDosyaAdi`). Dosya adı rastgele GUID (`3f2a...b1.jpg`), kullanıcının verdiği ad kullanılmaz.
- *Neden wwwroot dışı?* wwwroot'taki her dosya adresi bilen herkese açıktır. Bizde fotoğraf `/EvcilHayvan/Fotograf/{id}`
  action'ından geçer: önce "bu hayvan bu kullanıcının mı?" kontrolü, sonra `PhysicalFile` (`EvcilHayvanController.cs:52`).
- *Kontroller:* yalnız .jpg/.jpeg/.png, en fazla 2 MB, dosyanın ilk baytları gerçekten JPEG/PNG mi (`FotografServisi.cs:90-91`).
- `Yuklemeler/` `.gitignore`'da → kullanıcı fotoğrafları depoya girmez. Hayvan silinince ya da fotoğraf değişince eski dosya silinir.

**S: DI yaşam döngüleri (Scoped / Singleton / Transient)?**
- `AddDbContext` → **Scoped**: her HTTP isteğine bir DbContext.
- Servisler → **Scoped**: DbContext'i kullandıkları için onunla aynı ömürde olmalılar. Singleton bir servis scoped DbContext'i
  tutsaydı farklı istekler aynı DbContext'i paylaşırdı (thread-safe değil, veri karışır); ASP.NET Core bunu açılışta hata olarak yakalar.
- `FotografServisi` → **Singleton**: durum tutmaz, yalnızca klasör yolunu bilir.
- Transient kullanmadık (her istendiğinde yeni nesne; hafif, durumsuz yardımcılar için).
Kayıtlar: `Program.cs:99-118`.

**S: Çakışmayı nasıl hesaplıyorsun?**
Yeni randevu `[yeniBas, yeniBit)`, `yeniBit = yeniBas + hizmet.SureDakika`. Aynı hekimin "dolu sayılan" (Bekliyor, Onaylandi,
Tamamlandi) her randevusu `[mevcutBas, mevcutBit)` için:
`yeniBas < mevcutBit && mevcutBas < yeniBit` → çakışma. Örnek: mevcut 10:00–10:30, yeni 10:15–10:45 → `10:15 < 10:30` ve
`10:00 < 10:45` → çakışır. Yeni 10:30–11:00 → `10:30 < 10:30` yanlış → çakışmaz (uç uca olabilir).
Sıralı kontroller (`MusaitlikServisi.RandevuUygunMuAsync`): geçmiş tarih mi → hizmet/hekim aktif mi, hekim bu hizmeti veriyor mu →
klinik o gün/saatte açık mı → hekimin müsaitlik aralığına başlangıç VE bitiş sığıyor mu → çakışma. Her biri Türkçe neden döner.
*Aynı anda iki istek?* `OlusturAsync` transaction içinde `pg_advisory_xact_lock(veterinerId)` alır (`RandevuServisi.cs:87`):
aynı hekime eşzamanlı iki istek sıraya girer, ikincisi birincinin kaydını görür.
Reddedilen/iptal edilen randevu saati tekrar boşa çıkar.

**S: IDOR (adres çubuğunda id değiştirme) korumasını nasıl yaptın?**
Her sorgu önce `ErisilebilirHayvanlar(kb)` / `ErisilebilirRandevular(kb)` ile süzülür (`Services/YetkiSorgulari.cs`):
Admin hepsini, hekim yalnız kendi randevularını, üye yalnız kendi hayvanlarını/randevularını görür. Sonra `FirstOrDefault(id)`.
Başkasının kaydı süzgeçte elendiği için "yok" çıkar → **404**. 403 değil 404 dönmemizin nedeni: "böyle bir kayıt var ama sana
yasak" bilgisini de vermemek. Aynı kural MVC, REST ve MCP'de geçerli çünkü üçü de aynı servisi kullanıyor.

**S: Randevu durumları nasıl değişiyor?**
`Bekliyor → Onaylandi | Reddedildi | IptalEdildi`, `Onaylandi → Tamamlandi | IptalEdildi`. Diğer geçişler `GecisGecerliMi`
(`RandevuServisi.cs:264`) tarafından reddedilir. Onay/red/tamamla yalnız Admin veya randevunun kendi hekimi. Muayene notu yalnız
Tamamlandi randevuya ve yalnız kendi hekimi tarafından.

**S: Hizmetin fiyatı değişirse eski randevular ne olur?**
Değişmez. Randevu oluşurken süre ve ücret randevu satırına kopyalanır (`RandevuServisi.cs:104-105`). Rapordaki ciro da bu kopyadan hesaplanır.

**S: Validation nasıl çalışıyor?**
ViewModel'de `[Required]`, `[Range]`, `[StringLength]`, `[EmailAddress]`, `[Compare]`. Tag Helper'lar bunları `data-val-*`
özniteliklerine çevirir, jquery.validate tarayıcıda kontrol eder. Sunucuda her POST `ModelState.IsValid` kontrol eder
(JavaScript kapatılsa bile). İş kuralı hataları (çakışma vb.) `ModelState.AddModelError` ile aynı formda gösterilir.
REST'te `[ApiController]` geçersiz gövdede otomatik 400 döner.

**S: Veritabanı parolası nerede?**
Depoda değil. Docker için `.env` (gitignore'da), uygulama için `dotnet user-secrets` (kullanıcının bilgisayarında, proje klasörü dışında).
`appsettings.json`'da bağlantı dizesi yok.

**S: Docker'da PostgreSQL kullanmanın farkı?**
Uygulama açısından fark yok; aynı PostgreSQL. Docker kurulumu tek komutla (`docker compose up -d`) yapar, bilgisayara kalıcı servis
kurmaz, silmesi kolay ve hocanın bilgisayarında da aynı sürümle çalışır. Veriler `veteriner-veri` volume'ünde kalıcıdır.

---

## 5. Sonraki Günler İçin Commit Fikirleri

"En az 10 farklı günde 10 commit" şartı için kalan günlerde **gerçekten yapılan** küçük işler commit'lenir. Commit tarihleri
değiştirilmez. Öneriler:
- MCP istemcisinden (Claude Desktop veya Inspector arayüzü) kendi ekran görüntünü alıp README §7'ye eklemek.
- Hekim paneline tarih filtresi; admin randevu listesine hekim/tarih filtresi.
- Randevu listelerine sayfalama.
- Ana sayfaya "yaklaşan aşılar" hatırlatma kartı (üye için).
- Hizmet raporuna hekime göre kırılım (`GroupBy(r => r.Veteriner)`) ve yeni bir API ucu.
- Muayene notunu üyenin randevu detayında göstermek / yazdırma görünümü.
- Yorumları ve bu dosyayı sunuma göre gözden geçirmek.
