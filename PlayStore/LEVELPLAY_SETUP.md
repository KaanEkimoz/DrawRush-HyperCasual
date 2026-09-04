# DrawRush — LevelPlay (Unity) reklam entegrasyonu

Play Console'da "reklam var + reklam kimliği toplanıyor" beyan edildi. Şu an
**projede reklam SDK'sı YOK**. Yayına çıkmadan bu adımlar tamamlanmalı.

## ✅ App + Ad Unit + SDK + kod (2026-09-04) — ENTEGRE
- LevelPlay/ironSource console'da **Draw Rush** (Android) app'i açıldı.
- **App Key: `27f1c2215`**  ← SDK init (gizli değil, APK içinde gider).
- **Interstitial Ad Unit ID: `gn5krubs30l4gsgf`** (LevelPlay → Setup → Ad units,
  format Interstitial; LevelPlay otomatik 1 network + 1 mediation group kurdu).
- Status: **Temp** (Store availability = "Not live yet"; Play'de yayına çıkınca
  "Live"e döner). COPPA = **Not directed** (13+, çocuklara yönelik değil).
- SDK **`com.unity.services.levelplay` 9.5.1** kuruldu + EDM4A (Mobile Dependency
  Resolver) import edildi. Kod: `Assets/_Project/Ads/` (`AdConfig` + `AdManager`),
  interstitial `LevelFlow.NextLevel`'de her 3 level'de bir gösteriliyor.

## ⚠️ İki kritik uyarı (console'da çıktı)
1. **ironSource Ads direct-demand ağı 30 Nisan 2026'da kapatıldı.** Artık gelir
   **mediation (LevelPlay) + Unity Ads / iSX (ironSource Exchange)** üzerinden.
   LevelPlay SDK ve App Key hâlâ geçerli; sadece talep için **Unity Ads** (ve
   istersen AdMob/AppLovin) adapter'larını Integration Manager'dan eklemen gerekir.
2. **Hesabın "pending approval".** Onaylanana + payment preferences girilene kadar
   **gerçek reklam dolgusu/gelir gelmez.** SDK'yı şimdi entegre edip **test
   reklamlarıyla** (LevelPlay Test Suite) doğrulayabiliriz; gelir onaydan sonra.

---

## 0) Hesap — ✅ TAMAM (App Key alındı: `27f1c2215`)
- ~~Doğru hesaba gir, app oluştur, App Key al~~ → yapıldı.
- Kalan: **Ad Unit ID** gerekirse (SDK 8.x unified API `LevelPlayInterstitialAd`
  ad unit id ister; 7.x `IronSource.Agent` sadece app key ile çalışır) — hangi
  SDK sürümünün geldiği Unity açılınca netleşecek, koda ona göre yazacağım.

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
