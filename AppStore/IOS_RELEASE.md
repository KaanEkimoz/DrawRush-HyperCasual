# Draw Rush — iOS App Store (ilk sürüm 1.6.0)

## Kimlikler
| | |
|---|---|
| ASC app | **6834031898** — "Draw Rush: Pencil Drawing Game", SKU `drawrush001`, birincil dil en-US |
| Bundle ID | `com.ekimozgames.drawrush` (Team `36XQ3B775S`, Yusuf Kaan Usta) |
| Profil | `Draw Rush App Store` (App Store, Apple Distribution sertifikası, 2027-06-09'a kadar) |
| LevelPlay iOS app | key `298249335`; Interstitial `zw6f608gtz9xwlez`, Rewarded `75tx7fdrr71vcwen` (Reward x1), Banner `uj8pmgtaf84nvjei`; COPPA: Not directed |
| Sürüm | marketing 1.6.0 (Android ile aynı), iOS build numarası kendi sayacı: **1** |
| Cihaz | Yalnız iPhone (`targetDevice` iPhone Only) — iPad ekran görüntüsü gerekmez |

## Build akışı (Claude, CLI)
Unity'nin ana (Android) editörüne dokunmadan, iOS işi `ios-release` dalında, kendi Library klonuyla:
```bash
# 1) Unity → Xcode projesi (Builds/iOS). Cihaz/target ayarı değiştiyse önce rm -rf Builds/iOS.
Unity -batchmode -quit -projectPath . -buildTarget iOS -executeMethod IosBuild.Build -logFile Builds/unity_ios_build.log
# 2) Arşiv (manuel imza; otomatik imza "takımda cihaz yok" diye düşüyor)
cd Builds/iOS && xcodebuild -workspace Unity-iPhone.xcworkspace -scheme Unity-iPhone -configuration Release \
  -destination generic/platform=iOS -archivePath ../archive/DrawRush-<ver>-<build>.xcarchive -allowProvisioningUpdates \
  CODE_SIGN_STYLE=Manual CODE_SIGN_IDENTITY="Apple Distribution" DEVELOPMENT_TEAM=36XQ3B775S \
  PROVISIONING_PROFILE_SPECIFIER_APP="Draw Rush App Store" archive
# 3) Yükleme (Builds/archive/ExportOptions.plist: app-store-connect + upload, manuel profil)
cd ../archive && xcodebuild -exportArchive -archivePath DrawRush-<ver>-<build>.xcarchive \
  -exportOptionsPlist ExportOptions.plist -exportPath export -allowProvisioningUpdates
```
Her yeni yüklemede `PlayerSettings.iOS.buildNumber` artmalı.
`IosBuildPostProcess`: ATT metni yok, privacy manifest tracking=false, `ITSAppUsesNonExemptEncryption=false` (ihracat uyumluluğu sorusu çıkmaz).
UnityAds adaptörü (5.11.0.0) Xcode 26.5 ile sorunsuz link'lendi — Brick Crack'teki gibi Podfile'dan silmek gerekmedi.

## ASC'de hazır olanlar
- 4 dil (en-US, tr, es-ES, es-MX): ad, alt başlık, promosyon metni, açıklama, anahtar kelimeler, destek URL'si (`https://kaanusta.dev`), gizlilik URL'si (`https://kaanusta.dev/drawrush-privacy/`). Metinler: `AppStore/listings/ios_listing.json` (Play metinlerinden).
- Telif: `2026 Ekimoz Games`. Kategori: Games › Casual + Puzzle.
- Ekran görüntüleri: her dilde 5 adet iPhone 6.3" (1206×2622). Play görsellerinden (1080×1920) kâğıt rengiyle üst/alt dolgu (`AppStore/screenshots/<dil>/`). 6.5" (1284×2778) kopyaları da klasörde.

## Kaan'ın yapması gerekenler (beyan / hesap işleri)
1. **Content Rights** (App Information): "No, it does not contain, show, or access third-party content" — Kaan beyan etmeli.
2. **Age Rating** anketi: yalnız *Advertising = Yes* (reklam var), şiddet sorularında karikatür/fantastik çatışma için "Infrequent/Mild" mi "None" mı Kaan karar versin (düşmanlar kovalıyor, kan/ölüm yok). Brick Crack'te sonuç 4+ oldu.
3. **App Privacy** (LevelPlay resmi listesi, ATT yok → tracking **No**):
   | Veri türü | Kullanıcıya bağlı | Takip | Amaç |
   |---|---|---|---|
   | Identifiers › Device ID | No | **No** | Third-Party Advertising |
   | Usage Data › Product Interaction | No | No | Third-Party Advertising |
   | Usage Data › Advertising Data | No | No | Third-Party Advertising |
   | Diagnostics › Performance Data | No | No | Third-Party Advertising |
   Brick Crack'te "tracking Yes" binary'deki ATT ile çelişti diye reddedildi; burada binary'de ATT metni yok, manifest tracking=false → hepsi **No** olmalı.
4. **Pricing and Availability**: Free, tüm ülkeler (DSA trader durumu AB için hâlâ "In Review" olabilir).
5. **App Review Information**: ad, soyad, e-posta, **telefon**; "Sign-in required" işaretsiz. Not önerisi:
   > Draw Rush is a one-finger drawing game. Move with one finger; the pencil draws where it runs. Trace the dotted outline to finish a level while enemies chase you. No account or login. Ads are served by Unity LevelPlay (non-personalised unless the player consents in the EU/UK prompt); the app does not request App Tracking Transparency.
6. Build işlenince sürüme **build 1.6.0 (1)**'i seç → TestFlight'ta kendi iPhone'unda dene → **Add for Review → Submit** (Kaan onayı).

## Bilinen eksikler / sonra
- Ekran görüntüleri Android görsellerinden uyarlandı (üst/alt düz kâğıt bandı). Gerçek 19.5:9 iPhone görüntüleri sonra çekilebilir.
- iOS'ta haptik yok (`Haptics` yalnız Android). iOS'ta cihaz ülkesi okunmadığı için reklam onayı penceresi herkese bir kez çıkar.
- Reklamlar ve oyun **cihazda test edilmedi** (TestFlight ile denenmeli).
