// ==================================================================
// GİRİŞ SAHNESİ — 3D köpek + kaydırmayla ilerleyen anlatım
// ------------------------------------------------------------------
// Kütüphane: Three.js r170 (wwwroot/lib/three, MIT lisansı). CDN yok; dosyalar projede.
// Model: "Shiba Inu" — Quaternius, CC0 1.0 (kamu malı), https://poly.pizza/m/y4wdQpg767
//        wwwroot/models/shiba-inu.glb (0,85 MB, 1812 üçgen, iskelet animasyonlu)
//
// GENEL AKIŞ
//   1) Kaydırma konumu → hedef ilerleme (0..1)
//   2) Her karede: gösterilen ilerleme hedefe yumuşakça yaklaşır (lerp) → titreme yok
//   3) Bu TEK sayı hem Three.js kamerasını/ışıklarını/modelini, hem de CSS'teki cam fişleri
//      (--ilerleme değişkeni) yönetir → canvas ile HTML katmanları hep senkron
//
// NEDEN Three.js? Tarayıcının 3D çizim arayüzü WebGL çok alt seviyededir (üçgenler, shader'lar).
// Three.js sahne / kamera / ışık / model yükleme gibi kavramlarla çalışmayı sağlar.
// GSAP gibi ayrı bir kaydırma kütüphanesi KULLANMADIK: ihtiyaç tek bir oran hesabı.
// ==================================================================

import * as THREE from 'three';
import { GLTFLoader } from '/lib/three/examples/jsm/loaders/GLTFLoader.js';

const kok = document.documentElement;
const sahneKutusu = document.getElementById('girisSahne');
const tuval = document.getElementById('sahneTuval');
const yol = sahneKutusu?.parentElement;              // .giris-yolu (500vh uzunluğundaki bölüm)

// Layout'taki ön kontrol 3D'yi açmadıysa (WebGL yok / hareketi azalt) hiçbir şey yapma.
if (kok.classList.contains('sahne-3d') && sahneKutusu && tuval && yol) {
    try {
        baslat();
    } catch (hata) {
        // Beklenmeyen bir hata olursa sahneyi durağan hâline düşür: içerik ve butonlar görünür kalsın.
        console.error('3D sahne başlatılamadı:', hata);
        kok.classList.remove('sahne-3d');
    }
}

