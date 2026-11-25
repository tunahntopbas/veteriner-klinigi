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
| 5.1 | REST API ile veritabanı iletişimi | ✅ | `Controllers/Api/*.cs` (5 controller). Okuma + yazma (`POST /api/randevular`). Site içi tüketim: `Views/Home/Index.cshtml` (ana sayfadaki canlı boş saatler), `Views/Home/Hekimler.cshtml:22`, `Views/Randevu/Form.cshtml:80`, `Views/EvcilHayvan/Detay.cshtml:110`, `Areas/Admin/Views/Rapor/Index.cshtml:54` |
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
| Front-End | ✅ | Razor + Bootstrap 5 + jQuery; `Views/Shared/_Layout.cshtml` (role göre menü, mobilde offcanvas), `wwwroot/css/site.css` (tasarım dili), `wwwroot/js/site.js` (kaydırma animasyonu), `wwwroot/fonts/` |
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
| 13 | README raporu ve bu dosya | `README.md`, `docs/GELISTIRME_NOTLARI.md` | `8113fac` |
| 14 | **Kod incelemesi düzeltmeleri:** (1) saati gelmemiş randevu "Tamamlandı" yapılamaz (yoksa yarınki randevuya bugünden muayene notu yazılıp ciroya girerdi); (2) saati geçmiş bekleyen randevu onaylanamaz, yalnızca reddedilir; (3) aynı hayvan için de kilit (`pg_advisory_xact_lock(1, hayvanId)`); (4) API'de başkasının hayvanıyla randevu → 404. 47 maddelik uç durum testi geçti | `Services/RandevuServisi.cs` (`DurumDegistirAsync`, `KilitleriAlAsync`), `Controllers/Api/RandevularApiController.cs` | `3c73a71`, `579ffa1` |
| 15 | Hekim paneli ve muayene notu ekran görüntüleri yeni kurala uygun veriyle yeniden alındı (tamamlanan randevu artık geçmiş tarihli) | `docs/ekran-goruntuleri/09-*.png`, `10-*.png` | `aa587cd` |
| 16 | 19.11: kalan takvim ve arayüz planı çıkarıldı (§5) | `docs/GELISTIRME_NOTLARI.md` | `c9ee040` |
| 19 | 25.11: **Üye ekranları.** Hayvan kartı yenilendi: fotoğraf yoksa adın baş harfi, yaş (1 yaşından küçükse ay), kilo, "sıradaki aşı" satırı (30 gün kala safran). Hayvan detayında kimlik şeridi; aşı takvimi API'den gelen yapılan ve planlı dozları tek zaman çizelgesinde, araya "Bugün" çizgisi koyarak gösterir. Aşı kayıtları tablosu dar ekranda alt alta satırlara dönüşür. Kilo Türkçe ondalıkla (4,2 kg). Liste sorgusu aşı kayıtlarını da yükler | `Views/Shared/_HayvanKarti.cshtml`, `Views/EvcilHayvan/Index.cshtml`, `Views/EvcilHayvan/Detay.cshtml`, `Services/EvcilHayvanServisi.cs` (`ListeleAsync` → `Include(AsiKayitlari)`), `wwwroot/css/site.css` (§9 İç sayfalar) | (bu günün commit'i) |
| 18 | 20.11: **3D giriş sahnesi + koyu cam tema.** "/" adresinde Three.js ile dönen Shiba Inu modeli, kaydırmayla yaklaşan cam randevu fişleri, 3 aşamalı anlatım ve "Kliniğe gir" kartı; WebGL yoksa veya "hareketi azalt" açıksa durağan görünüm. Tüm site koyu tema, seçici cam efekti. Ana sayfa `Home/AnaSayfa`'ya taşındı | `Views/Home/Index.cshtml`, `Views/Shared/_GirisLayout.cshtml`, `wwwroot/js/giris-sahnesi.js`, `wwwroot/css/giris-sahnesi.css`, `wwwroot/lib/three/`, `wwwroot/models/`, `wwwroot/images/`, `wwwroot/css/site.css` | `966f14c`, `b30465a`, `216c77c` |
| 17 | 20.11: **Arayüz 1. gün.** Tasarım dili (renk ve yazı token'ları, Bootstrap teması), yeni üst bar (aktif sayfa çizgisi, mobilde sağdan açılan menü), veritabanından çalışma saatleri gösteren alt bilgi, yeni ana sayfa (canlı boş saatler fişi, hizmet/ücret listesi, hekimler, randevu adımları, asistan örneği, çalışma saatleri), kütüphanesiz kaydırma animasyonu | `wwwroot/css/site.css`, `wwwroot/js/site.js`, `wwwroot/fonts/`, `Views/Shared/_Layout.cshtml`, `Views/Home/Index.cshtml`, `ViewModels/AnaSayfaVM.cs`, `HomeController.Index` | `2453ee1`, `93d4770`, `f7a02a2` |

**Arayüz tasarım kararları (20.11)**
- *Fikir: "klinik randevu defteri".* Saat, tarih, süre ve ücretler eş genişlikli rakamlarla (JetBrains Mono) yazılır; alt alta hizalanır, defter gibi okunur.
  Başlıklar Bricolage Grotesque, metin Inter.
- *Renkler:* koyu petrol mürekkep `#13302F` (metin, koyu yüzeyler), hafif yeşile çalan kırık beyaz zemin `#F5F7F4`, adaçayı `#5E8C7B` (ikincil),
  tek vurgu rengi safran `#E9A23B` (yalnızca birincil buton, seçili saat, "bugün" işareti). Randevu durum renkleri vurgu renginden ayrı.
  Safran üstünde yazı beyaz değil koyu: beyaz yazı safran üstünde okunmuyor (kontrast yetersiz).
- *Neden yazı tipleri yerel?* `wwwroot/fonts` altında woff2 dosyaları. Sunumda internet olmayabilir; CDN'e bağlı olsaydı sayfa varsayılan
  fonta düşerdi. Türkçe harfler için "latin-ext" dosyası ayrı; tarayıcı yalnızca gerekeni indirir. Lisans: SIL Open Font License.
- *Bootstrap silinmedi:* ödevde teknoloji olarak isteniyor. Kendi renklerimizi Bootstrap'in CSS değişkenlerine (`--bs-body-color`, buton
  değişkenleri) bağladık; böylece henüz yenilenmeyen sayfalar da aynı renk ve fontu aldı.
