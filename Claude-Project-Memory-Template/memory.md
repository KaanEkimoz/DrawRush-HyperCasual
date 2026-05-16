# Genel Hafıza — Kaan & Claude

> Kaan ile yaptığın konuşmalardan çıkan **kalıcı** bilgiler.
> Projeye özgü şeyler `project-memory.md` içinde, asset pipeline tarifleri `recipes/asset-pipelines.md`'de — burası Kaan'ı tanımak + iş akışı / tool pattern'lerini hatırlamak için.

---

## Kaan

- **E-posta:** kaanekimoz@gmail.com
- **Git ismi:** Yusuf Kaan Usta
- **Dil tercihi:** Türkçe (sohbet, doküman); İngilizce (kod, kod yorumu, commit mesajı)
- **Unity Pro lisansı:** Aktif (Unity AI Assistant + Generators + Unity Points kullanılabilir).

## Çalışma Stili

- **Argümanlı öneri bekler:** "Sen ne önerirsin?" diye sorar — körü körüne onaylama yerine alternatifler + tradeoff ister.
- **Plan onaylanınca uygula, izin sorma:** Plan netse → uygula → özet ver. Scope dışı / destructive op'ta dur.
- **"Tam yetki" çalışma modu:** Büyük scope tasarım refactor'ları için "tam yetki" verir → plan + 8-15 commit'lik kesintisiz seri (her commit ayrı conventional commit), her büyük adım sonunda test green confirm.
- **"Bensiz karar al" yetkisi:** Gerçekten kendi başına karar ver, cevabın sonunda **"bensiz alınan kararlar"** listesi ile transparent ol.
- **Detay + görsellik isteyen doküman sever:** PDF, mermaid diyagramları, multi-agent review pattern.
- **Senior-review pattern:** Büyük refactor öncesi 2 paralel agent (senior Unity dev + senior software eng) ile review yap, raporları konsolide et, plan sun, sonra uygula.
- **Erken karar yerine erteleme:** "Şimdilik X yapalım, sonra karar veririz" sık kullanır.
- **Scope discipline:** Yan iş / scope creep yapılmaz.
- **Context dolunca yeni oturum:** `project-memory.md` yeni oturumun "nerede kaldık" cevabı — daima güncel tut. Session geçişinde Kaan açıkça "her şeyi düzenleyip maine iletip kalıcı dosyalarımızı bilgileri düzenler misin detay atlama" der → memory dosyaları + master merge + tag + push mecburi.
- **Güvenlik tercihi:** API key'leri sohbette paylaşmayı problem etmiyor — uyarıyı bir kez söyle, ısrar etme. **Ama dosyaya / commit'e koymak yasak** (bkz. rules.md → Secrets).

## Mekanik Tasarım Tercihleri (DrawRush'tan öğrenildi)

- **Player → "doğrudan çizen", asset → "uçan/teleport eden" değil.** Yere yatık (TransformZ aligned) persistent TrailRenderer player'da yaşar, asset spawn'lı uçuş efektleri kabul edilmez.
- **Kenar-kenar trail temizliği:** Trail her anchor temasında `Clear()` ile sıfırlanır → "şu an çizilen kenarın geçici göstergesi" semantiği. Kalıcı çizgiler (LineRenderer) bağımsız GameObject'te yaşar.
- **Closed-loop puzzle:** 1 → 2 → … → N → 1 (geri başlangıca). Açık-uçlu chain değil.
- **DrawArea exit progress'i korur** (default). Frustration kaynağı olan "elini drawArea dışına çıkardın → herşey sıfır" davranışı `resetProgressOnAreaExit` flag'iyle opt-in.
- **Combat-puzzle ayrımı:** DrawArea içindeyken enemy damage YOK. Drawing safe zone.

---

## Default Tercihler (yeni Unity projesi açılınca AI'ın varsayacağı)

> Bunlar Kaan'ın geçmiş projelerinden çıkmış default'lar. Yeni proje farklı bir yöne giderse Kaan ilk turda söyler; söylemediği sürece bunları varsay.

| Konu | Default | Sebep |
|---|---|---|
| **Render Pipeline** | URP | Multi-platform, mobile-friendly, Kaan'ın deneyimi burada |
| **Input** | Input System (yeni) | Unity 6 default, modern. Active Input Handling = Both |
| **DI / Service** | Static service locator (`GameServices`) | Singleton yasak, full DI framework (Zenject/VContainer) overkill — gerekirse VContainer'a geç |
| **Asset loading** | Addressables (Resources yasak) | Hot reload, memory bütçesi, mobile gereksinim |
| **Test framework** | Unity Test Framework (NUnit) | Yerleşik. Mock için NSubstitute hazır olduğunda |
| **Async** | `Awaitable` (Unity 6 native) → fallback `UniTask` | Coroutine sadece `IEnumerator` API gerektirenlerde |
| **UI** | uGUI (Canvas) default, UI Toolkit Editor tool'larında | Unity 6 LTS'de uGUI hâlâ runtime için olgun |
| **Hedef platform** | PC öncelik, mobile/WebGL ikincil | Kaan'ın MFD pattern'i; spesifik proje için override edilir |
| **Multiplayer** | Yok (varsayılan single-player) | Scope discipline; istenirse açıkça söylenir |
| **Version control büyük asset** | Git LFS (`.gitattributes` ile) | FBX/PNG/PSD/wav repo şişirir |