function baslat() {
    const mobil = window.matchMedia('(max-width: 767.98px)').matches;

    // ---------------- Çizici (renderer) ----------------
    const cizici = new THREE.WebGLRenderer({ canvas: tuval, antialias: true, alpha: true, powerPreference: 'high-performance' });
    // Ekran yoğunluğu: Retina'da 2'ye kadar, mobilde en fazla 1,5 (piksel sayısı karesiyle artar → pil/ısı).
    cizici.setPixelRatio(Math.min(window.devicePixelRatio, mobil ? 1.5 : 2));
    cizici.outputColorSpace = THREE.SRGBColorSpace;
    cizici.toneMapping = THREE.ACESFilmicToneMapping;      // parlak ışıklarda renkler patlamasın
    cizici.toneMappingExposure = 1.05;
    cizici.shadowMap.enabled = true;
    cizici.shadowMap.type = THREE.PCFSoftShadowMap;        // yumuşak kenarlı gölge

    const sahne = new THREE.Scene();                       // arka plan YOK (alpha): CSS zemini görünür

    // ---------------- Kamera ----------------
    // Dar görüş açısı (32°) → telefoto etkisi, model daha "ürün fotoğrafı" gibi durur.
    const kamera = new THREE.PerspectiveCamera(32, 1, 0.1, 100);

    // ---------------- Işıklar: klasik 3 nokta aydınlatma ----------------
    // Ana (key): sağ üstten, sıcak beyaz, gölge düşürür.
    const anaIsik = new THREE.DirectionalLight(0xfff1dc, 2.4);
    anaIsik.position.set(3.5, 5, 4);
    anaIsik.castShadow = true;
    anaIsik.shadow.mapSize.set(mobil ? 512 : 1024, mobil ? 512 : 1024);
    anaIsik.shadow.camera.near = 1; anaIsik.shadow.camera.far = 20;
    anaIsik.shadow.camera.left = -3; anaIsik.shadow.camera.right = 3; anaIsik.shadow.camera.top = 3; anaIsik.shadow.camera.bottom = -3;
    anaIsik.shadow.radius = 6;
    sahne.add(anaIsik);
    // Dolgu (fill): soldan, zayıf ve serin; gölgeleri tamamen karartmaz.
    const dolguIsik = new THREE.DirectionalLight(0xdbe8f4, 0.8);
    dolguIsik.position.set(-4, 2, 3);
    sahne.add(dolguIsik);
    // Arka (rim): arkadan, kenar parlaması. Biri adaçayı, biri safran → modelin silueti zeminden ayrılır.
    const arkaAdacayi = new THREE.PointLight(0x7fb0dc, 14, 12, 2);
    arkaAdacayi.position.set(-2.5, 2.2, -2.5);
    sahne.add(arkaAdacayi);
    const arkaSafran = new THREE.PointLight(0xd9b98a, 8, 12, 2);
    arkaSafran.position.set(2.6, 1.6, -2.2);
    sahne.add(arkaSafran);
    // Ortam: gölgeli taraf simsiyah olmasın.
    sahne.add(new THREE.HemisphereLight(0xf4f8fb, 0xc9d3db, 0.9));

    // ---------------- Zemin: sadece gölgeyi gösteren görünmez düzlem ----------------
    const zemin = new THREE.Mesh(
        new THREE.CircleGeometry(4, 48),
        new THREE.ShadowMaterial({ opacity: 0.16 })
    );
    zemin.rotation.x = -Math.PI / 2;
    zemin.receiveShadow = true;
    sahne.add(zemin);

    // Modeli taşıyan grup: döndürme ve kaydırma bu gruba uygulanır.
    const modelGrubu = new THREE.Group();
    sahne.add(modelGrubu);

    // ---------------- Model yükleme ----------------
    let karistirici = null;          // AnimationMixer: iskelet animasyonlarını oynatır
    const klipler = {};              // ad → AnimationAction
    let oynayan = null;

    new GLTFLoader().load('/models/shiba-inu.glb', (gltf) => {
        const model = gltf.scene;
        model.traverse((o) => {
            if (o.isMesh) { o.castShadow = true; o.receiveShadow = false; }
        });

        // Ölçek ve konum: modelin kutusunu ölç, yüksekliği ~1,5 birim olacak şekilde büyüt,
        // tabanını zemine (y = 0) ve merkezini eksenin üstüne oturt.
        const kutu = new THREE.Box3().setFromObject(model);
        const boyut = kutu.getSize(new THREE.Vector3());
        const olcek = 1.5 / boyut.y;
        model.scale.setScalar(olcek);
        kutu.setFromObject(model);
        const merkez = kutu.getCenter(new THREE.Vector3());
        model.position.x -= merkez.x;
        model.position.z -= merkez.z;
        model.position.y -= kutu.min.y;
        modelGrubu.add(model);

        // Animasyonlar: dosyada aynı klip iki adla var ("Idle" ve "AnimalArmature|Idle"); kısa adları alıyoruz.
        karistirici = new THREE.AnimationMixer(model);
        gltf.animations.filter((k) => !k.name.includes('|')).forEach((k) => { klipler[k.name] = karistirici.clipAction(k); });
        klipDegistir('Idle');
    }, undefined, (hata) => {
        // Model yüklenemezse sahneyi durağan hâle düşür (görsel + metin + buton).
        console.error('Model yüklenemedi:', hata);
        kok.classList.remove('sahne-3d');
        durdur();
    });

    // Aşamaya göre köpeğin hareketi: duruş → yürüyüş → başı eğik bekleme → duruş.
    // crossFadeTo: iki animasyon arasında 0,5 sn'lik yumuşak geçiş.
    function klipDegistir(ad) {
        const yeni = klipler[ad] ?? klipler.Idle;
        if (!yeni || yeni === oynayan) return;
        yeni.reset().play();
        if (oynayan) oynayan.crossFadeTo(yeni, 0.5, false);
        oynayan = yeni;
    }
    const asamaKlipleri = ['Idle', 'Walk', 'Idle_2_HeadLow', 'Idle_2'];

    // ---------------- Boyut ----------------
    function boyutla() {
        const g = sahneKutusu.clientWidth, y = sahneKutusu.clientHeight;
        cizici.setSize(g, y, false);
        kamera.aspect = g / y;
        kamera.updateProjectionMatrix();
    }
    boyutla();
    window.addEventListener('resize', boyutla);

    // ---------------- Kaydırma → ilerleme ----------------
    // Yolun ekranın üstüne değdiği an 0, yolun sonu ekranın altına geldiği an 1.
    // (Yol 500vh, sahne 100vh → kaydırılabilir mesafe = yolun boyu - ekran boyu.)
    let hedefIlerleme = 0;
    let ilerleme = 0;
    function hedefiHesapla() {
        const kutu = yol.getBoundingClientRect();
        const mesafe = kutu.height - window.innerHeight;
        hedefIlerleme = mesafe > 0 ? Math.min(1, Math.max(0, -kutu.top / mesafe)) : 0;
    }
    hedefiHesapla();
    window.addEventListener('scroll', hedefiHesapla, { passive: true });   // passive: kaydırmayı bekletmez

    // Yardımcılar
    const kenet = (x, a, b) => Math.min(b, Math.max(a, x));
    const aralik = (x, bas, bit) => kenet((x - bas) / (bit - bas), 0, 1);   // x, [bas, bit] içinde 0→1
    const yumusak = (t) => t * t * (3 - 2 * t);                              // smoothstep: uçlarda yavaş
    const karistir = (a, b, t) => a + (b - a) * t;                           // doğrusal ara değer (lerp)

    // ---------------- Çizim döngüsü ----------------
    const saat = new THREE.Clock();
    let calisiyor = false;
    let kareNo = 0;
    let sonAsama = -1;
    const bakis = new THREE.Vector3();

    function kare() {
        if (!calisiyor) return;
        kareNo = requestAnimationFrame(kare);
        const dt = Math.min(saat.getDelta(), 0.05);
        const zaman = saat.elapsedTime;

        // 2) Yumuşatma: her karede farkın %8'i kadar yaklaş. Fare tekerleği "adım adım" kaydırsa da
        //    sahne akıcı ilerler. (Saniyeye bağlı katsayı → 60 ve 120 Hz ekranda aynı his.)
        ilerleme = karistir(ilerleme, hedefIlerleme, 1 - Math.pow(0.92, dt * 60));
        if (Math.abs(ilerleme - hedefIlerleme) < 0.0001) ilerleme = hedefIlerleme;
        const p = ilerleme;

        // CSS katmanlarına aynı sayıyı yaz (cam fişler, ilerleme çubuğu).
        sahneKutusu.style.setProperty('--ilerleme', p.toFixed(4));

        // Aşama (0..3): metinler ve son kart CSS'te bu özniteliğe bakar.
        const asama = p < 0.27 ? 0 : p < 0.52 ? 1 : p < 0.78 ? 2 : 3;
        if (asama !== sonAsama) {
            sahneKutusu.dataset.asama = String(asama);
            klipDegistir(asamaKlipleri[asama]);
            sonAsama = asama;
        }

        // Son aşamada masaüstünde model sola kayar, sağda cam kart açılır.
        const sonAsamaOrani = yumusak(aralik(p, 0.74, 0.92));

        // ---- Model: boşta yavaşça döner + kaydırma ek dönüş verir ----
        // Serbest dönüş: zamanla 0,25 rad/sn + kaydırma boyunca 1,25 tur.
        const serbestAci = zaman * 0.25 + p * Math.PI * 2.5;
        // Son aşamada köpek, karta doğru dörtte üç profilden bakacak sabit bir açıya (1,0 rad) yerleşir.
        // Açı sürekli büyüdüğü için hedefin serbest açıya EN YAKIN tur karşılığını seçiyoruz
        // (yoksa köpek 0'a dönmek için birkaç tur geri sarardı).
        const TAM_TUR = Math.PI * 2;
        const sonAci = 1.0 + TAM_TUR * Math.round((serbestAci - 1.0) / TAM_TUR);
        modelGrubu.rotation.y = karistir(serbestAci, sonAci, sonAsamaOrani);
        modelGrubu.position.x = mobil ? 0 : karistir(0, -1.15, sonAsamaOrani);
        modelGrubu.position.y = mobil ? karistir(0, 0.55, sonAsamaOrani) : 0;

        // ---- Kamera: modelin etrafında bir yörüngede ----
        // Yarıçap: başta uzak (6,4), ortada yakın (4,2), sonda biraz geri (5,4).
        const yakinlasma = yumusak(aralik(p, 0, 0.55));
        const geriCekilme = yumusak(aralik(p, 0.7, 1));
        const yaricap = karistir(karistir(6.4, 4.2, yakinlasma), 5.4, geriCekilme) * (mobil ? 1.35 : 1);
        // Yükseklik: yukarıdan bakıştan göz hizasına iner.
        const yukseklik = karistir(2.9, 1.25, yumusak(aralik(p, 0, 0.6)));
        // Açı: kamera da yörüngede çeyrek tur kadar kayar → model hem kendi dönüyor hem biz etrafında geziyoruz.
        const aci = karistir(-0.35, 0.55, p);
        kamera.position.set(Math.sin(aci) * yaricap, yukseklik, Math.cos(aci) * yaricap);
        bakis.set(modelGrubu.position.x * 0.5, mobil ? 0.95 : 0.75, 0);
        kamera.lookAt(bakis);

        // ---- Işık: ilerledikçe safran kenar ışığı güçlenir, sahne ısınır ----
        arkaSafran.intensity = karistir(6, 26, p);
        arkaAdacayi.intensity = karistir(20, 10, p);
        anaIsik.intensity = karistir(2.1, 2.7, yumusak(aralik(p, 0.2, 0.8)));

        karistirici?.update(dt);
        cizici.render(sahne, kamera);
    }

    function baslatDongu() {
        if (calisiyor) return;
        calisiyor = true;
        saat.getDelta();               // durakladıktan sonra dev bir dt ile zıplamasın
        kareNo = requestAnimationFrame(kare);
    }
    function durdur() {
        calisiyor = false;
        cancelAnimationFrame(kareNo);
    }

    // ---------------- Performans: görünmüyorsa çizme ----------------
    // Sekme arka plandayken (visibilitychange) ve sahne ekrandan çıkınca (IntersectionObserver) dur.
    let sahneEkranda = true;
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) durdur(); else if (sahneEkranda) baslatDongu();
    });
    new IntersectionObserver(([girdi]) => {
        sahneEkranda = girdi.isIntersecting;
        if (sahneEkranda && !document.hidden) baslatDongu(); else durdur();
    }).observe(sahneKutusu);

    // Sayfadan çıkarken GPU kaynaklarını bırak.
    window.addEventListener('pagehide', () => {
        durdur();
        sahne.traverse((o) => {
            o.geometry?.dispose();
            [].concat(o.material ?? []).forEach((m) => m.dispose());
        });
        cizici.dispose();
    });

    baslatDongu();
}
