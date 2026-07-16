# CLAUDE.md

Bu proje ile çalışırken **her konuşmanın başında** aşağıdaki dosyaları okuman zorunludur:

1. **[rules.md](rules.md)** — Kaan'ın koyduğu, her zaman uyman gereken kurallar. Kod yazmadan, öneri sunmadan önce mutlaka oku. **En üstteki Quick Reference tablosu 30 saniyede taranır.**
2. **[memory.md](memory.md)** — Kaan ile yaptığın genel konuşmaların hafızası. Kaan'ın tercihleri, çalışma stili, default tercihler tablosu (Render Pipeline, Input System, vs.).
3. **[project-memory.md](project-memory.md)** — Projenin güncel durumu, yapılacaklar listesi, aşamalar, nerede kaldığımız.

Ek referans (lazım oldukça aç):
- **[recipes/asset-pipelines.md](recipes/asset-pipelines.md)** — Meshy / Mixamo / Stitch asset üretim tarifleri.

## Kurallar

- Her konuşma açıldığında üç ana dosyayı (rules / memory / project-memory) oku — kullanıcı hatırlatmasa bile.
- Yeni bir karar alındığında, kural konduğunda veya iş tamamlandığında ilgili dosyayı **anında güncelle**. Konuşma sonunu bekleme.
- `project-memory.md` dosyasını her önemli ilerleme sonrası güncelle (tamamlanan iş, yeni TODO, blok olan konu).
- `rules.md` dosyasına sadece Kaan açıkça "bu bir kural" dediğinde veya "bundan sonra hep böyle yap" dediğinde yazı ekle.
- `memory.md` dosyasına Kaan hakkında öğrendiğin kalıcı bilgileri yaz (tercihler, iletişim stili, teknik bilgi seviyesi vb.).
- Unity dışı projede `[Unity]` etiketli kuralları yok say — silmeye gerek yok, etiketle filtrele.

## Proje

- **Ad:** DrawRush (Unity ProductName: `DrawRush`, Company: `Ekimoz Games`, paket: `com.ekimozgames.drawrush`)
- **Tür:** Hyper-casual mobile drawing/puzzle oyunu
- **Engine / Stack:** Unity 6000.3.12f1 (LTS) — URP 17.3.0, Input System 1.19.0, Cinemachine 2.10.7, AI Navigation 2.0.11, Test Framework 1.6.0
- **Hedef:** Mobil (Android öncelik, portrait orientation) — kullanıcı parmak çizimiyle parçaları birleştirerek duvarı/şekli tamamlar, düşmanlarla çatışır

Detaylar `project-memory.md` içinde.
