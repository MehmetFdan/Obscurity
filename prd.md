# 🎮 PRD — Unity 3D FPS Horror Game
> **Versiyon:** 1.0 | **Motor:** Unity 6 LTS | **Pipeline:** HDRP | **Dil:** C#

---

## 📋 İçindekiler

1. [Proje Özeti](#1-proje-özeti)
2. [Teknik Temel](#2-teknik-temel)
3. [Mekanikler ve Geliştirme Sırası](#3-mekanikler-ve-geliştirme-sırası)
4. [Milestone Planı](#4-milestone-planı)
5. [Bağımlılık Haritası](#5-bağımlılık-haritası)
6. [Kabul Kriterleri](#6-kabul-kriterleri)

---

## 1. Proje Özeti

| Alan | Detay |
|------|-------|
| **Tür** | 3D First-Person Survival Horror |
| **Platform** | PC (Windows/Mac), Console (PS5, Xbox) |
| **Unity** | Unity 6 LTS |
| **Render** | HDRP (PC/Console) |
| **Hedef FPS** | 60 FPS @ 1080p · 30 FPS @ 4K |
| **Mimari** | Component-based + Service Locator + Event-driven |

---

## 2. Teknik Temel

Herhangi bir mekaniğe başlamadan önce aşağıdaki altyapı kurulmalıdır.  
Bu adımlar **Milestone 0** kapsamındadır ve tüm mekaniklerin önkoşuludur.

```
[ ] Unity 6 LTS projesi oluştur — HDRP template
[ ] Assembly Definition Files (.asmdef) yapılandır
[ ] ServiceLocator implementasyonu
[ ] EventBus<T> implementasyonu
[ ] SceneLoader (Addressables tabanlı async)
[ ] Object Pool Manager (UnityEngine.Pool)
[ ] Git + Git LFS yapılandırması (.gitattributes)
[ ] CI/CD pipeline (GameCI + GitHub Actions)
[ ] Klasör yapısı oluştur (Scripts/Core, Player, Enemy, Audio, UI, Save...)
```

---

## 3. Mekanikler ve Geliştirme Sırası

Her mekanik kendi önkoşullarına bağlıdır.  
Aşağıdaki sıra bu bağımlılıklar gözetilerek belirlenmiştir.

---

### 🥇 Aşama 1 — Temel Hareket (M1)

> **Önkoşul:** Teknik temel (M0) tamamlanmış olmalı.

---

#### M1-01 · FPS Player Controller
**Öncelik:** `KRİTİK` | **Bağımlı Olan:** Tüm diğer sistemler

Bu, projenin çekirdeğidir. Diğer hiçbir sistem bu olmadan test edilemez.

**Özellikler:**
- `CharacterController` tabanlı hareket (Rigidbody değil)
- WASD / Gamepad sol analog hareket
- Mouse / Gamepad sağ analog kamera (yatay + dikey clamp)
- Yerçekimi ve zemine tutunma (slope handling)
- Cinemachine `Virtual Camera` + `CinemachineBrain` entegrasyonu

**Teknik Notlar:**
- Tüm referanslar `Awake()` içinde cache'lenmeli — `Update()`'te `GetComponent` yasak
- Kamera input `InputSystem` (new) Action Map ile alınmalı
- `PlayerController` sınıfı `IService` implemente etmeli → ServiceLocator'a kayıt

**Kabul Kriterleri:**
```
[ ] Oyuncu 4 yönde sorunsuz hareket eder
[ ] Kamera mouse ile 360° döner, dikey ±89° ile sınırlıdır
[ ] Eğimli zeminlerde kayma / takılma yoktur
[ ] Gamepad desteği çalışır
```

---

#### M1-02 · Koşma ve Stamina Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M1-01

**Özellikler:**
- `Shift` basılıyken hız çarpanı (örn. x1.8)
- Stamina bar (0–100) — koşunca tükenir, durunca yenilenir
- Stamina bitince koşma devre dışı — kısa nefes sesi tetiklenir
- Sanity sistemine hook (düşük sanity = stamina yenilenme hızı azalır)

**Kabul Kriterleri:**
```
[ ] Stamina UI bar'ı görünür ve doğru güncellenir
[ ] Stamina 0'a ulaşınca koşma durur
[ ] Stamina belirli eşiğin altındayken EventBus<StaminaLowEvent> tetiklenir
```

---

#### M1-03 · Çömelme (Crouch) ve Eğilme (Lean)
**Öncelik:** `ORTA` | **Önkoşul:** M1-01

**Özellikler:**
- `C` tuşu ile capsule collider yüksekliği %50 azaltılır
- Kamera yavaşça aşağı kayar (DOTween tween)
- Sol/Sağ `Q/E` ile kamera X ekseni rotasyonu (±15°) — collider değişmez
- Lean sırasında adım sesi frekansı düşer

**Kabul Kriterleri:**
```
[ ] Çömelme sırasında düşük tavandan geçilemez (collider doğru)
[ ] Lean hareketi kameraya yansır, vücut yerinde kalır
[ ] Çömelme + Lean kombine çalışır
```

---

#### M1-04 · Procedural Kamera Efektleri
**Öncelik:** `ORTA` | **Önkoşul:** M1-01, M1-02

**Özellikler:**
- Yürürken baş sallama (head bob) — `ProceduralAnimator` bileşeni
- Koşunca bob frekansı artar
- Yorgunluk nefes efekti — Cinemachine `Noise Profile`
- El feneri sallantısı — bob ile senkronize

**Kabul Kriterleri:**
```
[ ] Bob hareketi yürüme hızına orantılı
[ ] Durduğunda bob sıfırlanır
[ ] Cinemachine noise profili stamina'ya göre dinamik değişir
```

---

### 🥈 Aşama 2 — Etkileşim ve Dünya (M2)

> **Önkoşul:** M1 aşaması tamamlanmış olmalı.

---

#### M2-01 · Interactable Sistemi
**Öncelik:** `KRİTİK` | **Önkoşul:** M1-01

Kapılar, çekmeceler, notlar, anahtarlar, ışık şalterleri — tüm etkileşimler bu sisteme bağlıdır.

```csharp
public interface IInteractable {
    string InteractPrompt { get; }
    void Interact(PlayerController player);
    bool CanInteract(PlayerController player);
}
```

**Özellikler:**
- Kamera merkezinden Raycast (mesafe: 2.5m)
- Hit olan obje `IInteractable` implemente ediyorsa UI prompt göster
- `E` tuşu ile `Interact()` çağrılır
- Prompt metni TextMeshPro ile HUD'da gösterilir

**Kabul Kriterleri:**
```
[ ] Menzil dışındaki objeler highlight almaz
[ ] Farklı obje tipleri farklı prompt metni gösterir
[ ] Etkileşim EventBus<InteractionEvent> ile yayınlanır
```

---

#### M2-02 · Kapı Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M2-01

**Özellikler:**
- Menteşeli kapı — `HingeJoint` veya `DOTween` animasyon
- Kilitli kapı durumu — anahtar item gerektirir
- Kilitli kapıyı zorlamaya çalışınca sarsılma animasyonu + ses
- Kapı açık/kapalı durumu Save sistemine yazılır

**Kabul Kriterleri:**
```
[ ] Kapı doğru yönde açılır (oyuncunun tarafına göre)
[ ] Kilitli kapı uygun ses + animasyon verir
[ ] Kapı durumu save/load döngüsünden geçer
```

---

#### M2-03 · Envanter Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M2-01

**Özellikler:**
- Maksimum slot kapasitesi (örn. 6 slot)
- `ScriptableObject` tabanlı `ItemData` (id, isim, ikon, açıklama, tip)
- `Tab` ile envanter UI açılır/kapanır
- Eşyaları birleştirme (combine) — örn. pil + el feneri
- Eşya kullanma, bırakma

**Kabul Kriterleri:**
```
[ ] Dolu envantere eşya alınamaz — uyarı verilir
[ ] ItemData ScriptableObject'te değiştirilince oyunda yansır
[ ] Combine logic doğru çalışır
```

---

#### M2-04 · Not / Doküman Okuma Sistemi
**Öncelik:** `ORTA` | **Önkoşul:** M2-01

**Özellikler:**
- Yerden alınan not ekranı kapatır, tam ekran gösterir
- TextMeshPro ile stillendirilmiş metin
- Çoklu sayfa desteği (ok tuşları)
- Not okunduğunda EventBus ile sanity sistemi tetiklenebilir

---

#### M2-05 · El Feneri Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M1-01, M2-03

**Özellikler:**
- `F` ile toggle açma/kapama
- Pil kapasitesi (0–100) — sürekli tükenir
- Pil azaldıkça ışık şiddeti ve rengi değişir (sarımsı, titremeye başlar)
- Pil yenileme — envanterdeki "Pil" eşyası kullanılır
- Düşman el fenerinden etkilenir (bkz. M4-01 AI sistemi)

**Kabul Kriterleri:**
```
[ ] Pil UI'ı doğru güncellenir
[ ] Pil 0'da ışık söner, titremeye başlar
[ ] Yeni pil takılınca tam kapasiteye döner
```

---

### 🥉 Aşama 3 — Ses ve Atmosfer (M3)

> **Önkoşul:** M1 ve M2-01 tamamlanmış olmalı.

---

#### M3-01 · Ses Altyapısı (FMOD)
**Öncelik:** `KRİTİK` | **Önkoşul:** M0

Tüm ses sisteminin temeli. Bu olmadan diğer ses mekanikleri geliştirilemez.

**Özellikler:**
- FMOD Studio Unity paketi entegrasyonu
- `AudioManager` → ServiceLocator'a kayıt
- Event yolu tabanlı ses tetikleme: `FMOD.Studio.EventInstance`
- Master / Music / SFX / Ambient mixer bus yapısı
- Snapshot sistemi: `Calm`, `Tense`, `Chase`, `Hidden`

**Kabul Kriterleri:**
```
[ ] FMOD build output projeye bağlı
[ ] AudioManager.PlayOneShot() ve PlayLoop() çalışır
[ ] Snapshot geçişleri smooth (fade in/out)
```

---

#### M3-02 · Adım Sesi Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M3-01, M1-01

**Özellikler:**
- `Physics.Raycast` ile zemin tipi tespiti (Layer / Tag bazlı)
- Zemin tiplerine göre FMOD event: taş, ahşap, toprak, su, metal, halı
- Yürüme / koşma / çömelme farklı ses ve frekans
- Adım sesi `SoundRadius` ile AI duyma sistemine beslenir

**Kabul Kriterleri:**
```
[ ] 6 zemin tipi farklı ses çıkarır
[ ] Çömelme adım sesi belirgin şekilde daha sessizdir
[ ] SoundRadius EventBus<FootstepEvent> ile AI'a iletilir
```

---

#### M3-03 · Ortam Ses Sistemi (Ambiance)
**Öncelik:** `ORTA` | **Önkoşul:** M3-01

**Özellikler:**
- `AmbianceZone` trigger collider — bölge bazlı ortam sesi
- Bölgeler arası geçişte crossfade (FMOD parameter ile)
- Rastgele aralıklı ses olayları (Poisson disk dağılımı)
- Sanity seviyesine göre ambiance yoğunluğu artar

---

#### M3-04 · Adaptif Müzik Sistemi
**Öncelik:** `ORTA` | **Önkoşul:** M3-01, M4 başlangıcı

**Özellikler:**
- FMOD multi-layer music event — katmanlar sanity / tehlike'ye göre aktif
- `MusicStateManager` → EventBus ile tehlike seviyesi dinler
- Geçiş kuralları: `Calm → Tense → Chase` ve geri
- Jumpscare stinger sesleri ayrı track

---

### ⚙️ Aşama 4 — Düşman AI (M4)

> **Önkoşul:** M1, M2-01, M3-02 tamamlanmış olmalı.

---

#### M4-01 · Düşman Temel AI (State Machine)
**Öncelik:** `KRİTİK` | **Önkoşul:** M1-01, M3-02

```
Patrol → Investigate → Chase → Attack → Search → Patrol
```

**Özellikler:**
- `NavMeshAgent` ile yol bulma (Unity AI Navigation paketi)
- `EnemyStateMachine` + abstract `EnemyState` sınıfı
- Waypoint tabanlı devriye — her oturumda rastgele sıra
- State geçişleri EventBus ile yayınlanır

**Kabul Kriterleri:**
```
[ ] Düşman tüm state'leri geçer, sonsuz döngü yok
[ ] NavMesh bake edilmiş ve agent engellere çarpmaz
[ ] State geçişleri loglara yazılır (debug modu)
```

---

#### M4-02 · Algı Sistemi (Sense System)
**Öncelik:** `KRİTİK` | **Önkoşul:** M4-01, M3-02, M2-05

**Özellikler:**

| Sensör | Mantık |
|--------|--------|
| **SightSensor** | Raycast + FOV açısı (örn. 90°) + ışık seviyesi kontrolü |
| **HearingSensor** | `FootstepEvent` ve `SoundRadius` değerini dinler |
| **SmellSensor** | Oyuncunun bıraktığı zamana dayalı iz noktaları |

- El feneri düşmanın dikkatini **çekebilir** (ışık kaynağı tespiti)
- Karanlıkta düşman görüş mesafesi azalır

**Kabul Kriterleri:**
```
[ ] Düşman görüş açısı dışından geçilebilir
[ ] Çömelince SoundRadius %60 azalır, düşman duymaz
[ ] El feneri kapatılınca görünürlük düşer
```

---

#### M4-03 · Düşman Animasyonu
**Öncelik:** `ORTA` | **Önkoşul:** M4-01

**Özellikler:**
- `Animator` + `Root Motion` — NavMesh ile senkronizasyon
- `Animator Override Controller` — farklı düşman tipleri için
- Blend tree: yavaş yürüme / hızlı koşma / sürünme
- IK (Inverse Kinematics) — zemin adaptasyonu (ayak IK)

---

### 💀 Aşama 5 — Oyuncu Psikolojisi (M5)

> **Önkoşul:** M1, M2, M3-01, M4-02 tamamlanmış olmalı.

---

#### M5-01 · Sanity Sistemi
**Öncelik:** `KRİTİK` | **Önkoşul:** M1-01, M3-01

Oyunun duygusal çekirdeği. Post-process, ses ve görsel efektlerin tamamı buna bağlanır.

**Sanity Azalma Nedenleri:**

| Olay | Azalma |
|------|--------|
| Düşmanla göz teması | -15/sn |
| Karanlıkta uzun süre kalma | -3/sn |
| Korkunç not/görüntü okuma | -10 (anlık) |
| Oyuncu ölümü (yeniden doğuşta) | -20 |

**Sanity Artma Nedenleri:**

| Olay | Artış |
|------|-------|
| Işıklı bölgede durma | +2/sn |
| Güvenli odada bekleme | +5/sn |
| İlaç / item kullanma | +20 (anlık) |

**Efekt Seviyeleri:**

| Seviye | Efektler |
|--------|----------|
| 100–75 | Temiz görüntü |
| 75–50 | Vignette, hafif noise |
| 50–25 | Chromatic aberration, film grain, yanlış sesler |
| 25–0 | Halüsinasyonlar, lens distortion, kalp sesi, düşman sesi yaklaşımı |

**Kabul Kriterleri:**
```
[ ] EventBus<SanityChangedEvent> her değişimde tetiklenir
[ ] Post-process Volume parametreleri smooth geçiş yapar
[ ] Sanity 0'a ulaşınca oyun biter (Game Over)
```

---

#### M5-02 · Halüsinasyon Sistemi
**Öncelik:** `ORTA` | **Önkoşul:** M5-01

**Özellikler:**
- Sanity < 25 olunca hayali düşman/nesne spawn edilir
- Hayali objeler `IHallucination` interface ile işaretlenir — hasar vermez
- Görsel-only: hayalet, silüet, yanlış konumda kapı vs.
- Object Pool ile yönetilir

---

#### M5-03 · Sağlık Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M4-01

**Özellikler:**
- HP: 0–100, anlık iyileşme yok — ilaç gerektirir
- Düşman saldırısı → hasar → HUD kırmızı flash + kan vignette
- HP 0 → Game Over → EventBus<PlayerDiedEvent>
- Hasar sesi + kamera sarsıntısı (Cinemachine Impulse)

---

### 🧩 Aşama 6 — Bulmaca ve İlerleme (M6)

> **Önkoşul:** M2 (Envanter + Kapı), M5-01 tamamlanmış olmalı.

---

#### M6-01 · Bulmaca Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M2-01, M2-03

**Özellikler:**
- `IPuzzle` interface → `Solve()` ve `IsSolved` property
- Kod kilidi, sembol bulmacası, çevresel bulmaca tipleri
- Çözüm durumu Save sistemine yazılır
- Bulmaca çözününce EventBus<PuzzleSolvedEvent>

---

#### M6-02 · Gizlenme (Hide) Sistemi
**Öncelik:** `YÜKSEK` | **Önkoşul:** M2-01, M4-02

**Özellikler:**
- `HideSpot` → `IInteractable` implemente eder
- Gizlenince SoundRadius = 0, görüş alanı kısıtlanır
- Gizlenme sırasında kalp sesi artar (gerilim)
- Düşman belirli mesafe içinde kapıyı açar (zor mod)

---

#### M6-03 · Checkpoint / Save Sistemi
**Öncelik:** `KRİTİK` | **Önkoşul:** M2-02, M2-03, M5-01, M5-03

**Özellikler:**
- Otomatik checkpoint — güvenli odaya girilince
- Manuel kayıt — save station ile (kayıt defteri)
- AES şifreli JSON — `Application.persistentDataPath`
- `GameSaveData` serialize: sahne, pozisyon, sanity, HP, envanter, kapı durumları

**Kabul Kriterleri:**
```
[ ] Oyun kapatılıp açıldığında aynı konumda devam edilir
[ ] Bozuk save dosyası gracefully handle edilir
[ ] Save slot sistemi: 3 slot desteklenir
```

---

### 🖥️ Aşama 7 — UI ve UX (M7)

> **Önkoşul:** M5-01, M2-03 tamamlanmış olmalı.

---

#### M7-01 · HUD
**Öncelik:** `YÜKSEK` | **Önkoşul:** M5-03, M1-02, M2-05

**Bileşenler:**

| Element | Veri Kaynağı |
|---------|-------------|
| Sağlık bar | `HealthSystem` |
| Stamina bar | `StaminaSystem` |
| Pil göstergesi | `FlashlightSystem` |
| Etkileşim prompt | `InteractionSystem` |
| Sanity indikatörü | `SanitySystem` |

---

#### M7-02 · Ana Menü ve Pause Menüsü
**Öncelik:** `ORTA` | **Önkoşul:** M7-01

**Özellikler:**
- Ana Menü: Yeni Oyun, Devam Et, Ayarlar, Çıkış
- Pause Menü: Devam Et, Kaydet, Ayarlar, Ana Menü
- Ayarlar: Grafik (kalite preset), Ses (master/müzik/sfx), Input rebinding

---

#### M7-03 · Erişilebilirlik
**Öncelik:** `ORTA` | **Önkoşul:** M7-02

**Özellikler:**
- Alt yazı (boyut ayarlanabilir)
- Colorblind modu (renk körü filtreleri)
- Motion sickness azaltma (FOV slider, head bob kapatma)
- Kontroller ekrana yazdırma (gamepad/KB)

---

### 🚀 Aşama 8 — Optimizasyon ve Polish (M8)

> **Önkoşul:** Tüm mekanikler tamamlanmış olmalı.

---

#### M8-01 · Performans Optimizasyonu
**Öncelik:** `KRİTİK`

```
[ ] Draw call < 300 (mid-range GPU hedef)
[ ] Occlusion culling bake edildi
[ ] GPU Instancing aktif (tekrarlayan meshler)
[ ] LOD Group tüm düşman ve büyük objelerde
[ ] Texture atlasing yapıldı
[ ] Addressables ile asset streaming — yükleme < 3sn
[ ] Memory Profiler ile sızıntı kontrolü yapıldı
```

---

#### M8-02 · Audio Polish
**Öncelik:** `YÜKSEK`

```
[ ] Tüm ses eventleri FMOD Studio'da mixlendi
[ ] 3D spatialization (HRTF) aktif
[ ] Reverb Zone bake edildi
[ ] Adaptive müzik tüm geçişlerde test edildi
```

---

#### M8-03 · Localization
**Öncelik:** `ORTA`

```
[ ] Unity Localization paketi entegrasyonu
[ ] TR / EN / DE / FR / ES dil desteği
[ ] Tüm UI stringleri Locale tablosuna alındı
[ ] Ses ve alt yazı sync test edildi
```

---

## 4. Milestone Planı

```
M0  ██████░░░░░░░░░░░░░░  Altyapı & Proje Kurulum
M1  ████████████░░░░░░░░  Temel Hareket
M2  ██████████████░░░░░░  Etkileşim & Dünya
M3  ████████████░░░░░░░░  Ses & Atmosfer
M4  ██████████████████░░  Düşman AI
M5  ██████████████░░░░░░  Oyuncu Psikolojisi
M6  ████████████░░░░░░░░  Bulmaca & İlerleme
M7  ████████░░░░░░░░░░░░  UI & UX
M8  ██████████░░░░░░░░░░  Optimizasyon & Polish
```

| Milestone | Süre (tahmini) | Çıktı |
|-----------|---------------|-------|
| **M0** | 1 hafta | Proje iskeleti, CI/CD, temel servisler |
| **M1** | 2 hafta | Oynanabilir karakter hareketi |
| **M2** | 3 hafta | Etkileşimli dünya, envanter |
| **M3** | 2 hafta | Ses altyapısı ve atmosfer |
| **M4** | 3 hafta | Düşman AI + algı sistemi |
| **M5** | 2 hafta | Sanity, sağlık, psikolojik efektler |
| **M6** | 2 hafta | Bulmacalar, gizlenme, save |
| **M7** | 1.5 hafta | HUD, menüler, erişilebilirlik |
| **M8** | 2 hafta | Optimizasyon, polish, localization |
| **Toplam** | **~18.5 hafta** | Production-ready build |

---

## 5. Bağımlılık Haritası

```
M0 (Altyapı)
│
├──► M1-01 FPS Controller ──────────────────────────────────────┐
│    ├──► M1-02 Koşma/Stamina                                   │
│    ├──► M1-03 Çömelme/Lean                                    │
│    └──► M1-04 Procedural Kamera                               │
│                                                                │
├──► M2-01 Interactable (M1-01 gerekli) ──────────────────────┐ │
│    ├──► M2-02 Kapı Sistemi                                   │ │
│    ├──► M2-03 Envanter                                       │ │
│    ├──► M2-04 Not Okuma                                      │ │
│    └──► M2-05 El Feneri (M2-03 gerekli)                      │ │
│                                                               │ │
├──► M3-01 FMOD Altyapı ───────────────────────────────────┐   │ │
│    ├──► M3-02 Adım Sesi (M1-01 gerekli)                  │   │ │
│    ├──► M3-03 Ambiance Sistemi                           │   │ │
│    └──► M3-04 Adaptif Müzik (M4 başlangıcı gerekli)     │   │ │
│                                                           │   │ │
├──► M4-01 Düşman AI (M1+M3-02 gerekli) ──────────────────┘   │ │
│    ├──► M4-02 Algı Sistemi (M2-05 gerekli)                   │ │
│    └──► M4-03 Düşman Animasyonu                              │ │
│                                                               │ │
├──► M5-01 Sanity (M1+M3-01+M4-02 gerekli) ◄──────────────────┘ │
│    ├──► M5-02 Halüsinasyon                                      │
│    └──► M5-03 Sağlık (M4-01 gerekli)                           │
│                                                                  │
├──► M6-01 Bulmaca (M2 gerekli)                                   │
│    ├──► M6-02 Gizlenme (M4-02 gerekli)                          │
│    └──► M6-03 Save Sistemi (M2+M5 gerekli) ◄────────────────────┘
│
├──► M7-01 HUD (M5+M1-02+M2-05 gerekli)
│    ├──► M7-02 Menüler
│    └──► M7-03 Erişilebilirlik
│
└──► M8 Optimizasyon & Polish (Tümü gerekli)
```

---

## 6. Kabul Kriterleri

### Genel Kalite Standartları

| Kriter | Hedef |
|--------|-------|
| **FPS** | 60 @ 1080p · 30 @ 4K (PC) |
| **Draw Call** | < 300 (mid-range GPU) |
| **RAM** | < 4 GB |
| **Yükleme Süresi** | < 3 saniye (sahne geçişi) |
| **Crash Rate** | < %0.1 (prod build) |
| **Test Coverage** | Core servisler için > %80 unit test |

### Definition of Done — Her Mekanik İçin

```
[ ] Fonksiyonel: Tanımlanan özellikler çalışır
[ ] Entegre: EventBus / ServiceLocator doğru bağlı
[ ] Test edildi: Unit veya PlayMode testi yazıldı
[ ] Profiling: Belirgin performans sorunu yok
[ ] Dokümante: XML summary ve inline yorum mevcut
[ ] Review: Kod incelemesi yapıldı (en az 1 reviewer)
```

---

*PRD v1.0 — Unity 3D FPS Horror Game*  
*Bu doküman Architecture Guide v1.0 baz alınarak hazırlanmıştır.*
