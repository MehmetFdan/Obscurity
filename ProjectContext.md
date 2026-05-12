# Unity 3D FPS Horror Game
## Mekanik → Pattern Eşleştirme Rehberi

> Bu doküman, FPS korku oyunundaki her mekanik için **hangi tasarım deseninin** kullanılması gerektiğini, **neden** kullanıldığını ve **nasıl birbirine bağlandığını** açıklar.

---

## İçindekiler

1. [Genel Mimari Haritası](#1-genel-mimari-haritası)
2. [Oyuncu Sistemleri](#2-oyuncu-sistemleri)
3. [Düşman AI Sistemi](#3-düşman-ai-sistemi)
4. [Sanity (Akıl Sağlığı) Sistemi](#4-sanity-akıl-sağlığı-sistemi)
5. [Ses Sistemi](#5-ses-sistemi)
6. [Etkileşim Sistemi](#6-etkileşim-sistemi)
7. [Kayıt / Yükleme Sistemi](#7-kayıt--yükleme-sistemi)
8. [Jumpscare Sistemi](#8-jumpscare-sistemi)
9. [Prosedürel Korku Sistemi — Strategy + EventBus + ScriptableObject Üçlüsü](#9-prosedürel-korku-sistemi--strategy--eventbus--scriptableobject-üçlüsü)
10. [Global Servisler ve Bağımlılık Yönetimi](#10-global-servisler-ve-bağımlılık-yönetimi)
11. [Object Pool — Sistemler Arası Kesişim Noktası](#11-object-pool--sistemler-arası-kesişim-noktası)
12. [Pattern Birlikte Çalışma Akışları](#12-pattern-birlikte-çalışma-akışları)
13. [Pattern Çakışma ve Entegrasyon Notları](#13-pattern-çakışma-ve-entegrasyon-notları)

---

## 1. Genel Mimari Haritası

Aşağıdaki tablo, hangi sistemin hangi pattern ile inşa edildiğini tek bakışta gösterir.

| Oyun Mekaniği | Birincil Pattern | Yardımcı Pattern(ler) | Katman |
|---|---|---|---|
| Global Servisler (Audio, Save, Scene) | **Service Locator** | — | Core Services |
| Sistemler Arası Haberleşme | **Event Bus (Observer)** | — | Core Services |
| Düşman Davranışı | **Hierarchical State Machine** | **Strategy** | Game Systems |
| Mermi / Kan Efekti / Ses / VFX / Halüsinasyon | **Object Pool** | **Event Bus** | Infrastructure |
| Oyuncu Yetenekleri (Hız, Görüş, Ses) | **Decorator** | **Strategy** | Game Systems |
| Oyuncu Eylemleri (Işık, Kapı, Eşya) | **Command** | — | Game Systems |
| Navigasyon / Saldırı Stratejisi | **Strategy** | **State Machine** | Game Systems |
| Sanity Efektleri | **Observer (Event Bus)** | **Decorator** | Game Systems |
| Etkileşilebilir Nesneler | **Interface (IInteractable)** | **Command** | Gameplay |
| Kayıt Sistemi | **Memento** (yapısal) | **Service Locator** | Data |
| AI Algı Kanalları | **Composite** (yapısal) | **State Machine** | Game Systems |
| Prosedürel Korku | **Strategy** | **EventBus + ScriptableObject** | Game Systems |

---

## 2. Oyuncu Sistemleri

### 2.1 FPS Hareket ve Kamera

**Kullanılan Pattern: Yok (Component-Based Unity Mimarisi)**

Hareket sistemi Unity'nin yerleşik `CharacterController` bileşeniyle çözülür. Pattern uygulanması gereken yer hareket mekaniğinin *efektleri* ve *modifierlardır*.

```
PlayerController
    ├── CharacterController   → hareket, çarpışma
    ├── Cinemachine Brain     → kamera render
    └── ProceduralAnimation   → bob, nefes efekti
```

**Neden CharacterController, Rigidbody değil?**
Fizik motoru dışında kalarak deterministik hareket sağlanır; düşman AI'nın oyuncu pozisyonunu hesaplaması tutarlı hale gelir.

---

### 2.2 Oyuncu Yetenekleri (Hız, Stamina, Ses Yarıçapı)

**Kullanılan Pattern: Decorator**

Oyuncuya runtime'da eklenen her yetenek (hız artışı, sessiz adım, görüş genişletme) mevcut stats nesnesini **sarmalar (wrap)**; base değerleri değiştirmeden üzerine etki ekler.

```
IPlayerStats
    └── BasePlayerStats          → MoveSpeed: 5f, StaminaRegen: 1f, SoundRadius: 3f
            └── SpeedBoostDecorator   → MoveSpeed: 5f × 1.4 = 7f
                    └── SilentStepDecorator → SoundRadius: 3f × 0 = 0f
```

**Ne zaman kullanılır?**
- Eşya toplandığında (sessiz ayakkabı, enerji içeceği)
- Gizlenme noktasına girildiğinde (`SoundRadius = 0`)
- Sanity seviyesi düştükçe yetenek debuff uygulandığında

**Alternatife göre avantajı:**
`if/else` zincirleri veya boolean flag'ler yerine her modifier kendi sınıfında tanımlı; yeni yetenek eklemek mevcut kodu değiştirmeyi gerektirmez (Open/Closed Principle).

---

### 2.3 Oyuncu Eylemleri (Işık, Kapı, Gizlenme)

**Kullanılan Pattern: Command**

Her oyuncu eylemi bir `ICommand` nesnesidir. Bu yapı üç kritik özelliği sağlar:

1. **Undo/Redo** — Işığı tekrar kapatabilmek için `Execute()` / `Undo()` ikilisi
2. **Input Rebinding** — Hangi tuşun hangi command'ı tetiklediği `InputActionMap` üzerinden ayrılmış
3. **Replay Sistemi** — Komutlar kuyrukta saklanarak oynanış tekrar edilebilir

```csharp
// Komut kuyruğu örneği
Queue<ICommand> _commandHistory = new();

void ExecuteCommand(ICommand cmd) {
    cmd.Execute();
    _commandHistory.Enqueue(cmd);
}
```

**Hangi eylemler Command olarak modellenmeli?**

| Eylem | Command Sınıfı | Undo Davranışı |
|---|---|---|
| El feneri aç/kapat | `ToggleFlashlightCommand` | Tekrar toggle |
| Kapıyı aç | `OpenDoorCommand` | Kapıyı kapat |
| Eşya kullan | `UseItemCommand` | Eşyayı envantere geri koy |
| Nota oku | `PickupNoteCommand` | Notu bırak |
| Işık şalteri | `ToggleLightCommand` | Tekrar toggle |

---

## 3. Düşman AI Sistemi

### 3.1 Davranış Döngüsü

**Kullanılan Pattern: Hierarchical State Machine (HSM)**

Düşmanın tüm davranış geçişleri bir durum makinesiyle yönetilir. "Hierarchical" olmasının nedeni bazı durumların alt durumlar içermesidir (örneğin `Investigate` içinde `ListenSubState` ve `SniffSubState`).

```
EnemyStateMachine
    ├── PatrolState
    │       ├── IdleAtWaypointSubState
    │       └── MoveToWaypointSubState
    ├── InvestigateState
    │       ├── MoveToSoundSubState
    │       └── ScanAreaSubState
    ├── ChaseState
    ├── AttackState
    ├── SearchState
    └── StunnedState
```

**Geçiş koşulları ve tetikleyiciler:**

```
PatrolState
    → InvestigateState  : HearingSensor tetiklendi (ses duyuldu)
    → ChaseState        : SightSensor tetiklendi (oyuncu görüldü)

InvestigateState
    → ChaseState        : Son konuma ulaşıldı ve oyuncu hâlâ görüş alanında
    → PatrolState       : Belirlenen süre içinde oyuncu bulunamadı

ChaseState
    → AttackState       : agent.remainingDistance < attackRange
    → SearchState       : Oyuncu görüş alanından çıktı

SearchState
    → ChaseState        : Oyuncu tekrar görüldü
    → PatrolState       : Arama süresi doldu
```

**Neden if/else değil State Machine?**
6 durum ve 12+ geçiş koşulu `Update()` içinde `if/else` bloklarıyla yönetilirse `spaghetti code` kaçınılmaz olur. Her durum kendi `Enter()`, `Tick()`, `Exit()` metodlarıyla izole edilir; yeni durum eklemek mevcut durumları bozmaz.

---

### 3.2 Navigasyon ve Saldırı Stratejileri

**Kullanılan Pattern: Strategy**

Farklı düşman tipleri farklı navigasyon ve saldırı davranışları sergiler. Strategy pattern bu davranışları runtime'da değiştirilebilir hale getirir.

```
INavigationStrategy
    ├── DirectChaseStrategy    → En kısa yoldan takip (yavaş düşman)
    ├── FlankingStrategy       → Oyuncunun önüne geçmeye çalışma (zeki düşman)
    └── AmbushStrategy         → Önceden bilinen konumda bekleme (kurucu düşman)

IAttackStrategy
    ├── MeleeAttackStrategy    → Yakın mesafe saldırı
    ├── JumpscareAttackStrategy→ Ani görünme + ses
    └── TeleportAttackStrategy → Halüsinasyon bazlı ışınlanma
```

**State Machine ile entegrasyon:**
`ChaseState.Tick()` içinde `_navigationStrategy.Navigate(agent, playerTransform)` çağrılır. Durum makinesi *ne yapılacağını* belirlerken, Strategy *nasıl yapılacağını* belirler.

```csharp
// ChaseState içinde:
public override void Tick() {
    Owner.NavigationStrategy.Navigate(Owner.Agent, Owner.PlayerTransform);
    // ...
}
```

---

### 3.3 Algı Sistemi (Görme, Duyma, Koku)

**Kullanılan Pattern: Composite (Yapısal) + Observer (Event Bus)**

Her algı kanalı bağımsız bir `ISensor` bileşenidir; `SensorComposite` hepsini tek noktadan değerlendirir. Herhangi bir sensör tetiklendiğinde `EventBus` üzerinden durum makinesine bildirim gönderilir.

```
SensorComposite
    ├── SightSensor     → Raycast + FOV açısı + ışık seviyesi
    ├── HearingSensor   → Oyuncu SoundRadius dinleme
    └── SmellSensor     → Zaman bazlı iz takibi (isteğe bağlı)
```

```csharp
// EventBus bağlantısı:
// HearingSensor → EnemyDetectedSoundEvent → InvestigateState'e geçiş
EventBus<EnemyDetectedSoundEvent>.Publish(new() { SoundPosition = pos });
```

---

## 4. Sanity (Akıl Sağlığı) Sistemi

**Kullanılan Pattern: Observer (Event Bus) + Decorator**

Sanity sistemi oyunun "sinir merkezi"dir; değer her değiştiğinde bağlı tüm sistemlere `EventBus` aracılığıyla bildirim gönderir. Hiçbir sistem birbirini doğrudan referans almaz.

```
SanitySystem (Publisher)
    └── EventBus<PlayerSanityChangedEvent>.Publish()
            ├── PostProcessingController  → Vignette, Chromatic Aberration
            ├── AudioManager             → Kalp atışı frekansı, düşman sesi
            ├── HallucinationSpawner     → Sahte düşman/nesne spawn
            ├── HUDController            → Sanity göstergesi güncelleme
            └── EnemyAI (HearingSensor)  → SoundRadius artışı (debuff)
```

**Sanity seviyelerine göre efekt zinciri:**

```
100 → 75   PostProcess: kapalı         Audio: normal
 75 → 50   PostProcess: hafif vignette Audio: ara sıra gürültü
 50 → 25   PostProcess: chromatic + grain  Audio: yanlış sesler
 25 →  0   PostProcess: lens distortion    Audio: kalp sesi + halüsinasyon
```

**Decorator bağlantısı:**
Düşük sanity'de `SpeedDebuffDecorator` oyuncu stats'ına eklenir; yüksek sanity'de kaldırılır. Sanity değişimi → EventBus → `PlayerStatsController` → Decorator ekleme/çıkarma.

---

## 5. Ses Sistemi

**Kullanılan Pattern: Service Locator + Observer (Event Bus)**

`AudioManager` bir servis olarak `ServiceLocator`'a kaydedilir; oyunun herhangi bir noktasından erişilebilir. Ses tetikleme kararları ise doğrudan çağrı yerine `EventBus` olayları üzerinden alınır.

```
ServiceLocator.Register<IAudioManager>(audioManager);

// Kullanım (herhangi bir script):
ServiceLocator.Get<IAudioManager>().PlaySFX("door_creak", transform.position);
```

**Adaptif müzik sistemi (Sanity + Tehlike):**

```
EventBus<PlayerSanityChangedEvent>
    → AudioManager
        → AudioMixer Snapshot geçişi
            ├── "Calm"    : sanity > 75  → minimal piano
            ├── "Tense"   : sanity 50-75 → string tremolo
            ├── "Danger"  : sanity 25-50 → heavy percussion
            └── "Panic"   : sanity < 25  → dissonant drone + kalp sesi

EventBus<EnemyDetectedPlayerEvent>
    → AudioManager
        → "Chase" Snapshot'ına ani geçiş
```

**Ses kategorileri ve pool bağlantısı:**
`AudioSource` bileşenleri sık oluşturulup yok edildiği için **Object Pool** ile yönetilir.

```
AudioSourcePool
    ├── Ambient   → Loop, düşük öncelik
    ├── SFX       → Kısa, yüksek öncelik (3D spatial)
    ├── Music     → Tek kaynak, crossfade
    └── Stinger   → Jumpscare anlık ses (en yüksek öncelik)
```

---

## 6. Etkileşim Sistemi

**Kullanılan Pattern: Interface Segregation (IInteractable) + Command**

Tüm etkileşilebilir nesneler `IInteractable` arayüzünü uygular. Oyuncu bir nesneyle etkileşime girdiğinde `ICommand` oluşturulur ve çalıştırılır; bu sayede undo/redo ve event log desteği sağlanır.

```
IInteractable
    ├── DoorController
    ├── DrawerController
    ├── NotePickup
    ├── LightSwitch
    ├── KeyItem
    ├── HideSpot
    └── PuzzleObject
```

**Raycast tabanlı etkileşim akışı:**

```
PlayerInteract.Update()
    → Physics.Raycast(kamera, interactRange)
        → hit.collider.GetComponent<IInteractable>()
            → CanInteract(player) == true
                → ICommand cmd = interactable.CreateCommand(player)
                → ExecuteCommand(cmd)     ← Command kuyruğuna da eklenir
```

**HideSpot özel durumu:**
Gizlenme noktası hem `IInteractable` hem de `Decorator` zinciriyle çalışır:

```
HideSpot.Interact()
    → player.AddDecorator(new SilentStepDecorator(player.Stats, 0f))
    → EventBus<PlayerHidingEvent>.Publish()
        → EnemyAI: SoundRadius dinlemeyi durdur
        → AudioManager: "Hiding" Snapshot'ına geç
```

---

## 7. Kayıt / Yükleme Sistemi

**Kullanılan Pattern: Memento (Yapısal) + Service Locator**

`GameSaveData` nesnesi Memento pattern'inin "snapshot"ını temsil eder: oyunun belirli bir andaki tüm durumunu içerir. `SaveSystem` bu snapshot'ı alıp şifreli JSON olarak yazar.

```
SaveSystem (Caretaker)
    ├── CreateSave()  → GameSaveData (Memento) oluştur → JSON şifrele → diske yaz
    └── LoadSave()    → diskten oku → JSON çöz → GameSaveData → sistemlere dağıt
```

**Kayıt verisi hangi sistemlerden toplanır?**

```
GameSaveData
    ├── SceneName          ← SceneManager
    ├── PlayerPosition     ← PlayerController.transform
    ├── Sanity             ← SanitySystem
    ├── Health             ← HealthSystem
    ├── CollectedItems     ← InventorySystem
    ├── DoorStates         ← DoorController[] (tüm kapılar)
    └── SaveTimestamp      ← System.DateTime
```

**ServiceLocator bağlantısı:**
`SaveSystem` bir servis olarak kayıtlıdır; checkpoint bölgeleri ve pause menüsü doğrudan erişir.

```csharp
// CheckpointZone.cs
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Player"))
        ServiceLocator.Get<ISaveSystem>().AutoSave();
}
```

---

## 8. Jumpscare Sistemi

**Kullanılan Pattern: Command + Observer (Event Bus)**

Her jumpscare, birden fazla sistemi (animasyon, ses, post-process, kamera sarsıntı) **aynı anda** tetiklemesi gereken bir eylemdir. Bu koordinasyon `EventBus` aracılığıyla sağlanır; hiçbir sistem diğerini doğrudan çağırmaz.

```
JumpscareManager
    → Cooldown kontrolü (art arda jumpscare engeli)
        → EventBus<JumpscareTriggeredEvent>.Publish()
                ├── AudioManager          → Stinger ses çal (max volume)
                ├── CinemachineImpulse    → Kamera sarsıntısı
                ├── PostProcessController → Anlık beyaz flash (Bloom overexpose)
                ├── EnemyAnimator         → Lunge animasyonu
                └── SanitySystem          → sanity.Decrease(15f)
```

**Tetikleyici türleri:**

| Tetikleyici | Mekanik |
|---|---|
| `TriggerZone` (Collider) | Kapı, köşe, dolap yakını |
| `EnemyAI.AttackState` | Düşmanın saldırı anı |
| `ProceduralHorror` | Prosedürel sistem rastgele tetikleme |

---

## 9. Prosedürel Korku Sistemi — Strategy + EventBus + ScriptableObject Üçlüsü

**Kullanılan Pattern: Strategy + Observer (Event Bus) + ScriptableObject (Veri Taşıyıcı)**

Oyunun tekrar edilebilirliğini artırmak için jumpscare zamanlaması, düşman waypoint'leri ve ambient ses tetikleyicileri yarı-rastgele üretilir. Bu sistemin ayırt edici özelliği üç pattern'in **birbirini tamamlayan roller** üstlenmesidir:

| Rol | Pattern | Sorumluluk |
|---|---|---|
| **Nasıl** üretilecek? | Strategy | Algoritma seçimi (Poisson, Gaussian, NavMesh) |
| **Ne** üretildi? Kime bildirilecek? | EventBus | Loose-coupled sistem bildirimleri |
| **Hangi parametrelerle** üretilecek? | ScriptableObject | Tasarımcı-düzenlenebilir konfigürasyon |

### 9.1 Strategy — Rastgelelik Algoritmaları

Her rastgelelik stratejisi `IRandomizationStrategy` arayüzüyle değiştirilebilir. Strateji **sadece** sonraki olayın zamanını veya konumunu hesaplar; sonucu kullanmaz.

```
IRandomizationStrategy
    ├── PoissonDiskStrategy       → Ambient ses olayları (düzgün dağılım)
    ├── GaussianTimingStrategy    → Jumpscare zamanlaması (ortalama etrafında)
    └── NavMeshSamplingStrategy   → Waypoint pozisyonları (NavMesh'te rastgele nokta)
```

```csharp
// Strategy sadece "ne zaman" veya "nerede" sorusunu cevaplar
public interface IRandomizationStrategy {
    float GetNextInterval(HorrorEventConfig config);
}

public class GaussianTimingStrategy : IRandomizationStrategy {
    public float GetNextInterval(HorrorEventConfig config) {
        // config.meanInterval ve config.stdDeviation SO'dan gelir
        return Mathf.Max(config.minInterval,
            GaussianRandom(config.meanInterval, config.stdDeviation));
    }
}
```

### 9.2 ScriptableObject — Tasarımcı Konfigürasyonu

Her prosedürel olay tipi kendi `ScriptableObject` config'ine sahiptir. Bu sayede tasarımcılar **kod açmadan** korku yoğunluğunu, zamanlama dağılımını ve tetikleme koşullarını ayarlayabilir.

```
HorrorEventConfig (ScriptableObject) [Temel — tüm olaylar miras alır]
    ├── float meanInterval        = 30f    // Ortalama tetikleme aralığı (sn)
    ├── float stdDeviation        = 8f     // Gaussian sapma
    ├── float minInterval         = 10f    // Minimum bekleme süresi
    ├── float maxSanityThreshold  = 70f    // Bu sanity'nin altında aktif
    └── int   priorityWeight      = 1      // Birden fazla olay çakışırsa öncelik

LightFlickerConfig : HorrorEventConfig
    ├── float flickerDuration    = 2f
    ├── float intensityMin       = 0.1f
    └── AnimationCurve flickerCurve

HallucinationConfig : HorrorEventConfig
    ├── GameObject[] hallucinationPrefabs   // Pool'dan çekilecek prefab listesi
    ├── float spawnRadius        = 12f
    ├── float lifetimeSec        = 5f
    └── bool  requiresLineOfSight = true

AmbientStingerConfig : HorrorEventConfig
    ├── AudioClip[] stingerClips          // Pool'dan AudioSource ile çalınacak
    ├── float volumeRange        = 0.3f
    └── float spatialBlend       = 0.8f
```

**Kritik avantaj:** Strategy sınıfı `HorrorEventConfig` referansını constructor'da alır. Aynı `GaussianTimingStrategy` sınıfı, farklı SO konfigürasyonlarıyla hem jumpscare hem ışık titremesi için kullanılabilir — **Strategy yeniden yazılmaz, SO değiştirilir.**

### 9.3 EventBus — Sistemler Arası Bildirim

Strategy bir zaman/konum ürettiğinde `ProceduralHorrorManager` ilgili event struct'ını `EventBus` üzerinden yayınlar. Dinleyen sistemler **birbirinden habersiz** çalışır.

```csharp
// Event struct tanımları (GC-friendly, class değil struct)
public struct LightFlickerEvent {
    public Vector3 Position;
    public float Duration;
    public AnimationCurve FlickerCurve;
}

public struct HallucinationSpawnEvent {
    public Vector3 SpawnPosition;
    public GameObject Prefab;      // Pool'dan alınacak
    public float Lifetime;
}

public struct AmbientStingerEvent {
    public AudioClip Clip;
    public Vector3 Position;
    public float Volume;
}
```

### 9.4 Üçlünün Birlikte Çalışma Döngüsü

Aşağıdaki akış, tek bir prosedürel olayın (örneğin halüsinasyon spawn) baştan sona nasıl işlediğini gösterir:

```
┌─────────────────────────────────────────────────────────────────────┐
│                  PROSEDÜREL KORKU — RUNTIME DÖNGÜSÜ                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. ScriptableObject (HallucinationConfig)                         │
│     ├── meanInterval = 25f, stdDeviation = 6f                      │
│     ├── spawnRadius = 12f, lifetimeSec = 5f                        │
│     └── hallucinationPrefabs = [GhostA, ShadowB, CorpseC]         │
│              │                                                      │
│              ▼                                                      │
│  2. Strategy (GaussianTimingStrategy)                               │
│     ├── GetNextInterval(config) → 22.3 saniye hesaplandı           │
│     └── Timer başlatıldı                                            │
│              │                                                      │
│              ▼  [22.3 sn sonra]                                     │
│  3. ProceduralHorrorManager                                         │
│     ├── Sanity kontrolü: currentSanity < config.maxSanityThreshold?│
│     │   ├── Evet → devam et                                        │
│     │   └── Hayır → yeni interval hesapla, bekle                   │
│     ├── NavMesh'te rastgele spawn noktası hesapla                  │
│     ├── Pool'dan prefab al (ObjectPool)                            │
│     └── EventBus<HallucinationSpawnEvent>.Publish(event)           │
│              │                                                      │
│              ▼                                                      │
│  4. EventBus Dinleyicileri (birbirinden habersiz)                   │
│     ├── HallucinationSpawner  → Prefab'ı pozisyona yerleştir       │
│     ├── AudioManager          → Fısıltı sesi çal (Pool'dan)       │
│     ├── SanitySystem          → sanity.Decrease(3f)                │
│     └── PostProcessController → Chromatic aberration flash         │
│              │                                                      │
│              ▼                                                      │
│  5. Yaşam süresi dolunca                                           │
│     ├── Halüsinasyon prefab → ObjectPool'a geri bırak              │
│     ├── AudioSource → AudioSourcePool'a geri bırak                 │
│     └── Yeni interval hesapla → Adım 2'ye dön                     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 9.5 ProceduralHorrorManager — Orkestrasyon Kodu

```csharp
public class ProceduralHorrorManager : MonoBehaviour {
    [SerializeField] private HorrorEventConfig[] eventConfigs;

    private Dictionary<HorrorEventConfig, IRandomizationStrategy> _strategies;
    private Dictionary<HorrorEventConfig, float> _timers;

    void Start() {
        _strategies = new();
        _timers = new();
        foreach (var config in eventConfigs) {
            var strategy = StrategyFactory.Create(config);
            _strategies[config] = strategy;
            _timers[config] = strategy.GetNextInterval(config);
        }
    }

    void Update() {
        var sanity = ServiceLocator.Get<ISanitySystem>().CurrentSanity;
        foreach (var config in eventConfigs) {
            _timers[config] -= Time.deltaTime;
            if (_timers[config] > 0f) continue;
            if (sanity > config.maxSanityThreshold) {
                _timers[config] = _strategies[config].GetNextInterval(config);
                continue;
            }
            DispatchEvent(config);
            _timers[config] = _strategies[config].GetNextInterval(config);
        }
    }

    private void DispatchEvent(HorrorEventConfig config) {
        switch (config) {
            case HallucinationConfig h:
                var prefab = h.hallucinationPrefabs[Random.Range(0, h.hallucinationPrefabs.Length)];
                var pos = NavMeshSampler.GetRandomPoint(transform.position, h.spawnRadius);
                EventBus<HallucinationSpawnEvent>.Publish(new() {
                    SpawnPosition = pos, Prefab = prefab, Lifetime = h.lifetimeSec
                });
                break;
            case LightFlickerConfig l:
                EventBus<LightFlickerEvent>.Publish(new() {
                    Position = transform.position, Duration = l.flickerDuration,
                    FlickerCurve = l.flickerCurve
                });
                break;
            case AmbientStingerConfig a:
                var clip = a.stingerClips[Random.Range(0, a.stingerClips.Length)];
                EventBus<AmbientStingerEvent>.Publish(new() {
                    Clip = clip, Position = transform.position, Volume = a.volumeRange
                });
                break;
        }
    }
}
```

**Prosedürel efektler ve bağlı sistemler (özet tablo):**

| Prosedürel Mekanik | Strateji | EventBus Bildirimi | SO Config |
|---|---|---|---|
| Işık titremesi | `GaussianTimingStrategy` | `LightFlickerEvent` → SanitySystem | `LightFlickerConfig` |
| Ambient korku sesi | `PoissonDiskStrategy` | `AmbientStingerEvent` → AudioManager | `AmbientStingerConfig` |
| Düşman waypoint | `NavMeshSamplingStrategy` | Doğrudan `PatrolState`'e | `EnemyConfig` |
| Gölge halüsinasyonu | `GaussianTimingStrategy` | `HallucinationSpawnEvent` → HallucinationSpawner | `HallucinationConfig` |

### 9.6 Üçlünün Ayrılma Prensibi

Bu üçlü birlikte çalışır ama **hiçbiri diğerini doğrudan bilmez:**

```
ScriptableObject  ←→  Strategy  ←→  EventBus
      ↕                   ↕              ↕
  "Ne kadar?"        "Ne zaman?"     "Kime?"
 (Tasarımcı)        (Algoritma)     (Sistem)
```

- **SO değişirse** → Strategy aynı kalır, sadece parametreler değişir
- **Strategy değişirse** → SO ve EventBus etkilenmez, sadece zamanlama algoritması değişir
- **Yeni dinleyici eklenirse** → EventBus'a subscribe olur; SO ve Strategy habersizdir

Bu ayrım sayesinde her bileşen **bağımsız test edilebilir:**

```csharp
// Strategy testi — EventBus ve MonoBehaviour bağımlılığı yok
[Test]
public void GaussianTiming_ReturnsWithinBounds() {
    var config = ScriptableObject.CreateInstance<HorrorEventConfig>();
    config.meanInterval = 30f;
    config.stdDeviation = 5f;
    config.minInterval = 10f;

    var strategy = new GaussianTimingStrategy();
    float result = strategy.GetNextInterval(config);

    Assert.GreaterOrEqual(result, config.minInterval);
}
```

---

## 10. Global Servisler ve Bağımlılık Yönetimi

**Kullanılan Pattern: Service Locator**

Singleton anti-pattern yerine tüm global servisler `ServiceLocator`'a kaydedilir. Her servis bir interface arkasına saklandığı için **Unit Test** yazarken mock implementasyon kolayca yerleştirilebilir.

**Kayıt sırası kritiktir:**

```
Awake() sırası (Script Execution Order ile belirlenir):
    1. AudioManager.Awake()   → ServiceLocator.Register<IAudioManager>(this)
    2. SaveSystem.Awake()     → ServiceLocator.Register<ISaveSystem>(this)
    3. EventBus (static)      → Zaten hazır, kayıt gerekmez
    4. SanitySystem.Awake()   → ServiceLocator.Register<ISanitySystem>(this)

Start() sırası (tüm Awake'ler tamamlandıktan sonra):
    5. PlayerController.Start() → ServiceLocator.Get<IAudioManager>()
    6. HUDController.Start()    → ServiceLocator.Get<ISanitySystem>()
```

**Test izolasyonu örneği:**

```csharp
// Test setup içinde mock kayıt:
ServiceLocator.Register<IAudioManager>(new MockAudioManager());
ServiceLocator.Register<ISaveSystem>(new MockSaveSystem());
// → Gerçek dosya yazımı olmadan SaveSystem testi yapılabilir
```

---

## 11. Object Pool — Sistemler Arası Kesişim Noktası

> Object Pool yalnızca mermi veya kan efekti için değildir. Oyundaki **neredeyse her sistem** çalışma zamanında tekrarlı oluşturma/yok etme yapan nesneler barındırır. Bu nedenle Object Pool, projede **kesişimsel (cross-cutting) bir altyapı pattern'i** olarak ele alınmalıdır.

### 11.1 Pool Gerektiren Tüm Sistemler

| Sistem | Pool'lanan Nesne | Neden Pool? | Yaşam Döngüsü |
|---|---|---|---|
| **Ses Sistemi** | `AudioSource` bileşeni | Her SFX için yeni GO oluşturmak GC spike yapar | Ses bitince → pool'a geri |
| **VFX Sistemi** | `ParticleSystem` prefab'ları (kan, toz, kıvılcım) | Particle prefab instantiate maliyetli | Efekt bitince → pool'a geri |
| **Mermi / Projectile** | `BulletProjectile` prefab | Yüksek frekanslı spawn (her ateş) | Çarpma veya menzil sonunda → pool'a geri |
| **Halüsinasyon Spawn** | Gölge / sahte düşman prefab'ları | Prosedürel sistemde sık spawn | Yaşam süresi dolunca → pool'a geri |
| **Jumpscare** | Flash overlay, kamera efekti nesneleri | Ani spawn + kısa ömür | Animasyon bitince → pool'a geri |
| **UI Popup** | Damage number, bildirim popup'ı | Sık oluşturulan kısa ömürlü UI | Fade-out sonrası → pool'a geri |
| **Düşman AI** | Waypoint marker, algı debug gizmo | Editor/runtime debug nesneleri | Kullanım bitince → pool'a geri |

### 11.2 Pool Mimarisi — Merkezi Kayıt, Dağıtık Kullanım

```
PoolManager (ServiceLocator'a kayıtlı)
    ├── Register<T>(prefab, initialSize, maxSize)
    ├── Get<T>() → T                    // Pool'dan al
    └── Release<T>(instance)            // Pool'a geri bırak

Kullanım noktaları (birbirinden habersiz):
    ├── AudioManager         → PoolManager.Get<AudioSource>()
    ├── VFXController        → PoolManager.Get<ParticleSystem>()
    ├── WeaponController     → PoolManager.Get<BulletProjectile>()
    ├── HallucinationSpawner → PoolManager.Get<HallucinationPrefab>()
    ├── JumpscareManager     → PoolManager.Get<FlashOverlay>()
    └── UIManager            → PoolManager.Get<DamagePopup>()
```

### 11.3 EventBus ile Pool Yaşam Döngüsü

Pool'dan alınan her nesne, işi bittiğinde **EventBus aracılığıyla** veya **callback ile** geri bırakılır. Bu, pool'un kullanıcı sistemlerle **loose-coupled** kalmasını sağlar.

```csharp
// Ses sistemi örneği — tam yaşam döngüsü
public class AudioManager : MonoBehaviour, IAudioManager {
    public void PlaySFX(AudioClip clip, Vector3 position) {
        var source = PoolManager.Get<AudioSource>();
        source.transform.position = position;
        source.clip = clip;
        source.Play();
        StartCoroutine(ReleaseWhenDone(source));
    }

    private IEnumerator ReleaseWhenDone(AudioSource source) {
        yield return new WaitWhile(() => source.isPlaying);
        PoolManager.Release(source);
    }
}

// VFX sistemi örneği
public class VFXController : MonoBehaviour {
    public void SpawnBloodSplatter(Vector3 position, Vector3 normal) {
        var vfx = PoolManager.Get<ParticleSystem>();
        vfx.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
        vfx.Play();
        StartCoroutine(ReleaseWhenStopped(vfx));
    }

    private IEnumerator ReleaseWhenStopped(ParticleSystem vfx) {
        yield return new WaitUntil(() => !vfx.isPlaying);
        PoolManager.Release(vfx);
    }
}

// Halüsinasyon — EventBus ile tetiklenen pool kullanımı
void OnEnable() => EventBus<HallucinationSpawnEvent>.Subscribe(OnHallucination);
void OnDisable() => EventBus<HallucinationSpawnEvent>.Unsubscribe(OnHallucination);

private void OnHallucination(HallucinationSpawnEvent e) {
    var ghost = PoolManager.Get<HallucinationPrefab>();
    ghost.Initialize(e.SpawnPosition, e.Lifetime);
    // Yaşam süresi dolunca kendi kendini pool'a geri bırakır
    ghost.OnLifetimeExpired += () => PoolManager.Release(ghost);
}
```

### 11.4 Pool Kesişim Haritası

Aşağıdaki diyagram Object Pool'un diğer pattern'lerle nasıl kesiştiğini gösterir:

```
                    ┌─────────────────────┐
                    │     Object Pool     │
                    │   (Infrastructure)  │
                    └──────────┬──────────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
            ▼                  ▼                  ▼
    ┌───────────────┐  ┌───────────────┐  ┌───────────────┐
    │ Service       │  │   EventBus    │  │ ScriptableObj │
    │ Locator       │  │  (Observer)   │  │  (Config)     │
    │               │  │               │  │               │
    │ Pool'u global │  │ Pool nesneyi  │  │ Pool boyutu,  │
    │ servis olarak │  │ event ile al/ │  │ max kapasitesi│
    │ kaydet        │  │ geri bırak    │  │ SO'da tanımlı │
    └───────┬───────┘  └───────┬───────┘  └───────┬───────┘
            │                  │                  │
            ▼                  ▼                  ▼
    ┌─────────────────────────────────────────────────────┐
    │            Tüketen Sistemler                        │
    │  Audio │ VFX │ Mermi │ Halüsinasyon │ Jumpscare│ UI│
    └─────────────────────────────────────────────────────┘
```

### 11.5 Pool Konfigürasyonu — ScriptableObject

```
PoolConfig (ScriptableObject)
    ├── string poolId            = "AudioSource"
    ├── GameObject prefab        → AudioSource prefab referansı
    ├── int initialSize          = 10
    ├── int maxSize              = 30
    ├── bool autoExpand          = true
    └── float autoShrinkDelay   = 60f    // Kullanılmayan fazla nesneyi temizle
```

**Kural:** Her pool'un boyutu ve davranışı `PoolConfig` SO ile belirlenir. Kod içinde hardcode `new Pool(20)` gibi ifadeler **yasaktır**.

---

## 12. Pattern Birlikte Çalışma Akışları

> Bu bölüm, pattern'lerin tekil kullanımının ötesinde **birbirleriyle nasıl zincirlendiğini** somut senaryolar üzerinden gösterir.

### 12.1 Senaryo: Oyuncu Karanlıkta Yürüyor → Düşman Duyuyor → Kovalamaca → Jumpscare

Bu akış 6 farklı pattern'in zincirleme çalışmasını gösterir:

```
[1] Decorator — Oyuncu karanlıkta, SilentStep yok
    PlayerStats.SoundRadius = 3f (base, decorator yok)
         │
         ▼
[2] Composite (Sensor) — HearingSensor tetiklendi
    SensorComposite.Evaluate()
    → HearingSensor: mesafe < SoundRadius? EVET
         │
         ▼
[3] EventBus — Algı bildirimi
    EventBus<EnemyDetectedSoundEvent>.Publish(soundPos)
         │
         ▼
[4] State Machine — Durum geçişi
    PatrolState → InvestigateState → ChaseState
    ChaseState.Enter(): müzik değişimi event'i yayınla
         │
         ▼
[5] Strategy — Kovalama algoritması
    ChaseState.Tick() → _navigationStrategy.Navigate()
    FlankingStrategy: oyuncunun önüne geçmeye çalış
         │
         ▼
[6] Command + EventBus — Saldırı anında jumpscare
    agent.remainingDistance < attackRange
    → JumpscareManager.Trigger()
        → EventBus<JumpscareTriggeredEvent>.Publish()
            ├── AudioManager    → Stinger (Pool'dan AudioSource)
            ├── CinemachineImpulse → Kamera sarsıntısı
            ├── PostProcess     → Flash (Pool'dan overlay)
            └── SanitySystem    → sanity.Decrease(15f)
```

### 12.2 Senaryo: Sanity Düşüşü → Halüsinasyon → Oyuncu Gizlenmesi → Düşman Kaybetmesi

```
[1] Observer (EventBus) — Sanity bildirimi
    SanitySystem: sanity = 22f (< 25 eşiği)
    → EventBus<PlayerSanityChangedEvent>.Publish(22f)
         │
         ▼
[2] Strategy + ScriptableObject — Prosedürel halüsinasyon
    ProceduralHorrorManager:
    → HallucinationConfig (SO): maxSanityThreshold = 30f → aktif
    → GaussianTimingStrategy: 4.7 sn sonra tetikle
    → Pool'dan halüsinasyon prefab al
    → EventBus<HallucinationSpawnEvent>.Publish()
         │
         ▼
[3] Decorator — Halüsinasyon etkisi
    → EventBus dinleyicisi: SpeedDebuffDecorator ekle
    → PlayerStats.MoveSpeed: 5f × 0.7 = 3.5f
         │
         ▼
[4] Command + Interface — Oyuncu gizleniyor
    → Oyuncu HideSpot'a raycast → IInteractable.CreateCommand()
    → HideCommand.Execute()
        → player.AddDecorator(SilentStepDecorator, SoundRadius = 0)
        → EventBus<PlayerHidingEvent>.Publish()
         │
         ▼
[5] State Machine + Composite — Düşman kaybediyor
    → EnemyAI: HearingSensor artık tetiklenmiyor (SoundRadius = 0)
    → SightSensor: oyuncu gizlenme noktasında → görünmüyor
    → ChaseState → SearchState → PatrolState (süre doldu)
         │
         ▼
[6] Observer — Gizlenmeden çıkış
    → HideCommand.Undo()
    → SilentStepDecorator kaldırıldı
    → EventBus<PlayerHidingEndEvent>.Publish()
    → AudioManager: "Calm" snapshot'a geçiş
```

### 12.3 Senaryo: Prosedürel Işık Titremesi → Sanity Düşüşü → Adaptif Müzik

```
[1] Strategy + SO → ProceduralHorrorManager
    LightFlickerConfig (SO): meanInterval = 20f
    GaussianTimingStrategy → 18.1 sn hesaplandı
         │
         ▼
[2] EventBus → Işık titremesi
    EventBus<LightFlickerEvent>.Publish(position, duration, curve)
    → LightController: ışığı flickerCurve'e göre titre
    → SanitySystem: sanity.Decrease(config.sanityImpact)
         │
         ▼
[3] EventBus → Sanity değişimi yayılması
    EventBus<PlayerSanityChangedEvent>.Publish(newSanity)
    → PostProcessController: vignette artır
    → AudioManager: Snapshot geçişi ("Tense" → "Danger")
    → HUDController: sanity bar güncelle
         │
         ▼
[4] Service Locator → Adaptif müzik
    AudioManager (ServiceLocator'dan erişilen):
    → AudioMixer.TransitionToSnapshot("Danger", 2f)
    → Ambient layer: string tremolo ekle
    → Stinger kuyruğu: sonraki stinger'ı yakınlaştır
```

### 12.4 Pattern Etkileşim Matrisi

| Pattern A | Pattern B | Etkileşim Biçimi | Örnek |
|---|---|---|---|
| **State Machine** | **Strategy** | SM "ne" yapılacağını, Strategy "nasıl" yapılacağını belirler | ChaseState → FlankingStrategy |
| **State Machine** | **EventBus** | Durum geçişleri event olarak yayınlanır | ChaseState.Enter() → ChaseStartedEvent |
| **EventBus** | **Object Pool** | Pool nesnesi event ile alınır/bırakılır | JumpscareEvent → Pool'dan AudioSource |
| **Strategy** | **ScriptableObject** | Strateji parametreleri SO'dan okunur | GaussianTiming → HorrorEventConfig |
| **Decorator** | **EventBus** | Decorator ekleme/çıkarma event ile tetiklenir | SanityChanged → SpeedDebuff ekle |
| **Command** | **EventBus** | Komut çalıştırma sonrası event yayınlanır | HideCommand → PlayerHidingEvent |
| **Command** | **Decorator** | Komut Decorator ekler/çıkarır | HideCommand → SilentStepDecorator |
| **Composite** | **State Machine** | Sensor sonucu SM geçişi tetikler | SensorComposite → InvestigateState |
| **Service Locator** | **Object Pool** | Pool servisi SL'ye kayıtlıdır | SL.Get<IPoolManager>() |
| **Memento** | **Service Locator** | SaveSystem SL'den erişilir | SL.Get<ISaveSystem>().AutoSave() |

---

## 13. Pattern Çakışma ve Entegrasyon Notları

### Event Bus Aşırı Kullanım Riski

Event Bus haberleşmeyi kolaylaştırır; ancak her şeyi event üzerinden yapmak **debug zorluğuna** yol açar. Kural olarak:

- **Event Bus kullan:** Farklı sistemler arası bildirim (Sanity → Audio, AI → HUD)
- **Doğrudan referans kullan:** Aynı sistem içi iletişim (SanitySystem → kendi PostProcess bileşeni)

### State Machine ile Strategy Ayrımı

| Karar | State Machine | Strategy |
|---|---|---|
| **Ne yapılacak?** (Patrol mu, Chase mi?) | ✅ | ❌ |
| **Nasıl yapılacak?** (Direkt mi, Flank mı?) | ❌ | ✅ |
| **Geçiş koşulları** | ✅ | ❌ |
| **Algoritma değişimi** | ❌ | ✅ |

### Object Pool ve Event Bus Entegrasyonu

Ses kaynakları ve efektler pool'dan alınır; kullanım bitince event ile serbest bırakılır:

```
AudioSource pool'dan al
    → Ses çal
        → OnAudioFinished event
            → Pool'a geri bırak
```

### ScriptableObject ile Pattern Sinerjisi

Tüm konfigürasyon değerleri (düşman hızı, sanity azalma miktarı, waypoint yarıçapı) `ScriptableObject`'te tutulur. Bu, pattern içindeki `magic number` sorununu ortadan kaldırır ve tasarımcıların kod açmadan değer değiştirmesini sağlar.

```
SanityConfig (ScriptableObject)
    ├── float decayRateInDarkness = 0.5f
    ├── float decayRateNearEnemy  = 2.0f
    └── float recoveryRate        = 0.1f

EnemyConfig (ScriptableObject)
    ├── float sightAngle     = 60f
    ├── float hearingRadius  = 8f
    └── float chaseSpeed     = 4.5f
```

---

## Özet: Hangi Problemi Hangi Pattern Çözer?

| Problem | Çözüm Pattern | Anahtar Fayda |
|---|---|---|
| "Global servislere her yerden nasıl erişirim?" | Service Locator | Singleton bağımlılığı yok, test edilebilir |
| "Sistemler birbirini bilmeden nasıl haberleşir?" | Event Bus | Loose coupling, yeni sistem eklemek kolay |
| "Düşman davranışları karmaşık if/else'e dönüştü" | State Machine | Her durum izole, geçişler açık |
| "Farklı düşmanlar farklı davranmalı" | Strategy | Runtime'da algoritma değişimi |
| "Oyuncuya yetenek ekle/çıkar" | Decorator | Base değerleri bozmadan katman ekleme |
| "Eylemi geri alabilmek istiyorum" | Command | Execute/Undo, replay sistemi |
| "Mermi spawn GC baskısı yaratıyor" | Object Pool | Sıfır allocation, performans |
| "Oyun durumunu diske yaz/oku" | Memento | Snapshot tabanlı kayıt izolasyonu |
| "Etkileşilebilir nesneler farklı türde ama aynı arayüzde" | Interface (IInteractable) | Polimorfizm, Raycast tek nokta |
| "Her sistemde tekrarlı nesne oluşturma var" | Object Pool (kesişimsel) | Tek altyapı, tüm sistemler paylaşır |
| "Pattern'ler izole çalışmıyor, birlikte kullanılmalı" | Pattern Zincirleme | Senaryo bazlı akış, sorumluluk ayrımı |

---

*Unity 3D FPS Horror — Mekanik & Pattern Rehberi v2.0*
*Referans: unity_fps_horror_guide.md*