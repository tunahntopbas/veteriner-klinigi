# Veteriner Kliniği Randevu ve Hasta Takip Sistemi

| | |
|---|---|
| **Proje** | Veteriner Kliniği Randevu ve Hasta Takip Sistemi (Pati Veteriner Kliniği) |
| **Öğrenci No** | G221210024 |
| **Ad Soyad** | Veli Tunahan Topbaş |
| **Ders Grubu** | 2. Öğretim A Grubu (2A) |
| **GitHub** | https://github.com/tunahntopbas/veteriner-klinigi |

---

## İçindekiler

1. [Proje Hakkında](#1-proje-hakkında)
2. [Kullanılan Teknolojiler](#2-kullanılan-teknolojiler)
3. [Roller ve Yetkiler](#3-roller-ve-yetkiler)
4. [Veritabanı (ER Diyagramı)](#4-veritabanı-er-diyagramı)
5. [Ekran Görüntüleri](#5-ekran-görüntüleri)
6. [REST API](#6-rest-api)
7. [MCP Sunucusu (Yapay Zekâ Entegrasyonu)](#7-mcp-sunucusu-yapay-zekâ-entegrasyonu)
8. [Güvenlik ve Yetkilendirme](#8-güvenlik-ve-yetkilendirme)
9. [Kurulum ve Çalıştırma](#9-kurulum-ve-çalıştırma)
10. [Demo Hesapları](#10-demo-hesapları)
11. [Proje Yapısı](#11-proje-yapısı)
12. [Kullanılan Dış Kaynaklar ve Lisanslar](#12-kullanılan-dış-kaynaklar-ve-lisanslar)

---

## 1. Proje Hakkında

Bu proje, **ASP.NET Core MVC (.NET 10)** ile geliştirilmiş bir veteriner kliniği randevu ve hasta takip sistemidir.
Tek bir klinik ("Pati Veteriner Kliniği") için tasarlanmıştır.

**Klinik tarafı (Admin)**
- Klinik bilgileri ve haftalık **çalışma saatleri** tanımlanır.
- **Hizmetler** süre (dakika) ve ücretleriyle tanımlanır (genel muayene, aşı, kısırlaştırma, diş temizliği...).
- **Veteriner hekimler** giriş hesaplarıyla birlikte oluşturulur; her hekimin **uzmanlık alanları**, **verebildiği hizmetler**
  ve **müsaitlik aralıkları** (örn. Pazartesi 09:00–12:00) belirlenir.
- Tüm randevular listelenir, onaylanır / reddedilir; hizmet bazında **randevu sayısı ve ciro raporu** alınır.

**Hayvan sahibi tarafı (Üye)**
- Üye olur, birden fazla **evcil hayvan** kaydeder (tür, ırk, doğum tarihi, kilo, isteğe bağlı fotoğraf).
- Her hayvanın **aşı kayıtlarını** (aşı adı, yapıldığı tarih, sonraki doz) tutar; yaklaşan aşıları görür.
- Hayvanı, hizmeti ve **o gün o hizmet için boş olan hekimi/saati** seçerek **randevu alır**.
  Sistem; klinik saatini, hekimin müsaitliğini ve hekimin diğer randevularını (hizmet süresiyle birlikte) kontrol eder,
  uygun değilse **nedenini söyleyerek uyarır**.
- Randevu **"Bekliyor"** durumunda oluşur; Admin veya ilgili hekim onaylar ya da reddeder.

**Veteriner hekim tarafı**
- Yalnızca **kendi randevularını** görür, onaylar / reddeder / tamamlar.
- Tamamlanan randevu için **muayene notu** (tanı, tedavi, öneriler) girer.

**Ek olarak**
- Uygulamanın verileri bir **REST API** ile dışarıya açılır (bkz. [Bölüm 6](#6-rest-api)).
- Uygulama, yapay zekâ asistanlarının (Claude Desktop, VS Code, MCP Inspector) çağırabileceği bir **MCP sunucusu** içerir
  (bkz. [Bölüm 7](#7-mcp-sunucusu-yapay-zekâ-entegrasyonu)). Örneğin asistana *"Pamuk'un bir sonraki aşısı ne zaman?"*
  ya da *"Yarın sabah Pamuk için genel muayene randevusu al"* denebilir.

> Ödev maddelerinin kodda **tam olarak nerede** karşılandığı (dosya + satır) [docs/GELISTIRME_NOTLARI.md](docs/GELISTIRME_NOTLARI.md)
> dosyasındaki kontrol listesindedir. Kod içinde de her madde `// ÖDEV 4.2 — ...` biçiminde yorumla işaretlidir.

---

## 2. Kullanılan Teknolojiler

| Katman | Teknoloji |
|---|---|
| Çatı | ASP.NET Core MVC, .NET 10, C# |
| Veritabanı | PostgreSQL 17 (Docker konteynerinde) |
| ORM | Entity Framework Core 10 (Code-First, Migrations) + Npgsql |
| Kimlik | ASP.NET Core Identity (çerez tabanlı giriş, roller, parola hash'leme, hesap kilitleme) |
| Arayüz | Razor View, Tag Helper, Partial View, Bootstrap 5, jQuery (AJAX), jQuery Validation |
| REST API | `[ApiController]`, DTO (record), OpenAPI belgesi + **Scalar** arayüzü (`/scalar`) |
| Yapay zekâ | **Model Context Protocol** — `ModelContextProtocol.AspNetCore` 2.4 (Streamable HTTP, `/mcp`) |

**Mimari:** Tek proje, katmanlı yapı. `Controllers` (MVC) · `Controllers/Api` (REST) · `Mcp` (MCP araçları) →
hepsi **aynı** `Services` katmanını çağırır → `Data/UygulamaDbContext` (EF Core) → PostgreSQL.
İş kuralı (örneğin çakışma kontrolü) yalnızca serviste yazılıdır; bu yüzden MVC formu, REST API ve MCP aracı aynı kurala uyar.

```mermaid
flowchart LR
    T[Tarayıcı<br/>Razor + jQuery] -->|form POST| MVC[MVC Controller'lar]
    T -->|AJAX GET/POST| API[REST API<br/>/api/...]
    AI[AI asistanı<br/>Claude / VS Code / Inspector] -->|JSON-RPC + Bearer| MCP[MCP sunucusu<br/>/mcp]
    MVC --> S[Servis katmanı<br/>RandevuServisi, MusaitlikServisi...]
    API --> S
    MCP --> S
    S --> DB[(PostgreSQL<br/>Docker)]
```

---

## 3. Roller ve Yetkiler

Admin, hekim ve üyeler için **ayrı tablo yoktur**. Herkes tek bir kullanıcı tablosunda (`AspNetUsers`, sınıf adı `Kullanici`)
durur; kim olduğu **rol** ile belirlenir (`AspNetRoles` + `AspNetUserRoles` ara tablosu). Hekimin mesleki bilgileri
(unvan, uzmanlık, müsaitlik) ayrı `Veterinerler` tablosundadır ve `KullaniciId` ile kullanıcıya bağlıdır.

| Rol | Yapabildikleri |
|---|---|
| **Admin** | Klinik, çalışma saati, hizmet, uzmanlık, hekim ve müsaitlik CRUD; tüm randevuları görme, onay/red/iptal/silme; rapor |
| **Veteriner** | Yalnızca kendi randevularını görme, onaylama/reddetme/tamamlama; muayene notu girme |
| **Uye** (hayvan sahibi) | Kendi hayvanları ve aşı kayıtları için CRUD; randevu alma/düzenleme/iptal; MCP erişim anahtarı üretme |

Kayıt sayfasından gelen her kullanıcı **her zaman** `Uye` rolüyle oluşturulur. Admin hesabı ödevde verildiği gibi
uygulama ilk açıldığında otomatik oluşturulur (parola hash'lenerek).

---

## 4. Veritabanı (ER Diyagramı)

Veritabanı EF Core **Code-First** ile oluşturulur (migration: `IlkOlusturma`). Oluşan SQL betiğinin tamamı:
[docs/veritabani-olusturma.sql](docs/veritabani-olusturma.sql).

```mermaid
erDiagram
    AspNetUsers ||--o{ AspNetUserRoles : ""
    AspNetRoles ||--o{ AspNetUserRoles : ""
    AspNetUsers ||--o| Veterinerler : "hekim hesabı"
    AspNetUsers ||--o{ EvcilHayvanlar : "sahibi"
    AspNetUsers ||--o{ McpErisimAnahtarlari : "MCP anahtarı"
    Klinikler ||--o{ CalismaSaatleri : ""
    Klinikler ||--o{ Hizmetler : ""
    Klinikler ||--o{ Veterinerler : ""
    Veterinerler ||--o{ VeterinerMusaitlikleri : ""
    Veterinerler ||--o{ VeterinerUzmanliklari : ""
    Uzmanliklar ||--o{ VeterinerUzmanliklari : ""
    Veterinerler ||--o{ VeterinerHizmetleri : ""
    Hizmetler ||--o{ VeterinerHizmetleri : ""
    EvcilHayvanlar ||--o{ AsiKayitlari : ""
    EvcilHayvanlar ||--o{ Randevular : ""
    Hizmetler ||--o{ Randevular : ""
    Veterinerler ||--o{ Randevular : ""
    Randevular ||--o| MuayeneNotlari : ""

    AspNetUsers {
        string Id PK
        string AdSoyad
        string Email
        string PasswordHash
    }
    AspNetRoles {
        string Id PK
        string Name "Admin, Veteriner, Uye"
    }
    Klinikler {
        int Id PK
        string Ad
        string Adres
        string Telefon
    }
    CalismaSaatleri {
        int Id PK
        int KlinikId FK
        int Gun
        bool Kapali
        time AcilisSaati
        time KapanisSaati
    }
    Hizmetler {
        int Id PK
        string Ad
        int SureDakika
        decimal Ucret
        bool Aktif
    }
    Uzmanliklar {
        int Id PK
        string Ad
    }
    Veterinerler {
        int Id PK
        string KullaniciId FK
        string AdSoyad
        string Unvan
        bool Aktif
    }
    VeterinerMusaitlikleri {
        int Id PK
        int VeterinerId FK
        int Gun
        time BaslangicSaati
        time BitisSaati
    }
    EvcilHayvanlar {
        int Id PK
        string SahipId FK
        string Ad
        string Tur
        string Irk
        date DogumTarihi
        decimal Kilo
        string FotografDosyaAdi
    }
    AsiKayitlari {
        int Id PK
        int EvcilHayvanId FK
        string AsiAdi
        date YapildigiTarih
        date SonrakiDozTarihi
    }
    Randevular {
        int Id PK
        int EvcilHayvanId FK
        int HizmetId FK
        int VeterinerId FK
        datetime BaslangicZamani
        datetime BitisZamani
        int SureDakika "hizmetten kopya"
        decimal Ucret "hizmetten kopya"
        int Durum "Bekliyor/Onaylandi/Reddedildi/Tamamlandi/IptalEdildi"
        string RedNedeni
    }
    MuayeneNotlari {
        int Id PK
        int RandevuId FK "unique"
        string Teshis
        string Tedavi
    }
    McpErisimAnahtarlari {
        int Id PK
        string KullaniciId FK
        string AnahtarHash "SHA-256, unique"
        string OnEk
        bool IptalEdildi
    }
```

**Önemli tasarım kararları**
- `Randevular.SureDakika` ve `Ucret`, randevu anında hizmetten **kopyalanır**. Admin sonradan hizmetin fiyatını değiştirse bile
  eski randevuların ücreti değişmez.
- `Randevular.BitisZamani` = başlangıç + hizmet süresi. Çakışma kontrolü bu sütunla yapılır.
- Hekim ↔ uzmanlık ve hekim ↔ hizmet ilişkileri **çoka çok**tur (`VeterinerUzmanliklari`, `VeterinerHizmetleri` ara tabloları).

---

## 5. Ekran Görüntüleri

| | |
|---|---|
| **Ana sayfa** (klinik çalışma saatleri) | **Hekimler** (liste jQuery ile REST API'den geliyor) |
| ![](docs/ekran-goruntuleri/01-ana-sayfa.png) | ![](docs/ekran-goruntuleri/02-hekimler-api.png) |
| **Kayıt** (sunucu + istemci doğrulaması) | **Evcil hayvan ekleme** (fotoğraf yükleme) |
| ![](docs/ekran-goruntuleri/03-kayit-dogrulama.png) | ![](docs/ekran-goruntuleri/04-hayvan-ekle-formu.png) |
| **Hayvan detayı** (aşı takvimi REST API'den) | **Randevu alma** (uygun hekim/saat API'den) |
| ![](docs/ekran-goruntuleri/05-hayvan-detay-asi-takvimi.png) | ![](docs/ekran-goruntuleri/06-randevu-al-uygun-saatler.png) |
| **Randevu detayı** ("Bekliyor") | **Profil** (MCP erişim anahtarı) |
| ![](docs/ekran-goruntuleri/07-randevu-detay-bekliyor.png) | ![](docs/ekran-goruntuleri/08-profil-mcp-anahtari.png) |
| **Hekim paneli** (kendi randevuları) | **Muayene notu** |
| ![](docs/ekran-goruntuleri/09-hekim-paneli.png) | ![](docs/ekran-goruntuleri/10-hekim-muayene-notu.png) |
| **Admin paneli** | **Hekim müsaitlik aralıkları** |
| ![](docs/ekran-goruntuleri/11-admin-paneli.png) | ![](docs/ekran-goruntuleri/12-admin-hekim-musaitlik.png) |
| **Hizmet yönetimi** | **Hizmet raporu** (REST API'den) |
| ![](docs/ekran-goruntuleri/13-admin-hizmetler.png) | ![](docs/ekran-goruntuleri/14-admin-rapor-api.png) |
| **Tüm randevular** (onay / red) | **REST API arayüzü** (Scalar, `/scalar`) |
| ![](docs/ekran-goruntuleri/15-admin-randevular.png) | ![](docs/ekran-goruntuleri/16-scalar-rest-api.png) |

---

## 6. REST API

REST API, `Controllers/Api/` klasöründeki `[ApiController]` sınıflarıdır. Entity yerine **DTO** döner
(`Dtos/ApiDtolari.cs`) ve doğru HTTP durum kodlarını kullanır. Tüm uçlar uygulama çalışırken
**http://localhost:5078/scalar** adresinden denenebilir (OpenAPI belgesi: `/openapi/v1.json`).

Kimlik doğrulama, siteye giriş yapınca oluşan **çerez** ile yapılır. Giriş yapılmadan korumalı bir uca istek atılırsa
giriş sayfasına yönlendirme (302) **yapılmaz**, doğrudan `401` döner; rol yetmiyorsa `403` döner.

### 6.1 Uç listesi

| Metot | Adres | Yetki | Açıklama | Durum kodları |
|---|---|---|---|---|
| GET | `/api/hizmetler` | Herkes | Aktif hizmetler (süre, ücret) | 200 |
| GET | `/api/veterinerler?uzmanlik=Cerrahi` | Herkes | Tüm hekimler, isteğe bağlı uzmanlığa göre LINQ filtresi | 200 |
| GET | `/api/veterinerler/{id}` | Herkes | Tek hekim | 200, 404 |
| GET | `/api/veterinerler/uygun?hizmetId=1&tarih=2026-12-01` | Herkes | **Belirli tarihte seçilen hizmet için uygun hekimler** ve boş saatleri | 200, 400, 404 |
| GET | `/api/evcil-hayvanlar` | Giriş | Kullanıcının hayvanları | 200, 401 |
| GET | `/api/evcil-hayvanlar/{id}/asilar` | Giriş | **Bir hayvanın aşı geçmişi ve yaklaşan aşıları** | 200, 401, 404 |
| GET | `/api/randevular` | Giriş | Kullanıcının randevuları (Admin: hepsi, hekim: kendisininkiler) | 200, 401 |
| GET | `/api/randevular/{id}` | Giriş | Tek randevu | 200, 401, 404 |
| POST | `/api/randevular` | Giriş | **Yeni randevu oluşturma (veritabanına yazma)** | 201, 400, 401, 404 |
| GET | `/api/raporlar/hizmet-ozeti?baslangic=...&bitis=...` | Admin | **Hizmet türüne göre randevu sayısı ve ciro** (LINQ GroupBy) | 200, 400, 401, 403 |

### 6.2 REST API'nin site içinde tüketildiği yerler

API yalnızca dışarıya açık değil, sitenin kendisi de jQuery `$.getJSON` / `$.ajax` ile kullanıyor:

| Sayfa | Çağrılan uç | Dosya |
|---|---|---|
| Hekimlerimiz (uzmanlık filtresiyle) | `GET /api/veterinerler?uzmanlik=` | `Views/Home/Hekimler.cshtml` |
| Randevu al / düzenle (tarih + hizmet seçilince boş saat düğmeleri) | `GET /api/veterinerler/uygun` | `Views/Randevu/Form.cshtml` |
| Evcil hayvan detayı → "Yaklaşan aşılar" kutusu | `GET /api/evcil-hayvanlar/{id}/asilar` | `Views/EvcilHayvan/Detay.cshtml` |
| Admin → Hizmet raporu (tarih aralığı seçilip tablo doldurulur) | `GET /api/raporlar/hizmet-ozeti` | `Areas/Admin/Views/Rapor/Index.cshtml` |

### 6.3 Örnek istek ve cevaplar

Aşağıdaki cevaplar uygulama örnek verilerle çalışırken alınmıştır.

**Belirli tarihte seçilen hizmet için uygun hekimler**

```http
GET /api/veterinerler/uygun?hizmetId=1&tarih=2026-12-01
```
```json
200 OK
[
  {
    "veterinerId": 2,
    "adSoyad": "Can Demir",
    "unvan": "Vet. Hek.",
    "uzmanliklar": ["Dermatoloji", "Egzotik Hayvan"],
    "bosSaatler": ["10:00", "10:15", "10:30", "...", "15:30"]
  },
  {
    "veterinerId": 1,
    "adSoyad": "Elif Kaya",
    "unvan": "Uzm. Vet. Hek.",
    "uzmanliklar": ["Cerrahi", "Küçük Hayvan"],
    "bosSaatler": ["09:00", "09:15", "...", "11:30", "13:00", "...", "16:30"]
  }
]
```
> Elif Kaya'nın 11:30'dan sonra 13:00'e kadar boş saati yok: müsaitliği 09–12 ve 13–17, Genel Muayene 30 dakika,
> 11:45'te başlayan randevu 12:15'te biterdi ve müsaitlik aralığının dışına taşardı.

**Bir hayvanın aşı geçmişi ve yaklaşan aşıları**

```http
GET /api/evcil-hayvanlar/1/asilar
Cookie: .AspNetCore.Identity.Application=...
```
```json
200 OK
{
  "evcilHayvanId": 1,
  "hayvanAdi": "Pamuk",
  "tur": "Kedi",
  "gecmis": [
    { "id": 2, "asiAdi": "Kuduz Aşısı", "yapildigiTarih": "2026-10-28", "sonrakiDozTarihi": "2027-10-28", "kalanGun": 345, "notlar": null },
    { "id": 1, "asiAdi": "Karma Aşı",   "yapildigiTarih": "2026-09-18", "sonrakiDozTarihi": "2027-09-18", "kalanGun": 305, "notlar": null },
    { "id": 3, "asiAdi": "İç Parazit",  "yapildigiTarih": "2026-09-08", "sonrakiDozTarihi": "2026-12-07", "kalanGun": 20,  "notlar": "3 ayda bir tekrar" }
  ],
  "yaklasan": [
    { "id": 3, "asiAdi": "İç Parazit",  "sonrakiDozTarihi": "2026-12-07", "kalanGun": 20, "...": "..." },
    { "id": 1, "asiAdi": "Karma Aşı",   "sonrakiDozTarihi": "2027-09-18", "kalanGun": 305, "...": "..." },
    { "id": 2, "asiAdi": "Kuduz Aşısı", "sonrakiDozTarihi": "2027-10-28", "kalanGun": 345, "...": "..." }
  ]
}
```

**Hizmet türüne göre randevu sayısı ve ciro raporu** (Admin)

```http
GET /api/raporlar/hizmet-ozeti?baslangic=2026-01-01&bitis=2026-12-31
```
```json
200 OK
[
  { "hizmetId": 1, "hizmetAdi": "Genel Muayene", "randevuSayisi": 4, "tamamlananSayisi": 2, "ciro": 1200.00 },
  { "hizmetId": 6, "hizmetAdi": "Diş Temizliği", "randevuSayisi": 1, "tamamlananSayisi": 0, "ciro": 0.0 }
]
```
> Ciro yalnızca **tamamlanan** randevuların ücretlerinin toplamıdır.

**Randevu oluşturma — başarılı**

```http
POST /api/randevular
Content-Type: application/json

{ "evcilHayvanId": 1, "hizmetId": 1, "veterinerId": 1, "baslangic": "2026-12-01T10:30:00", "sahipNotu": "Kontrol" }
```
```json
201 Created
Location: /api/randevular/6

{
  "id": 6, "evcilHayvanId": 1, "hayvanAdi": "Pamuk", "hizmet": "Genel Muayene", "veteriner": "Elif Kaya",
  "baslangic": "2026-12-01T10:30:00", "bitis": "2026-12-01T11:00:00",
  "sureDakika": 30, "ucret": 600.00, "durum": "Bekliyor", "sahipNotu": "Kontrol"
}
```

**Randevu oluşturma — çakışma (sistem uyarır)**

```http
POST /api/randevular
{ "evcilHayvanId": 1, "hizmetId": 1, "veterinerId": 1, "baslangic": "2026-11-24T14:15:00" }
```
```json
400 Bad Request
{ "hata": "Elif Kaya 24.11.2026 14:00–14:30 arası başka bir randevuda (Genel Muayene). Seçtiğiniz 14:15–14:45 aralığı bununla çakışıyor." }
```

**Randevu oluşturma — eksik alan (otomatik model doğrulaması)**

```json
400 Bad Request
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "EvcilHayvanId": ["EvcilHayvanId zorunludur."] } }
```

**Diğer durum kodları**

| İstek | Sonuç |
|---|---|
| Giriş yapmadan `GET /api/evcil-hayvanlar` | `401 Unauthorized` |
| Üye hesabıyla `GET /api/raporlar/hizmet-ozeti` | `403 Forbidden` |
| ayse@ornek.com ile **başkasının** hayvanı: `GET /api/evcil-hayvanlar/3/asilar` | `404 Not Found` |
| `GET /api/veterinerler/999` | `404 Not Found` + `{ "hata": "Hekim bulunamadı." }` |

---

## 7. MCP Sunucusu (Yapay Zekâ Entegrasyonu)

**MCP (Model Context Protocol)**, bir yapay zekâ asistanının dış uygulamalardaki "araçları" (tool) çağırmasını sağlayan
açık bir protokoldür. Bu projede uygulama bir **MCP sunucusudur**: asistan, kullanıcının adına klinik verilerini okuyabilir
ve randevu oluşturabilir. Asistan **istemci**, bizim uygulamamız **sunucu**dur.

- **Adres:** `POST http://localhost:5078/mcp` (Streamable HTTP taşıma, JSON-RPC 2.0)
- **Kod:** `Mcp/KlinikAraclari.cs` (araçlar), `Mcp/McpAnahtarKimlikDogrulama.cs` (kimlik doğrulama), `Program.cs` (kayıt)
- **Kütüphane:** resmi C# SDK'sı `ModelContextProtocol.AspNetCore`

### 7.1 Araçlar

| Araç | Tür | Parametreler | Ne yapar |
|---|---|---|---|
| `hizmetler` | okuma | – | Hizmetleri id, süre, ücret ile listeler |
| `hayvanlarim` | okuma | – | Kullanıcının evcil hayvanları |
| `randevularim` | okuma | – | Kullanıcının randevuları ve durumları |
| `uygun_saatler` | okuma | `hizmetId`, `tarih` | O gün o hizmet için boş hekimler ve saatler |
| `asi_takvimi` | okuma | `hayvanAdi` | Hayvanın geçmiş ve yaklaşan aşıları ("Pamuk'un sonraki aşısı ne zaman?") |
| `randevu_olustur` | **yazma** | `hayvanAdi`, `hizmetId`, `veterinerId`, `tarih`, `saat`, `not` | "Bekliyor" durumunda randevu oluşturur |

Araçlar iş kuralını kendileri yazmaz: `randevu_olustur`, MVC formu ve `POST /api/randevular` ile **aynı**
`RandevuServisi.OlusturAsync` metodunu çağırır. Bu yüzden çakışma kontrolü üç yolda da aynıdır; çakışma varsa asistan
nedenini Türkçe olarak alır ve kullanıcıya iletir.

### 7.2 Yetkilendirme

MCP ucu herkese açık değildir:
1. Kullanıcı siteye giriş yapar → **Profil** sayfasından bir **MCP erişim anahtarı** üretir (`vk_...`). Anahtar yalnızca bir kez gösterilir;
   veritabanında yalnızca **SHA-256 özeti** saklanır. Anahtar istenince iptal edilebilir.
2. Asistan her istekte `Authorization: Bearer vk_...` başlığını gönderir.
3. `McpAnahtarKimlikDogrulama` anahtarı doğrular ve isteği **anahtarın sahibi** olarak çalıştırır.
4. Araçlar yalnızca o kullanıcının verisini görür. Başka birinin hayvanının adı verilirse "bulunamadı" döner.
5. Anahtarsız istek → `401 Unauthorized`.

### 7.3 Bağlanma

**MCP Inspector (test aracı):**
```bash
npx @modelcontextprotocol/inspector@0.17.2 --cli http://localhost:5078/mcp --transport http \
  --header "Authorization: Bearer vk_ANAHTARINIZ" --method tools/list
```
```bash
npx @modelcontextprotocol/inspector@0.17.2 --cli http://localhost:5078/mcp --transport http \
  --header "Authorization: Bearer vk_ANAHTARINIZ" --method tools/call --tool-name asi_takvimi --tool-arg hayvanAdi=Pamuk
```

**VS Code (GitHub Copilot, Agent modu):** depodaki [.vscode/mcp.json](.vscode/mcp.json) hazırdır. VS Code sunucuyu başlatırken anahtarı sorar.

**Claude Desktop:** `claude_desktop_config.json` içine:
```json
{
  "mcpServers": {
    "pati-veteriner-klinigi": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:5078/mcp", "--header", "Authorization: Bearer vk_ANAHTARINIZ"]
    }
  }
}
```

![MCP Inspector çıktısı](docs/ekran-goruntuleri/17-mcp-inspector-cli.png)

---

## 8. Güvenlik ve Yetkilendirme

- **Başkasının kaydına erişim (IDOR) engeli:** Servis sorguları `YetkiSorgulari` uzantılarıyla kullanıcıya göre filtrelenir.
  Üye adres çubuğuna başkasının kaydının id'sini yazarsa kayıt "yokmuş gibi" davranılır ve **404** döner. Test edilen durumlar:

  | Deneme | Sonuç |
  |---|---|
  | ayse → `/EvcilHayvan/Detay/3` (Mehmet'in hayvanı) | 404 |
  | ayse → Mehmet'in randevusunun detayı | 404 |
  | ayse → `/api/evcil-hayvanlar/3/asilar` | 404 |
  | ayse → `/EvcilHayvan/Fotograf/3` | 404 |
  | Hekim Can Demir → Elif Kaya'nın randevusu | 404 |
  | Üye → `/Admin/Panel` | 403 (Erişim engellendi sayfası) |
  | MCP: ayse'nin anahtarıyla `asi_takvimi(Karabaş)` | "bulunamadı" |

- **Parolalar:** Identity tarafından hash'lenerek saklanır; 5 hatalı girişte hesap 5 dakika kilitlenir.
- **CSRF:** Tüm POST formlarında `[ValidateAntiForgeryToken]`; çerez `HttpOnly` + `SameSite=Lax`.
- **Doğrulama:** ViewModel'lerde DataAnnotations; hem sunucuda (`ModelState`) hem tarayıcıda (jQuery Validation).
- **Fotoğraflar:** `wwwroot` **dışında**, `VeterinerKlinigi/Yuklemeler/hayvanlar/` klasöründe, rastgele (GUID) dosya adıyla saklanır.
  Yalnızca jpg/png ve en fazla 2 MB kabul edilir; dosyanın ilk baytları kontrol edilir. Fotoğraf `/EvcilHayvan/Fotograf/{id}`
  action'ı ile, yetki kontrolünden sonra gönderilir.
- **Gizli bilgiler depoda yok:** Veritabanı parolası yalnızca `.env` (git'e girmez) ve `dotnet user-secrets` içindedir.
  Depoda yalnızca örnek dosya `.env.example` vardır.

---

## 9. Kurulum ve Çalıştırma

**Gerekenler:** .NET 10 SDK, Docker Desktop, (MCP Inspector için) Node.js.

```bash
git clone https://github.com/tunahntopbas/veteriner-klinigi.git
cd veteriner-klinigi
```

**1) Ortam dosyası:** örnek dosyayı kopyalayıp `POSTGRES_PASSWORD` değerini değiştirin.
```bash
cp .env.example .env
```

**2) PostgreSQL'i Docker'da başlatın:**
```bash
docker compose up -d
```

**3) Bağlantı dizesini user-secrets'a yazın** (parola `.env` dosyasındakiyle aynı olmalı):
```bash
cd VeterinerKlinigi
dotnet user-secrets set "ConnectionStrings:VarsayilanBaglanti" "Host=localhost;Port=5432;Database=veteriner_klinigi;Username=veteriner;Password=PAROLANIZ"
```

**4) Veritabanını oluşturun:**
```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```
> Bu adım atlanırsa da sorun olmaz: uygulama açılışta bekleyen migration'ları kendisi uygular ve örnek verileri ekler.

**5) Uygulamayı çalıştırın:**
```bash
dotnet run
```
Tarayıcıda **http://localhost:5078** · REST API arayüzü **http://localhost:5078/scalar** · MCP ucu **http://localhost:5078/mcp**

---

## 10. Demo Hesapları

| Rol | E-posta | Parola |
|---|---|---|
| Admin | `g221210024@sakarya.edu.tr` | `sau` |
| Veteriner | `elif.kaya@patiklinik.com` | `Veteriner1!` |
| Veteriner | `can.demir@patiklinik.com` | `Veteriner1!` |
| Veteriner | `zeynep.arslan@patiklinik.com` | `Veteriner1!` |
| Üye | `ayse@ornek.com` (Pamuk, Boncuk) | `Uye12345!` |
| Üye | `mehmet@ornek.com` (Karabaş) | `Uye12345!` |

---

## 11. Proje Yapısı

```
veteriner-klinigi/
├── docker-compose.yml          PostgreSQL konteyneri
├── .env.example                örnek ortam değişkenleri (.env git'e girmez)
├── .vscode/mcp.json            VS Code MCP istemci ayarı
├── docs/
│   ├── GELISTIRME_NOTLARI.md   ödev maddesi → dosya kontrol listesi, sunum soru-cevap
│   ├── veritabani-olusturma.sql
│   └── ekran-goruntuleri/
└── VeterinerKlinigi/
    ├── Program.cs              servis kayıtları + middleware hattı
    ├── Models/                 entity sınıfları
    ├── Data/                   UygulamaDbContext, Roller, VeriTohumlayici (örnek veri)
    ├── Migrations/
    ├── Services/               iş kuralları (MVC, API ve MCP'nin ortak katmanı)
    ├── ViewModels/             form modelleri
    ├── Dtos/                   REST API DTO'ları
    ├── Controllers/            MVC controller'ları (üye + herkese açık)
    ├── Controllers/Api/        REST API controller'ları
    ├── Mcp/                    MCP araçları ve anahtar doğrulaması
    ├── Areas/Admin/            Admin paneli
    ├── Areas/Hekim/            Veteriner hekim paneli
    ├── Views/                  Razor görünümleri ve partial'lar
    └── wwwroot/                CSS, JS, Bootstrap, jQuery, Three.js, yazı tipleri, 3D model
```

---

## 12. Kullanılan Dış Kaynaklar ve Lisanslar

Tüm dosyalar projenin içindedir (`wwwroot/`); uygulama çalışırken internete bağlanmaz.

| Kaynak | Nerede | Lisans |
|---|---|---|
| Bootstrap 5.3, jQuery, jQuery Validation | `wwwroot/lib/` | MIT |
| [Three.js](https://threejs.org) r170 (giriş sahnesindeki 3D çizim) | `wwwroot/lib/three/` | MIT |
| 3D model: "Shiba Inu", [Quaternius](https://quaternius.com), [poly.pizza/m/y4wdQpg767](https://poly.pizza/m/y4wdQpg767) | `wwwroot/models/shiba-inu.glb` | CC0 1.0 (kamu malı) |
| Yazı tipleri: Bricolage Grotesque, Inter, JetBrains Mono | `wwwroot/fonts/` | SIL Open Font License 1.1 |