- *Kart yalnızca gerçek nesnelerde:* hizmetler fiyat listesi gibi satır satır, hekimler sade sütunlar. Kart görünümü canlı "boş saatler fişi"
  ve hayvan kartı gibi tek başına bir nesne olan şeylerde.
- *Hata düzeltmesi:* üst bara buzlu cam efekti (`backdrop-filter`) verince mobil menü ekran yerine üst bara göre konumlandı ve sayfa
  yana taştı; efekt kaldırıldı, CSS'e not düşüldü.

**Güncelleme (20.11, ikinci tur): 3D giriş sahnesi + koyu cam tema** — kullanıcının isteğiyle.
- *Koyu tema:* `<html data-bs-theme="dark">` ile Bootstrap'in kendi koyu modu açıldı; değişkenleri kendi token'larımıza bağladık
  (zemin `#0B1A1A`, yüzey `#12262A`, metin `#E6EFEC`, soluk metin `#9DB3AD`, vurgu yine tek renk: safran `#F0B24A`).
  Böylece formlar, tablolar, dropdown'lar tek tek yazmadan koyu geldi. Açık temaya özel sınıflar (`bg-white`, `table-light`,
  `bg-light text-dark`) sayfalardan temizlendi. Randevu durum rozetleri koyu zeminde okunacak şekilde yeniden renklendirildi (`.durum-*`).
