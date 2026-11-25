// ==================================================================
// Pati Veteriner Kliniği — ortak betikler (her sayfada yüklenir)
// Hareketlerin görünümü site.css §8 ve §13'te; burada yalnızca sınıf ekleyen ve
// CSS değişkeni yazan küçük parçalar var. Kütüphane yok (jQuery bile gerekmiyor).
//
// Ortak kural: kullanıcı "hareketi azalt" istiyorsa (prefers-reduced-motion) hiçbir hareket kurulmaz,
// içerik son haliyle görünür. Fareye bağlı efektler (ışık, eğilme, mıknatıs) dokunmatik ekranda kurulmaz.
// ==================================================================
var hareketIstemiyor = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
var fareVar = window.matchMedia('(hover: hover) and (pointer: fine)').matches;

// ------------------------------------------------------------------
// KAYDIRINCA BELİRME (kütüphanesiz)
// "belir" sınıflı bölümler ekrana girince "gorundu" sınıfı alır.
// NEDEN IntersectionObserver? Kaydırma olayını her pikselde dinlemek yerine tarayıcı,
// öğe görünür alana girdiğinde bize SADECE BİR KEZ haber verir → performanslı.
// ------------------------------------------------------------------
(function () {
    var hedefler = document.querySelectorAll('.belir');
    if (hedefler.length === 0) return;
    if (hareketIstemiyor || !('IntersectionObserver' in window)) {
        hedefler.forEach(function (h) { h.classList.add('gorundu'); });
        return;
    }
    var gozlemci = new IntersectionObserver(function (girdiler) {
        girdiler.forEach(function (girdi) {
            if (!girdi.isIntersecting) return;
            girdi.target.classList.add('gorundu');
            gozlemci.unobserve(girdi.target);      // bir kez yeter
        });
    }, { threshold: 0.15, rootMargin: '0px 0px -8% 0px' });
    hedefler.forEach(function (h) { gozlemci.observe(h); });
})();

// ------------------------------------------------------------------
// SIRA NUMARALARI: listelerin çocuklarına --i yazılır; CSS gecikmeyi bundan hesaplar
// (ör. animation-delay = --i × 45ms). En fazla 12: uzun listede son satır da çabuk gelsin.
// ------------------------------------------------------------------
document.querySelectorAll('.sirali, .randevu-tablosu tbody, .hayvan-izgara, .asi-cizelgesi').forEach(function (liste) {
    Array.prototype.forEach.call(liste.children, function (cocuk, i) {
        cocuk.style.setProperty('--i', Math.min(i, 12));
    });
});

// ------------------------------------------------------------------
// ÜST BAR: sayfa kaydırılınca ince gölge (.golgeli) alır.
// ------------------------------------------------------------------
(function () {
    var bar = document.querySelector('.ust-bar');
    if (!bar) return;
    function bak() { bar.classList.toggle('golgeli', window.scrollY > 8); }
    bak();
    window.addEventListener('scroll', bak, { passive: true });
})();

// ------------------------------------------------------------------
// KAYAN GÖSTERGE: üst menüde aktif bağlantının arkasındaki beyaz hap.
// Fare bir bağlantının üzerine gelince oraya kayar, çıkınca aktif olana döner.
// Konum = bağlantının offsetLeft/offsetWidth değeri; hareketi CSS transition yapar.
// ------------------------------------------------------------------
function gostergeyiTasi(gosterge, hedef) {
    if (!hedef) { gosterge.classList.remove('hazir'); return; }
    gosterge.style.width = hedef.offsetWidth + 'px';
    gosterge.style.height = hedef.offsetHeight + 'px';
    gosterge.style.transform = 'translate(' + hedef.offsetLeft + 'px,' + hedef.offsetTop + 'px)';
    gosterge.classList.add('hazir');
}
(function () {
    var menu = document.querySelector('.ust-bar .navbar-nav');
    if (!menu || !window.matchMedia('(min-width: 992px)').matches) return;
    var g = document.createElement('span'); g.className = 'nav-gosterge'; g.setAttribute('aria-hidden', 'true');
    menu.prepend(g);
    var aktif = function () { return menu.querySelector('.nav-link.aktif'); };
    // Yazı tipi yüklenince genişlikler değişir; o yüzden fontlar hazır olunca da ölçülür
    gostergeyiTasi(g, aktif());
    if (document.fonts) document.fonts.ready.then(function () { gostergeyiTasi(g, aktif()); });
    menu.addEventListener('mouseover', function (e) { var a = e.target.closest('.nav-link'); if (a) gostergeyiTasi(g, a); });
    menu.addEventListener('mouseleave', function () { gostergeyiTasi(g, aktif()); });
    window.addEventListener('resize', function () { gostergeyiTasi(g, aktif()); });
})();

