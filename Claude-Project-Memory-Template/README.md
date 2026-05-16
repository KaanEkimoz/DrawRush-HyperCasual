# Claude Project Memory Template

> Yeni bir projeye Claude Code ile başlarken; hafıza, kurallar ve süreklilik için MFD'de oturmuş 3-dosya sistemini hazır olarak kurar.

---

## İçinde ne var

| Dosya | Görev |
|---|---|
| `CLAUDE.md` | Proje root'una konur. Claude'a "her oturum başında 3 hafıza dosyasını oku" der + proje üst-tanımı (Engine/Stack). |
| `rules.md` | Kalıcı kurallar — Quick Reference tablosu en üstte, sonra İletişim / İzin Sınırları / Kod Stili / [Unity] Mimari & Klasör / Secrets / Git. |
| `memory.md` | Kaan profili + Default Tercihler tablosu (Render Pipeline / Input / DI / Test / Async / Platform) + tool pattern'leri (MCP, Notion, debug). |
| `project-memory.md` | Projenin **canlı durumu**. Boş iskelet — yer tutucular dolduruldukça canlanır. ADR tablosu, Roadmap, Asset envanteri. |
| `recipes/asset-pipelines.md` | Meshy / Mixamo / Stitch / PDF asset üretim tarifleri — referans, lazım olunca aç. |
| `.gitignore` | Unity standardı + Secrets exclusion. Yeni proje ilk commit'inden itibaren güvenli. |
| `.gitattributes` | Git LFS — FBX/PNG/PSD/WAV vs. binary'leri LFS'e gönderir. Repo şişmesin. |
| `.editorconfig` | `_camelCase`, `var` policy, file-scoped namespace, indent — `rules.md` kurallarının yarısını otomatik enforce eder. |

---

## Kurulum (yeni proje, 5 dakika)

```bash
NEW_PROJECT="$HOME/Workspace/MyNextThing"
mkdir -p "$NEW_PROJECT"

# Tüm template (markdown + dotfiles + recipes klasörü)
cp -R ~/Desktop/Claude-Project-Memory-Template/. "$NEW_PROJECT/"

cd "$NEW_PROJECT"
git lfs install
git init
git add .gitignore .gitattributes .editorconfig CLAUDE.md rules.md memory.md project-memory.md recipes/
git commit -m "chore: bootstrap claude memory system + unity defaults"
```

Sonra:
1. `CLAUDE.md` aç → ilk satırdaki proje adını / domain açıklamasını değiştir.
2. `project-memory.md` aç → "Mevcut Durum" başlığını doldur (1-2 cümle), "Sıradaki Adım"a ilk 3 task'i ekle.
3. `rules.md` zaten dolu — Unity-spesifik kurallar `[Unity]` etiketli, Unity dışı projede de bilgi olarak kalsınlar (Kaan zamanın %90'ı Unity yapıyor).
4. Claude Code aç (`claude`) → "Bu yeni bir proje, CLAUDE.md'yi okuyup başlayalım" de → Claude 3 dosyayı taraması gerektiğini bilir.

---

## Felsefe

**Hafıza = süreklilik.** Tek oturumda biten iş yok; oturum kapanır, Kaan uyur, ertesi gün yeni `claude` açar — bu 3 dosya sayesinde Claude **nerede kaldığını, niye yaptığını, kuralı ne olduğunu** bilir.

3 katman:
- **rules.md** = sabit yasalar — yeni kural sadece açıkça koyulduğunda eklenir
- **memory.md** = sen + alışkanlıklar — yavaş değişir
- **project-memory.md** = bu hafta — her ilerleme sonrası güncel

Üçü ayrı tutuldu çünkü ezilmesinler — kural state'i ezmesin, state kuralı.

---

## Bakım

- Yeni kural: sadece açıkça "bu bir kural" / "bundan sonra hep böyle yap" dediğinde `rules.md`'ye eklenir.
- Yeni iş pattern (örn. yeni bir MCP entegrasyonu, yeni bir asset üretim akışı): `memory.md` → "Tekrarlayan İş Pattern'leri" bölümüne.
- Önemli ilerleme (commit, milestone, blocker): `project-memory.md` anında güncellenir, oturum sonu beklenmez.
- 3 dosya birlikte kabarmaya başlarsa (>500 satır): consolidation pass — eskimiş satırları sil, gereksiz detayı özetle.

---

## Notlar

- Bu template **Unity-aware** — Unity kuralları `[Unity]` etiketiyle işaretli ama silinmesi önerilmiyor. Kaan ağırlıklı Unity yapıyor; Unity dışı projede o satırlar etiketli durarak referans bilgi olarak kalır.
- `kaan-unity-rules.md` adı MFD'de tarihsel sebeple kullanıldı — bu template'te jenerik `rules.md`. Unity-only proje için istersen `unity-rules.md`'ye rename et.