- *Cam (glass) SEÇİCİ:* yalnızca üst bar, mobil menü, ana sayfadaki canlı boş saatler fişi ve giriş sahnesindeki fişler/kart.
  Formlar ve tablolar düz koyu yüzey: her şey cam olursa metin okunmaz ve efekt ucuzlar. Cam görünsün diye zeminde çok bulanık,
  sabit renk lekeleri var (`body::before`). Tarayıcı `backdrop-filter` desteklemiyorsa `@supports not` ile düz yarı opak yüzey.
- *Üst bardaki cam:* bulanıklık üst barın kendisine değil `::before` katmanına verildi (yukarıdaki offcanvas hatası tekrar olmasın).
- *Rotalar:* `/` artık giriş sahnesi (`HomeController.Index`, ayrı `_GirisLayout`), eski ana sayfa `Home/AnaSayfa`. Logo, giriş/çıkış
  sonrası yönlendirmeler ve hata sayfalarındaki "Ana sayfaya dön" bağlantıları `AnaSayfa`'ya gider.
- *3D için kütüphane:* Three.js r170, `wwwroot/lib/three` (MIT). Model: Quaternius "Shiba Inu", CC0, `wwwroot/models/shiba-inu.glb`
  (0,85 MB, 1812 üçgen, Idle/Walk gibi animasyonlarla). Kaydırma için ek kütüphane yok (GSAP kullanılmadı).
- *Hata düzeltmesi:* .NET'in importmap Tag Helper'ı elle yazdığımız `<script type="importmap">`'in `type` özniteliğini siliyordu;
  `<!script>` ile o etiket için Tag Helper kapatıldı (`_GirisLayout.cshtml`).

**Karşılaşılan sorunlar ve çözümleri** (sunumda "zorlandığın yer neresiydi?" sorusuna)
- *Rapor sorgusu SQL'e çevrilemedi*: `GroupBy` içinde record constructor ve filtreli `Sum` EF Core'da çevrilemiyor.
  Önce anonim tipe koşullu `Sum` (`Sum(r => r.Durum == Tamamlandi ? r.Ucret : 0)`) ile projeksiyon, sonra bellekte DTO'ya dönüştürme.
- *Ondalık sayılar*: tr-TR'de ayırıcı virgül, tarayıcı nokta gönderiyor → kültürü kopyalayıp yalnız sayı ayırıcısını değiştirdik (`Program.cs:21`).
- *Area adı çakışması*: "Veteriner" area'sı `Models.Veteriner` ile çakıştı → "Hekim".
- *Tamamlanan randevu çakışması*: Test sırasında bulundu; tamamlanmış randevunun saatine yeni randevu alınabiliyordu. Düzeltildi.
- *Gelecekteki randevuyu tamamlama*: Kod incelemesinde bulundu; hekim yarınki randevuyu bugünden "Tamamlandı" yapabiliyordu. Artık `BaslangicZamani <= şimdi` şartı var.