// Durum filtresi (Views/Shared/_DurumFiltresi): seçili çipin arkasındaki koyu hap fareyle kayar.
// Tıklanınca sayfa değişir; View Transitions destekleyen tarayıcıda geçiş yumuşak olur.
document.querySelectorAll('.filtre-cipleri').forEach(function (kap) {
    var g = document.createElement('span'); g.className = 'filtre-gosterge'; g.setAttribute('aria-hidden', 'true');
    kap.prepend(g); kap.classList.add('kayan');
    var secili = function () { return kap.querySelector('a[aria-current="page"]'); };
    function tasi(hedef) {
        gostergeyiTasi(g, hedef);
        kap.querySelectorAll('a').forEach(function (a) { a.classList.toggle('gosterge-ustunde', a === hedef); });
    }
    tasi(secili());
    if (document.fonts) document.fonts.ready.then(function () { tasi(secili()); });
    kap.addEventListener('mouseover', function (e) { var a = e.target.closest('a'); if (a) tasi(a); });
    kap.addEventListener('mouseleave', function () { tasi(secili()); });
});

// ------------------------------------------------------------------
// BAŞLIKLARI PARÇALAMA: ana sayfa selamı harf harf, iç sayfa başlıkları kelime kelime
// bulanıktan netleşir. Ekran okuyucu parçaları değil, başlığın tamamını (aria-label) okur.
// ------------------------------------------------------------------
function parcala(el, harfHarf) {
    if (el.dataset.parcalandi) return;
    el.dataset.parcalandi = '1';
    el.setAttribute('aria-label', el.textContent.trim());
    var sira = 0;
    (function gez(dugum) {
        Array.prototype.slice.call(dugum.childNodes).forEach(function (n) {
            if (n.nodeType === 3) {
                var parca = document.createDocumentFragment();
                // harf harf bölerken birleşik işaretler (ör. "İ" = I + üst nokta) harfinden ayrılmasın
                var birimler = harfHarf ? (n.textContent.match(/\s+|\P{M}\p{M}*/gu) || []) : n.textContent.split(/(\s+)/);
                birimler.forEach(function (b) {
                    if (b === '' ) return;
                    if (/^\s+$/.test(b)) { parca.appendChild(document.createTextNode(b)); return; }
                    var s = document.createElement('span');
                    s.className = 'parca'; s.textContent = b; s.setAttribute('aria-hidden', 'true');
                    s.style.setProperty('--i', sira++);
                    parca.appendChild(s);
                });
                n.replaceWith(parca);
            } else if (n.nodeType === 1) gez(n);
        });
    })(el);
    if (!harfHarf) el.style.setProperty('--adim', '70ms');
    el.classList.add('bolundu');
}
if (!hareketIstemiyor) {
    document.querySelectorAll('.ozet-selam .satir > span').forEach(function (el) { parcala(el, true); });
    document.querySelectorAll('.sayfa-basi h1, .hesap-kutusu h1, .durum-sayfasi h1').forEach(function (el) { parcala(el, false); });
}

// ------------------------------------------------------------------
// SAYAÇ RAKAMLARI: "12.400 TL", "34 dk", "7" gibi metinlerde her rakam 0–9 sütununa dönüşür
// ve kendi rakamına kayar (eski kilometre sayacı gibi). Rakam olmayan karakterler aynen kalır.
// Sayfanın kendi betikleri (ör. geri sayım) metni sonra değiştirirse normal metne döner; sorun olmaz.
// DOMContentLoaded: sayfa betikleri ilk değeri yazdıktan SONRA çalışsın diye.
// ------------------------------------------------------------------
document.addEventListener('DOMContentLoaded', function () {
    if (hareketIstemiyor) return;
    document.querySelectorAll('.sayi-deger, .buyuk-sayi, .geri-sayim-sayi, .cubuk-sayi, [data-sayac]').forEach(function (el, sira) {
        var metin = el.textContent.trim();
        if (!/\d/.test(metin) || el.children.length) return;
        el.setAttribute('aria-label', metin);
        el.textContent = '';
        el.classList.add('sayac-hazir');
        Array.from(metin).forEach(function (ch, i) {
            if (!/\d/.test(ch)) { var d = document.createElement('span'); d.setAttribute('aria-hidden', 'true'); d.textContent = ch === ' ' ? ' ' : ch; el.appendChild(d); return; }
            var kutu = document.createElement('span'); kutu.className = 'sayac-rakam'; kutu.setAttribute('aria-hidden', 'true');
            var sutun = document.createElement('span');
            for (var t = 0; t < 2; t++) for (var r = 0; r < 10; r++) { var s = document.createElement('span'); s.textContent = r; sutun.appendChild(s); }
            sutun.style.transitionDelay = (0.25 + sira * 0.08 + i * 0.06) + 's';
            kutu.appendChild(sutun); el.appendChild(kutu);
            // iki kare bekle: tarayıcı önce 0 konumunu çizsin, sonra hedefe kaysın
            requestAnimationFrame(function () { requestAnimationFrame(function () { sutun.style.transform = 'translateY(-' + ((10 + +ch) * 1.15) + 'em)'; }); });
        });
    });
});

