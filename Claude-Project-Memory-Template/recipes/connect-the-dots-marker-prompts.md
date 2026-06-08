# Connect-the-Dots Marker — Image-Gen Prompts

DrawRush'taki şeffaf küre anchor'ların yerine "connect-the-dots" (noktaları
birleştir) marker fikrini görselleştirmek için. AI görsel aracına (Midjourney /
DALL·E / Stable Diffusion / Leonardo) yapıştır. Promptlar İngilizce — görsel
modeller İngilizce'de çok daha iyi.

Oyun bağlamı (promptlara işlendi): top-down hafif eğik kamera (~50°), kalem
tutan stickman karakter, renkli playground/beach zemini, köşeleri birleştirerek
kare/üçgen/altıgen çiziyor, kenar çizilince renkli duvar yükseliyor.

---

## PROMPT A — Oyun içi konsept (fikri anlamak için: "nasıl görünür?")

Use this to SEE the idea in context.

```
Hyper-casual mobile game screenshot, top-down camera angled about 50 degrees,
a cute minimalist stickman character holding an oversized pencil, standing on a
bright colorful sandy playground floor. The character is drawing a square by
connecting glowing corner markers — each corner is a clean hollow white-and-cyan
ring with a small number inside (1, 2, 3, 4) like a children's connect-the-dots
puzzle, with thin dashed guide lines hinting the path between dots. Along the
edges that are already connected, solid vibrant colorful walls rise up. Playful,
clean low-poly 3D render, soft ambient shadows, saturated cheerful colors, no
text UI, mobile game art style, polished and juicy.
--ar 9:16 --style raw --v 6
```

Negative (SD/Leonardo): `realistic, photo, dark, cluttered UI, text watermark, gore`

---

## PROMPT B — Tek marker asset (oyunda kullanılacak gerçek görsel)

Isolated, transparent background, top-down — drop straight into Unity.

```
A single connect-the-dots corner marker for a cute mobile drawing game,
isolated on a transparent background, top-down view, centered. A clean hollow
ring (donut shape) with a soft glow and a small bold number "1" in the middle,
painted-marker / sticker style, bright cyan and white with a thin darker
outline, subtle soft drop shadow beneath it, playful cartoon vibe, crisp
high-resolution game UI asset, flat-ish 3D, no background, no extra text.
--ar 1:1 --style raw --v 6
```

İpucu: aynı promptu "number 2", "number 3"... diye çoğalt, ya da numarasız
("no number, just a glowing dotted ring") sade versiyon iste.

---

## PROMPT C — Marker varyasyon sayfası (seçenekleri kıyaslamak için)

```
A neat reference sheet of 6 corner-marker designs for a casual drawing game,
white background, flat top-down icons in a grid. Each is a "connect the dots"
style point: 1) hollow ring with number, 2) glowing dotted circle, 3) paint
blob / drop, 4) small pencil-tip mark, 5) pulsing target reticle, 6) star dot.
Bright playful colors, cyan/orange/white palette, clean cartoon vector look,
consistent style, labeled 1 to 6.
--ar 3:2 --v 6
```

---

## Notlar
- Oyun paleti: çizgiler/duvarlar canlı renkli; marker'lar cyan/beyaz kontrast iyi durur.
- "Sıradaki köşe" daha parlak/büyük yanıp sönsün fikri promptlara eklenebilir:
  `the next target dot is brighter and slightly larger, gently pulsing`.
- Asset gelince Unity'de: küre mesh'i yerine quad + bu texture (billboard) veya
  decal; collider/trigger aynı kalır, sadece görsel değişir.
