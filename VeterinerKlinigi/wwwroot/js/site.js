// ==================================================================
// Pati Veteriner Kliniği — ortak betikler (her sayfada yüklenir)
// ==================================================================

// ------------------------------------------------------------------
// KAYDIRINCA BELİRME (kütüphanesiz)
// "belir" sınıflı bölümler ekrana girince "gorundu" sınıfı alır (şu an ana sayfa kullanmıyor; iç sayfalar için hazır);
// geçişin kendisi CSS'te (site.css → 8. bölüm).
//
// NEDEN IntersectionObserver? Kaydırma olayını (scroll) her pikselde dinlemek yerine
// tarayıcı, öğe görünür alana girdiğinde bize SADECE BİR KEZ haber verir → performanslı.
// NEDEN kütüphane yok? İhtiyaç tek bir sınıf eklemek; AOS/GSAP gibi bir paket gereksiz yük olurdu.
//
// Güvenlik ağları:
//   - Tarayıcı IntersectionObserver desteklemiyorsa ya da kullanıcı "azaltılmış hareket"
//     istiyorsa her şey doğrudan görünür yapılır.
//   - Başlangıç stili CSS'te yalnızca <html class="js"> varken uygulanır (JS yoksa gizli içerik yok).
// ------------------------------------------------------------------
(function () {
    var hedefler = document.querySelectorAll('.belir');
    if (hedefler.length === 0) return;

    var hareketIstemiyor = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (hareketIstemiyor || !('IntersectionObserver' in window)) {
        hedefler.forEach(function (h) { h.classList.add('gorundu'); });
        return;
    }

    var gozlemci = new IntersectionObserver(function (girdiler) {
        girdiler.forEach(function (girdi) {
            if (!girdi.isIntersecting) return;
            girdi.target.classList.add('gorundu');
            gozlemci.unobserve(girdi.target);      // bir kez yeter; geri kaydırınca tekrar oynamaz
        });
    }, {
        threshold: 0.15,                          // öğenin %15'i görününce
        rootMargin: '0px 0px -8% 0px'             // ekranın en altına değmeden biraz önce
    });

    hedefler.forEach(function (h) { gozlemci.observe(h); });
})();

// ------------------------------------------------------------------
// SIRALI BELİRME: "sirali" sınıflı listenin çocuklarına sıra numarası (--i) verilir.
// CSS gecikmeyi bundan hesaplar: transition-delay = --i × 55ms. Liste "belir" içinde
// olduğu için aynı gözlemciyle tetiklenir (üst öğe "gorundu" alınca).
// ------------------------------------------------------------------
document.querySelectorAll('.sirali').forEach(function (liste) {
    Array.prototype.forEach.call(liste.children, function (cocuk, i) {
        cocuk.style.setProperty('--i', Math.min(i, 10));   // en fazla ~0,55 sn bekleme
    });
});

// ------------------------------------------------------------------
// İMLECİ İZLEYEN IŞIK: "isikli" kartlarda farenin konumu CSS değişkenine (--mx, --my) yazılır,
// cam yüzeydeki yumuşak ışık (radial-gradient) oraya kayar. Dokunmatik ekranda ve
// "hareketi azalt" açıkken çalışmaz. requestAnimationFrame: karede en fazla bir güncelleme.
// ------------------------------------------------------------------
(function () {
    if (!window.matchMedia('(hover: hover) and (prefers-reduced-motion: no-preference)').matches) return;
    document.querySelectorAll('.isikli').forEach(function (kart) {
        var bekleyen = null;
        kart.addEventListener('pointermove', function (e) {
            if (bekleyen) return;
            bekleyen = requestAnimationFrame(function () {
                var r = kart.getBoundingClientRect();
                kart.style.setProperty('--mx', (e.clientX - r.left) + 'px');
                kart.style.setProperty('--my', (e.clientY - r.top) + 'px');
                bekleyen = null;
            });
        });
    });
})();