// ------------------------------------------------------------------
// FAREYE BAĞLI ETKİLER (yalnızca fare varsa ve hareket isteniyorsa)
//   - .isik: kartın kenarındaki ışık imleci izler (--mx, --my)
//   - .egik: hayvan kartı imlece göre 3D eğilir, üstünde parlama gezer
//   - .miknatis: birincil düğme imlece hafifçe çekilir
// requestAnimationFrame: karede en fazla bir güncelleme (performans).
// ------------------------------------------------------------------
(function () {
    if (!fareVar || hareketIstemiyor) return;

    function izle(el, fn) {
        var bekleyen = null;
        el.addEventListener('pointermove', function (e) {
            if (bekleyen) return;
            bekleyen = requestAnimationFrame(function () { fn(e, el.getBoundingClientRect()); bekleyen = null; });
        });
    }

    document.querySelectorAll('.ozet-kart, .panel, .adim-kutusu, .randevu-ozeti, .isikli').forEach(function (k) {
        k.classList.add('isik');
        izle(k, function (e, r) {
            k.style.setProperty('--mx', (e.clientX - r.left) + 'px');
            k.style.setProperty('--my', (e.clientY - r.top) + 'px');
        });
    });

    document.querySelectorAll('.hayvan-kart').forEach(function (k) {
        k.classList.add('egik');
        var p = document.createElement('span'); p.className = 'parlama'; p.setAttribute('aria-hidden', 'true'); k.appendChild(p);
        izle(k, function (e, r) {
            var x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
            k.style.transform = 'rotateY(' + ((x - .5) * 10) + 'deg) rotateX(' + ((.5 - y) * 10) + 'deg) translateZ(4px)';
            k.style.setProperty('--px', (x * 100) + '%');
            k.style.setProperty('--py', (y * 100) + '%');
        });
        k.addEventListener('pointerleave', function () { k.style.transform = ''; });
    });

    document.querySelectorAll('.miknatis, .sayfa-basi .btn-primary, .ozet-kart .btn-primary').forEach(function (b) {
        izle(b, function (e, r) {
            b.style.transform = 'translate(' + ((e.clientX - r.left - r.width / 2) * .18) + 'px,' + ((e.clientY - r.top - r.height / 2) * .3) + 'px)';
        });
        b.addEventListener('pointerleave', function () { b.style.transform = ''; });
    });
})();

// ------------------------------------------------------------------
// FORM GÖNDERİLİRKEN: düğme "bekliyor" dönmesi gösterir (çift tıklamayı da engeller).
// İstemci doğrulaması (jquery.validate) varsa ve form geçersizse düğmeye dokunulmaz;
// hatalı alanlar CSS'teki "salla" animasyonuyla kendini gösterir.
// ------------------------------------------------------------------
document.addEventListener('submit', function (e) {
    var form = e.target;
    if (e.defaultPrevented || form.method.toLowerCase() !== 'post') return;
    if (window.jQuery && jQuery(form).data('validator') && !jQuery(form).valid()) return;
    var dugme = e.submitter || form.querySelector('button[type=submit]');
    if (!dugme || !dugme.classList.contains('btn')) return;
    // onsubmit="return confirm(...)" iptal ederse bu satıra gelmeden defaultPrevented olur
    setTimeout(function () { if (!e.defaultPrevented) dugme.classList.add('gonderiliyor'); }, 0);
});
// Geri tuşuyla dönülen sayfada (bfcache) düğme dönmeye devam etmesin
window.addEventListener('pageshow', function () {
    document.querySelectorAll('.gonderiliyor').forEach(function (b) { b.classList.remove('gonderiliyor'); });
});
