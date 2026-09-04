# DrawRush — LevelPlay (Unity) reklam entegrasyonu

Play Console'da "reklam var + reklam kimliği toplanıyor" beyan edildi. Şu an
**projede reklam SDK'sı YOK**. Yayına çıkmadan bu adımlar tamamlanmalı.

> Neden ben tam yapamadım: (1) Chrome'daki ironSource/LevelPlay hesabı **kısıtlı**
> ("You don't have permission to view this data") — **App Key**'e ulaşamadım.
> (2) Ağ adaptörleri **Integration Manager** (interaktif Unity penceresi) ile
> indirilir ve reklamlar **ancak gerçek cihaz build'inde** görünür — bunları bu
> araçlarla yapamam. Aşağısı net yol; App Key'i verirsen kod tarafını ben bağlarım.

---

## 0) Hesap (SENİN İLK İŞİN)
- Doğru ironSource/LevelPlay hesabına gir (kısıtlı seat değil, admin).
- **Monetize → SDK Integration / App Management**'te DrawRush için bir app oluştur
  (Android, `com.ekimozgames.drawrush`), **App Key**'i not al.
- Ad Unit'ler oluştur: en az bir **Interstitial** (ve istersen **Rewarded**, **Banner**).

## 1) SDK'yı ekle
- Unity → Window → Package Manager → **Unity Registry** → "LevelPlay" (`com.unity.services.levelplay`) → Install.
  (veya LevelPlay Unity plugin `.unitypackage`'ini panelden indirip import et.)
- Menüde **LevelPlay → Integration Manager** aç → medi ettiğin ağların (AdMob,
  AppLovin, Unity Ads…) **adapter**'larını "Install/Update" et. (Bu adım şart —
  adapter yoksa reklam gelmez. External Dependency Manager'ı da o kurar.)

## 2) Kodu bağla
- `GameManager` (veya boot objesine) bir **AdManager** koy. Boot'ta init:
  ```csharp
  LevelPlay.Init(APP_KEY);            // App Key'i buraya (namespace SDK sürümüne göre:
  LevelPlay.OnInitSuccess += _ => {}; // com.unity3d.mediation VEYA Unity.Services.LevelPlay)
  var interstitial = new LevelPlayInterstitialAd(INTERSTITIAL_AD_UNIT_ID);
  interstitial.OnAdLoaded += _ => {};
  interstitial.OnAdClosed += _ => interstitial.LoadAd(); // kapanınca yeni yükle
  interstitial.LoadAd();
  ```
- **Interstitial'ı nerede göster:** `Assets/_Project/Core/Scripts/LevelFlow.cs` →
  `NextLevel()` içinde, level ilerlerken. Her level'de değil, **her 2-3 level'de bir**
  (örn. `PlayerProgress.LevelsPlayed % 3 == 0` iken) `interstitial.ShowAd()`.
- **Banner (opsiyonel):** ana menü/level arası. **Rewarded (opsiyonel):** revive /
  ekstra coin (shop ile güzel bağlanır).
- App Key'i koda gömme; bir ScriptableObject config'e koy (GameConfig gibi).

## 3) Test
- Reklamlar **editörde görünmez**. Bir **cihaza build** al, LevelPlay **Test Suite**
  ile test reklamlarını doğrula.
- Data Safety zaten LevelPlay'e göre dolduruldu; gizlilik metni de LevelPlay diyor.

---

## Bana ne lazım
Sadece **App Key** (ve varsa Interstitial/Rewarded Ad Unit ID'leri). Onları verince
AdManager'ı yazıp `LevelFlow`'a bağlarım; sen adapter kurulumunu + cihaz testini yaparsın.
Alternatif: "reklamsız yayınla" de → Ads=Hayır + Data Safety'yi "veri yok"a çekerim,
hemen yayınlanır (reklamı v2'de eklersin).