---

## Multi-Agent Kullanımı

**Spawn EDİLİR:**
- Bulk asset / SO instance üretimi (paralel)
- Test yazma paralel (EditMode/PlayMode farklı subagent'larda)
- Her milestone sonu architect/review subagent
- Kod review (subagent ana context'i görmez → bias-free review)
- Performance profiling, level/content design subagent
- Faz sonu retrospective (paralel)
- N-ajan dış göz review (büyük tasarım planlarında) — çıktı plana DAHİL DEĞİL, referans, Kaan kendi seçer

**Spawn EDİLMEZ:**
- Ana gameplay kodu (kritik path — main agent yazmalı)
- Kullanıcıyla iterasyon gerektiren işler
- < 30 dk atomik task'lar
- Sıralı bağımlılık zinciri (paralel kazanımı yok)

**Kurallar:** Subagent main context'i görmez — prompt'a path + kural referansı + rapor formatı yaz. Subagent commit yetkisi yok; main agent review + commit. Tek mesajda 2-4 paralel.

---

## Tekrarlayan Tool / İş Pattern'leri

### Git akışı
- Conventional Commits (`feat`/`fix`/`docs`/`refactor`/`chore`/`test`/`perf`/`style`/`ci`).
- Her özellik bitti → anında commit. Biriktirme yasak.
- Amend YOK — tweak bile yeni commit.
- Claude branch: `claude-dev`. `main` merge sadece Kaan açıkça söyleyince + `--no-ff`.
- Push noktası Kaan'ın "push" demesi.
- History rewrite öncesi `cp -r .git .git.bak` + branch backup (`git branch backup-$(date +%s)`).

### [Unity] Unity MCP (CoplayDev)
- **Bağlantı sıralaması:** Unity Editor aç → `Window > MCP For Unity > Start Server` → sonra Claude Code oturumu.
- Bağlantı yoksa **dur ve Kaan'a sor** ("Unity MCP bağlı değil — açar mısın? Yoksa Editor scripti / manuel ile devam edeyim?"). Sessizce alternatif yola geçme.
- Asset rename → `manage_asset move` (.meta GUID korunur).
- Script create/edit sonrası `read_console` + `refresh_unity` (scripts + compile).
- SO edit → `manage_scriptable_object modify` ile patches array (raw YAML edit yerine).
- Game View screenshot Canvas Screen Space Overlay'i yakalamaz — geçici Screen Space Camera moduna al, sonra revert.
- Domain reload sonrası 15-30 s bekle (`run_in_background:true` + sleep).
- Sahne hierarchy düzenleme: `manage_gameobject parent` string-based name lookup, çoklu eşleşmede yanlış hedef seçer → `execute_code` ile `Undo.SetTransformParent(...)` daha güvenli.

### [Unity] ScriptableObject lifetime
- SO state Play mode'da değişip Editor'da kalır → "neden değer farklı" gizli bug.
- Runtime'da yazılan SO field'ları Play mode sonu **Inspector'a yansır**, dirty asset olur. Test sandbox'ta SO yaratıp ana SO'yu kirletme.

### Notion
- `notion-create-pages` parent root-level parametre, batch 100 page.
- Master + child page yapısı. Markdown standard, `- [ ]` otomatik checkbox.
- `notion-update-page content_updates` find/replace pattern (full rewrite gereksiz).

### Iterative Visual Debug (Unity Play mode)
Play 10-15s gözle, screenshot al → bug'ları numaralı listele → fix → Stop → `refresh_unity` → re-play. "Sıkıntılı bir şey görmeyene kadar tekrar et."

### Bensiz çalışma protokolü
Kaan "uyuyacağım, bensiz devam et, full yetki" dediğinde:
1. **Memory'lere körelme** — "yarım kalan iş" notu son commit ile kapanmış olabilir, gerçek state'i `git status` + `git log --oneline -10` + sahne hierarchy ile doğrula.
2. **Risk önceliği:** kod-only > SO scaffold > sahne edit > destructive ops. Destructive olanı yapma; "bensiz devam"da olsan bile.
3. **Her mantıksal birim ayrı commit** — Kaan uyandığında 8 küçük commit görüyor, diff anlaşılır.
4. **Bulk SO üretimi:** `manage_scriptable_object create` + `batch_execute` — bir batch'te N SO üretilir.
5. **Kural çakışırsa:** önce `rules.md`'ye doğru kuralı yaz, sonra `project-memory.md`'deki yanlış TODO'yu sil/düzelt. İki dosya ayrı tutulduğunda kural+state birbirini ezmez.

---

## Asset Pipeline Tarifleri → `recipes/asset-pipelines.md`

Mesh / texture / animation / UI üretim akışları (Meshy, Mixamo, Stitch, PDF doc) ayrı dosyada — her proje aynı asset'i üretmez, fluff yapmasın.

---

_Bu template'i yeni proje için kopyaladıktan sonra: Kaan profili + Default Tercihler + git pattern'leri her zaman geçerli. Unity-spesifik pattern'ler ([Unity] etiketli) Unity dışı projede de referans olarak kalsın — Kaan ağırlıklı Unity'de çalışıyor, ileride yine işine yarar._