**Uç durum testleri (adım 14'te, çalışan uygulamaya karşı, 47/47 geçti)**
Çakışma (farklı hayvan / uzun hizmet / uç uca 10:00–10:30 + 10:30), Pazar kapalı, öğle arası, bitişi müsaitlik dışına taşan saat,
klinik kapanışını aşan hizmet, geçmiş tarih, hekimin vermediği hizmet, başkasının hayvanı (404), Cumartesi çalışmayan hekim,
uygun saat listesinin dolu saatleri göstermesi; geçersiz durum geçişleri (onaylıyı tekrar onaylama, Bekliyor → Tamamlandı,
iptal edileni onaylama), başka hekimin onay/not denemesi (404), üyenin hekim paneline girmesi (engellendi), tamamlanmamışa not,
saati gelmemişi tamamlama, saati geçmiş bekleyeni onaylama, reddedilen saatin yeniden boşalması, düzenlemede kendisiyle çakışmama,
tamamlananı düzenleme/iptal, randevusu olan hizmet/hekimi silme (500 değil, anlaşılır mesaj), rapor (red/iptal sayılmaz,
ciro yalnız tamamlanan), ters tarih aralığı (400). MCP: 6 araç listelendi, çakışma nedeni döndü, başkasının hayvanı bulunamadı, anahtarsız 401.

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
*Aynı anda iki istek?* `OlusturAsync` / `GuncelleAsync` transaction içinde iki kilit alır (`RandevuServisi.KilitleriAlAsync`):
`pg_advisory_xact_lock(0, veterinerId)` aynı hekime, `pg_advisory_xact_lock(1, evcilHayvanId)` aynı hayvana gelen eşzamanlı
istekleri sıraya sokar; ikinci istek birincinin kaydını görür ve "çakışıyor" cevabı alır. İlk sayı kilidin türüdür, böylece
"hekim 3" ile "hayvan 3" aynı kilit sayılmaz. Kilitler hep aynı sırayla alındığı için kilitlenme (deadlock) olmaz.
Ayrıca aynı hayvanın aynı saatte iki randevusu da olamaz (`MusaitlikServisi`, kontrol 6).
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

**S: Tamamlanmamış randevuya muayene notu girilebilir mi?**
Hayır, iki kat koruma var. (1) Not yalnızca "Tamamlandı" durumundaki randevuya ve yalnızca randevunun kendi hekimi tarafından
yazılır (`RandevuServisi.MuayeneNotuKaydetAsync`). (2) Bir randevu ancak saati geldiyse "Tamamlandı" yapılabilir:
`BaslangicZamani > şimdi` ise "Henüz saati gelmemiş randevu tamamlanamaz." hatası döner (`DurumDegistirAsync`, `// ÖDEV 3.4`).
Böylece yarınki randevuya bugünden not yazılamaz ve gerçekleşmemiş randevu ciroya girmez. Tersine, saati geçmiş ama hiç
onaylanmamış bir talep artık onaylanamaz; yalnızca reddedilebilir.

**S: "Şimdi" hangi saate göre?**
Sunucunun yerel saatine (`DateTime.Now`). Klinik saati = sunucu yerel saati (Europe/Istanbul) kabul edildi; veritabanı sütunları
da saat dilimsiz (`timestamp without time zone`). Uygulama UTC'de çalışan bir sunucuya taşınırsa saatler 3 saat kayar; o durumda
`TimeZoneInfo.ConvertTime(DateTime.UtcNow, "Europe/Istanbul")` kullanılmalı. Tek klinik ve yerel çalıştırma için şu hâli yeterli.

**S: Animasyonları nasıl yaptın? Kütüphane kullandın mı?**
İki ayrı yer var. Giriş sahnesi (`/`) Three.js kullanır (aşağıdaki soru). Ana sayfadaki animasyonlarda kütüphane yok. `wwwroot/js/site.js` içinde tarayıcının kendi `IntersectionObserver` API'si var: `belir` sınıflı bölümler ekrana
girince tarayıcı bir kez haber verir, biz de `gorundu` sınıfını ekleriz. Hareketin kendisi CSS'te (`transition`), `site.css` 8. bölüm.
Kaydırma olayını her pikselde dinlemediğimiz için sayfa yorulmaz. Tek "orkestre" an "Randevu nasıl işler" bölümü: adımlar sırayla
gelir, aradaki çizgi dolar (`transition-delay`). Güvenlik ağları:
1. Başlangıç stili yalnızca `<html class="js">` varken uygulanır; JavaScript kapalıysa hiçbir içerik soluk kalmaz.
2. Başlangıç tamamen görünmez değil (`opacity: .35`); animasyon çalışmasa da metin okunur.
3. İşletim sisteminde "hareketi azalt" açıksa (`prefers-reduced-motion`) tüm animasyonlar kapanır.

**S: 3D sahneyi nasıl yaptın? Model nereden, lisansı ne?**
- *Model:* "Shiba Inu", Quaternius'un alçak poligonlu hayvan paketinden; poly.pizza üzerinden indirildi
  (https://poly.pizza/m/y4wdQpg767). Lisansı **CC0 1.0**: kamu malı, atıf zorunlu değil ama README'de belirttik.
  Dosya `wwwroot/models/shiba-inu.glb` (glTF ikili biçimi: geometri + malzeme + iskelet animasyonları tek dosyada).
- *Çizim:* Three.js (`wwwroot/lib/three`, CDN değil → internet gerekmez). `GLTFLoader` modeli yükler; `AnimationMixer` modeldeki
  hazır animasyonları oynatır (aşamaya göre Idle → Walk → Idle_2_HeadLow → Idle_2, `crossFadeTo` ile yumuşak geçiş).
  Işık klasik 3 nokta: ana ışık (gölge düşürür), dolgu ışığı, arkadan adaçayı ve safran renkli kenar ışıkları. Zemin yalnızca gölgeyi
  gösteren saydam bir düzlem (`ShadowMaterial`).
- *Kaydırma:* sayfa 500vh uzunluğunda; sahne `position: sticky` ile ekrana yapışık kalır. JS kaydırma konumunu 0–1 arası bir
  ilerleme sayısına çevirir; her karede gösterilen değer hedefe yumuşakça yaklaşır (lerp). Bu TEK sayı hem Three.js kamerasını
  (yörünge yarıçapı, yükseklik, açı), modelin dönüşünü, ışık şiddetini hem de CSS'teki cam fişleri (`--ilerleme` değişkeni) yönetir.
- *Cam fişler:* düz HTML. CSS 3D: kapsayıcıda `perspective: 900px`, her fişin `translate3d(x, y, z)` değeri var; `--kamera`
  ilerledikçe her fişin z değerine eklenir, yani fişler bize doğru gelip kameranın "içinden" geçer. Opaklık
  `min(uzaktan açılma, yakında kaybolma)` formülüyle CSS'te hesaplanır (`wwwroot/css/giris-sahnesi.css`, yorumlarda ayrıntı).
- *Performans:* piksel oranı en fazla 2 (mobilde 1,5); sekme arka plandayken (`visibilitychange`) ve sahne ekrandan çıkınca
  (`IntersectionObserver`) çizim durur; sayfadan çıkarken GPU kaynakları `dispose` edilir. Mobilde fiş sayısı 8'den 4'e iner.
- *Yedek:* Tarayıcı WebGL çizemiyorsa veya "hareketi azalt" açıksa 3D hiç başlamaz; modelden önceden alınmış bir görüntü, üç cümle ve
  "Kliniğe gir" kartı normal sayfa gibi alt alta görünür. Model yüklenemezse ya da JS hata verirse de aynı görünüme düşer.
  "Atla" ve "Kliniğe gir" sıradan bağlantılar olduğu için her durumda çalışır.

**S: Hayvan detayındaki aşı zaman çizelgesi nasıl oluşuyor?**
Sayfa açılınca jQuery `GET /api/evcil-hayvanlar/{id}/asilar` çağırır. API iki liste döner: `gecmis` (yapılan dozlar)
ve `yaklasan` (sonraki doz tarihi bugün veya sonrası). JS ikisini tek diziye koyup tarihe göre sıralar; ISO tarih
(`2026-12-07`) metin olarak karşılaştırıldığında da doğru sıralandığı için ek tarih kütüphanesi gerekmez. İlk planlı
dozdan hemen önce "Bugün" satırı eklenir. 30 gün ve daha az kalan doz safran renkte. API'den gelen metin `guvenli()` ile
HTML'den arındırılarak yazılır (XSS'e karşı). Aynı API'yi MCP'deki `asi_takvimi` aracı da kullanır, yani web ve yapay
zekâ asistanı aynı kuralla çalışır.

**S: Telefonda tablolar nasıl görünüyor?**
Aşı kayıtları tablosu 576px'ten dar ekranda `.yigin-tablo` sınıfıyla satır satır bloklara dönüşür: başlık satırı
gizlenir, her hücrenin etiketi HTML'deki `data-etiket` özniteliğinden CSS `::before { content: attr(data-etiket) }` ile
yazılır. JS yok, yatay kaydırma yok.

**S: Ana sayfadaki boş saatler nereden geliyor?**
Sayfa açılınca jQuery `GET /api/veterinerler/uygun?hizmetId=1&tarih=...` çağırır (REST API'nin sitedeki bir tüketim yeri daha).
Hangi gün gösterileceği sunucuda, veritabanındaki çalışma saatlerinden hesaplanır: bugün açıksa ve kapanışa muayene süresi kadar
zaman varsa bugün, değilse sonraki açık gün. Saatleri hesaplayan servis, randevu formu ve MCP `uygun_saatler` aracıyla aynı.

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

## 5. Kalan Takvim ve Arayüz Planı

Teslim: **17.12.2026**. Son push en geç **16.12.2026**; teslimden sonra depoda değişiklik yapılmaz.
SABİS'e yalnızca 3 satırlık .txt yüklenir: numara, ad soyad, depo adresi.

"En az 10 farklı günde 10 commit" şartı için iş günlere bölünür, her gün yapılan iş o gün commit'lenir.
Commit tarihleri değiştirilmez.

**Sınırlar**
- Bootstrap 5 + jQuery temel olarak kalır (ödevde teknoloji olarak istenmiş). Ek kütüphane yalnızca giriş sahnesinde: Three.js (20.11'de kullanıcıyla kararlaştırıldı).
- Daha özenli görünüm ana sayfa ve üye ekranlarında olur. Admin ve hekim panelleri sade, okunaklı ve hızlı kalır.
- Backend (servisler, API, MCP) bu aşamada değişmez; değişirse uç durum testleri yeniden çalıştırılır.

**İş listesi** (sıra önerilir, gün sayısı esnek)

| # | İş | Not |
|---|---|---|
| 1 | Tasarım dili: renkler, yazı tipleri, boşluklar, buton/kart stilleri; yeni ana sayfa | `wwwroot/css/site.css` CSS değişkenleri, `_Layout.cshtml` |
| 2 | Ana sayfada kaydırınca beliren bölümler | `IntersectionObserver` + CSS geçişleri, kütüphanesiz; `prefers-reduced-motion` açıksa animasyon yok |
| 3 | Üye ekranları: hayvan kartları, aşı takvimi görünümü | `_HayvanKarti`, `EvcilHayvan/Detay` — **yapıldı (25.11)** |
| 4 | Randevu alma akışı: adım adım seçim (hayvan → hizmet → tarih → hekim/saat), boş saat ızgarası | `Randevu/Form.cshtml`; API çağrısı aynı kalır |
| 5 | Hekim paneli: günlük görünüm, tarih filtresi | `Areas/Hekim` |
| 6 | Admin paneli ve grafikli hizmet raporu | Grafik `<canvas>` ile elle çizilir ya da basit CSS çubukları |
| 7 | Mobil uyum, form hata mesajlarının görünümü | Tüm formlar 400px genişlikte denenir |
| 8 | MCP istemcisinden (Claude Desktop veya Inspector arayüzü) kendi ekran görüntüm | README §7 |
| 9 | Derste yeni işlenen konular projeye uygun yerde gösterilir | Hangi konu hangi dosyada, bu dosyaya yazılır |
| 10 | Son ekran görüntüleri, README kontrolü, sunum provası | `docs/ekran-goruntuleri/`, §4 soru-cevaplar |

İhtiyaç olursa ek işler: randevu listelerine sayfalama, hizmet raporuna hekime göre kırılım (`GroupBy(r => r.Veteriner)`),
üyenin randevu detayında muayene notunu görmesi.
