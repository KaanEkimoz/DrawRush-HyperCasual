# DrawRush — Unity release-hazırlık taraması

Tarih: 2026-09-03 · Sürüm: 1.0.0 (code 1)

## ✅ Sağlam / hazır
- **Android build ayarları:** IL2CPP ✅, ARM64+ARMv7 ✅ (Play ARM64 ister), Portrait ✅, targetSDK 35 / minSDK 26 ✅, Linear color space ✅, URP HighQuality ✅
- Paket/isim doğru (`com.ekimozgames.drawrush`, DrawRush), keystore **set** ✅, 6 Android ikon ✅
- Build'de 2 sahne: `00_SplashScreen` + `01_DrawRushGame` ✅ (splash→oyun `VideoEnd.cs` ile bağlı)
- **7 ses cue'sunun hepsi dolu** (wallRise, win, lose, hit, enemyDie=enemy_pop, coin, uiClick) — sessiz feedback yok ✅
- **0 eksik/kırık script**, konsolda gerçek hata yok ✅
- 35 level, 112 düşman — **hepsinde EnemyDeath** (poof+ses+shrink) ✅
- Zorluk eğrisi, tutorial, win/lose/shop, yıldız puanı, coin, skin sistemi bağlı ✅

## 🟡 Küçük (build anında hallolur)
- `buildAppBundle = False` → AAB üretmek için True olmalı (build adımı ayarlar).

## 🔴 Gerçek eksikler
1. ~~Reklam SDK (LevelPlay) entegre DEĞİL~~ → ✅ **ENTEGRE (2026-09-04, commit `0c09d533`)**.
   LevelPlay 9.5.1 + EDM4A, App Key `27f1c2215`, Interstitial `gn5krubs30l4gsgf`,
   `LevelFlow.NextLevel`'de **her 2 yanma + ≥70 sn** (frequency cap; runtime doğrulandı).
   **Kalan (senin, cihaz/interaktif):** LevelPlay → Integration Manager'dan adapter'lar
   (Unity Ads vb.) + EDM Android Resolve + cihazda LevelPlay Test Suite ile test +
   ironSource hesabı "pending approval" onayı (gerçek dolgu/gelir onaydan sonra).
2. **AAB henüz build edilmedi** — keystore şifresi (sende) + build gerekiyor.

## 🟠 Polish eksikleri (blocker değil, ama iyi olur)
1. ~~Ses/mute butonu YOK~~ → ✅ **EKLENDİ** (HUD'da mute toggle, commit `20bcf1e5`).
2. **Düşman görsel çeşitliliği** — hepsi aynı kırmızı blob mesh; farklı modeller (Meshy/3D gen) kredi ister.
3. (opsiyonel) "Rate us" / haptic feedback (titreşim) — hyper-casual'da yaygın.

## Sonuç
Teknik olarak **yayına çok yakın**. Tek gerçek blocker: reklam SDK + imzalı AAB (ikisi de senin aksiyonun). Mute butonunu istersen hemen eklerim.
