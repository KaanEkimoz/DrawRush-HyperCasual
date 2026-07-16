# CLAUDE.md — DrawRush (DrawAndRush2)

Bu proje ile çalışırken **her konuşmanın başında** aşağıdaki dosyaları okuman zorunludur. Bunlar `Claude-Project-Memory-Template/` klasöründe yaşıyor:

1. **[Claude-Project-Memory-Template/rules.md](Claude-Project-Memory-Template/rules.md)** — Kaan'ın koyduğu, her zaman uyman gereken kurallar. Kod yazmadan, öneri sunmadan önce mutlaka oku. **En üstteki Quick Reference tablosu 30 saniyede taranır.**
2. **[Claude-Project-Memory-Template/memory.md](Claude-Project-Memory-Template/memory.md)** — Kaan ile yaptığın genel konuşmaların hafızası. Kaan'ın tercihleri, çalışma stili, default tercihler tablosu (Render Pipeline, Input System, vs.).
3. **[Claude-Project-Memory-Template/project-memory.md](Claude-Project-Memory-Template/project-memory.md)** — Projenin güncel durumu, yapılacaklar listesi, aşamalar, nerede kaldığımız.

Ek referans (lazım oldukça aç):
- **[Claude-Project-Memory-Template/recipes/asset-pipelines.md](Claude-Project-Memory-Template/recipes/asset-pipelines.md)** — Meshy / Mixamo / Stitch asset üretim tarifleri.

## Kurallar

- Her konuşma açıldığında üç ana dosyayı (rules / memory / project-memory) oku — kullanıcı hatırlatmasa bile.
- Yeni bir karar alındığında, kural konduğunda veya iş tamamlandığında ilgili dosyayı **anında güncelle**. Konuşma sonunu bekleme.
- `project-memory.md` dosyasını her önemli ilerleme sonrası güncelle (tamamlanan iş, yeni TODO, blok olan konu).
- `rules.md` dosyasına sadece Kaan açıkça "bu bir kural" dediğinde veya "bundan sonra hep böyle yap" dediğinde yazı ekle.
- `memory.md` dosyasına Kaan hakkında öğrendiğin kalıcı bilgileri yaz (tercihler, iletişim stili, teknik bilgi seviyesi vb.).

## Proje

- **Ad:** DrawRush (Unity productName: `DrawRush`, Company: `Ekimoz Games`, paket: `com.ekimozgames.drawrush`)
  - ⚠️ 2026-07 rebrand: eski `Studios208` / `DrawAndRush2` isimleri **kullanılmayacak** (Kaan o stüdyodan ayrıldı, haklar kendisinde).
  - **Namespace `DrawRush.*`** (2026-07-17'de `Studios208.DrawRush.*`'tan taşındı), asmdef'ler `DrawRush` + `DrawRush.Tests.EditMode`. Şirket adı bilerek koda girmedi — marka değişse kod etkilenmesin.
  - Kodda `Studios208` geçen **tek yer** `PersistentObject.cs`'teki `[MovedFrom]` satırı: o bir tarihî kayıt, eski serialize veriyi eşliyor. **Yeniden adlandırma.**
- **Tür:** Hyper-casual mobile drawing/puzzle + chase combat
- **Engine / Stack:** Unity 6000.3.12f1 LTS + URP 17.3.0 + Input System + AI Navigation + Cinemachine 2.10.7
- **Hedef:** Android öncelik (mobile portrait), kullanıcı parmak çizimiyle parçaları birleştirip duvarı/şekli tamamlar; bu sırada düşmanlardan kaçar/onlarla çarpışır
- **Repo:** https://github.com/KaanEkimoz/DrawRush-HyperCasual (public, 39★, 6 fork)
- **Default branch:** `master` — Claude `claude-dev` branch'inde çalışmalı

Detaylar `Claude-Project-Memory-Template/project-memory.md` içinde.
