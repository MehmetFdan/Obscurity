# UNITY 3D FPS HORROR GAME

Production-Ready Mimari, Pattern ve Teknoloji Rehberi

Unity 6 (LTS) | C# | URP | HDRP

Versiyon 1.0  —  2025

## 1. Giriş ve Proje Genel Bakış

Bu doküman, Unity ile production-ready bir 3D FPS (First-Person Shooter) korku oyunu geliştirmek için gereken mimari kararları, tasarım desenlerini, teknoloji seçimlerini ve en iyi pratikleri kapsamaktadır. Hedef, bakımı kolay, ölçeklenebilir ve yüksek performanslı bir kod tabanı oluşturmaktır.

| Konu | Detay |
| --- | --- |
| Hedef Platform | PC (Windows/Mac), Console (PS5, Xbox) |
| Unity Versiyonu | Unity 6 LTS (veya 2022.3 LTS) |
| Render Pipeline | HDRP (yüksek kalite) veya URP (performans odaklı) |
| Dil | C# (.NET Standard 2.1) |
| Mimari Yaklaşım | Component-based + Service Locator + Event-driven |

## 2. Mimari Genel Yapı

Bir FPS korku oyununda mimari, farklı sistemlerin birbirini minimumda etkileyecek şekilde tasarlanmasını gerektirir. Aşağıdaki katmanlı mimari önerilmektedir:

### 2.1 Katmanlı Mimari

| Katman | Sorumluluk |
| --- | --- |
| Presentation Layer | UI, HUD, ana menü, canavar efektleri — doğrudan oyun mantığı içermez |
| Game Systems Layer | Oyun mekanikleri: FPS controller, envanter, sağlık, AI, ses |
| Core Services Layer | EventBus, ServiceLocator, SceneLoader, SaveSystem, AudioManager |
| Data Layer | ScriptableObject veritabanları, JSON save/load, PlayerPrefs wrapper |
| Infrastructure Layer | Addressables, Pool Manager, Logger, profiler entegrasyonları |

### 2.2 Klasör Yapısı

Aşağıdaki klasör yapısı büyük ekiplerde bile düzeni korur:

Assets/

│    │    ├─ Scripts/

│    │    ├─ Core/          # ServiceLocator, EventBus, GameManager

│    │    ├─ Player/         # FPS Controller, Flashlight, Stamina

│    │    ├─ Enemy/          # AI, StateMachine, NavMesh

│    │    ├─ Gameplay/       # Inventory, Door, Interactable

│    │    ├─ Audio/          # AudioManager, Ambient, SFX

│    │    ├─ UI/             # HUD, PauseMenu, Sanity UI

│    │    ├─ Save/           # SaveSystem, SerializableData

│    │    └─ Utils/          # Extensions, Helpers, Constants

│    ├─ ScriptableObjects/   # GameData, EnemyConfig, ItemData

│    ├─ Prefabs/             # Player, Enemies, Props, VFX

│    ├─ Scenes/              # MainMenu, Level01..N, Loading

│    ├─ Materials/           # HDRP materials

│    ├─ VFX/                 # Particle systems, VFX Graph

│    └─ Audio/               # Music, SFX, Ambient

└─ Plugins/                  # Third-party assets

## 3. Temel Tasarım Desenleri (Design Patterns)

### 3.1 Service Locator Pattern

Unity'de Singleton yerine Service Locator kullanmak, bağımlılıkları gevşek bağlı tutar ve test edilebilirliği artırır. Her servis bir arayüz (interface) arkasına saklanır.

```csharp
public static class ServiceLocator {
private static readonly Dictionary<Type, object> _services = new();
public static void Register<T>(T service) =>
_services[typeof(T)] = service;
public static T Get<T>() {
if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
throw new Exception($"Service {typeof(T)} not registered!");
}
}
```

Ne Zaman Kullanılır?

AudioManager, SaveSystem, EventBus, SceneLoader gibi global servislere erişimde

Singleton anti-pattern yerine her zaman ServiceLocator tercih edin

Servisler Awake() içinde Register edilmeli, diğer bileşenler Start() içinde Get etmeli

### 3.2 Event Bus (Observer) Pattern

Sistemler arası haberleşmede doğrudan referans yerine Event Bus kullanmak, gevşek bağlılık sağlar. Korku oyununda sanity değişimi, kapı açılması, düşman tespiti gibi olaylar bu sistemle yayınlanır.

```csharp
// Olay tanımı
public struct PlayerSanityChangedEvent { public float NewSanity; }
public struct EnemyDetectedPlayerEvent  { public Transform Enemy; }
// EventBus implementasyonu
public static class EventBus<T> where T : struct {
static event Action<T> _event;
public static void Subscribe(Action<T> l)   => _event += l;
public static void Unsubscribe(Action<T> l) => _event -= l;
public static void Publish(T e)             => _event?.Invoke(e);
}
// Kullanım — EnemyAI.cs
EventBus<EnemyDetectedPlayerEvent>.Publish(new() { Enemy = transform });
// Kullanım — HUD.cs
void OnEnable()  => EventBus<PlayerSanityChangedEvent>.Subscribe(OnSanity);
void OnDisable() => EventBus<PlayerSanityChangedEvent>.Unsubscribe(OnSanity);
```

### 3.3 Hiyerarşik Durum Makinesi (Hierarchical State Machine)

Hem düşman AI hem de oyuncu durumları (yürüme, koşma, çömelme, gizlenme) için State Machine kullanmak şarttır. Karmaşık geçişler ve iç içe durumlar için Hierarchical State Machine (HSM) önerilir.

```csharp
public abstract class EnemyState {
protected EnemyAI Owner;
public virtual void Enter()  { }
public virtual void Tick()   { }
public virtual void Exit()   { }
}
public class EnemyPatrolState   : EnemyState { ... }
public class EnemyChaseState    : EnemyState { ... }
public class EnemyAttackState   : EnemyState { ... }
public class EnemyInvestigate   : EnemyState { ... }
```

| Düşman Durumu | Geçiş Koşulu |
| --- | --- |
| Patrol (Devriye) | Varsayılan durum — NavMesh waypoint izleme |
| Investigate (Araştır) | Ses duyuldu / iz bulundu — son bilinen konuma git |
| Chase (Koval) | Oyuncu görüş alanında — doğrudan kovalama |
| Attack (Saldır) | Oyuncuya yaklaşıldı — hasar ver |
| Search (Ara) | Oyuncu kayboldu — arama yaparak Patrol'e dön |
| Stunned (Sersem) | Işık çarptı / engel — kısa bekleme |

### 3.4 Object Pool Pattern

FPS oyunlarında mermi, kan efekti, ses kaynakları gibi sık oluşturulan/yok edilen objeler için Object Pool zorunludur. Unity 2021+ ile gelen UnityEngine.Pool API'si kullanılmalıdır.

```csharp
public class BulletPool : MonoBehaviour {
[SerializeField] private Bullet _prefab;
private ObjectPool<Bullet> _pool;
void Awake() => _pool = new ObjectPool<Bullet>(
createFunc:    () => Instantiate(_prefab),
actionOnGet:   b  => b.gameObject.SetActive(true),
actionOnRelease: b => b.gameObject.SetActive(false),
actionOnDestroy: b => Destroy(b.gameObject),
defaultCapacity: 30, maxSize: 100
);
public Bullet Get()          => _pool.Get();
public void  Release(Bullet b) => _pool.Release(b);
}
```

### 3.5 Command Pattern — Geri Al / Tekrar Et

Oyuncu eylemlerini (ışık aç/kapat, kapı kilitle, eşya kullan) Command olarak modellemek undo/redo desteği ve replay sistemi için temel oluşturur. Ayrıca input rebinding'i de kolaylaştırır.

```csharp
public interface ICommand {
void Execute();
void Undo();
}
public class ToggleLightCommand : ICommand {
private readonly Light _light;
public ToggleLightCommand(Light l) => _light = l;
public void Execute() => _light.enabled = !_light.enabled;
public void Undo()    => _light.enabled = !_light.enabled;
}
```

### 3.6 Decorator Pattern — Oyuncu Yetenekleri

Oyuncu üzerine eklenebilir yetenekler (hız artışı, görüş alanı genişletme, ses damperi) için Decorator pattern kullanılır. ScriptableObject tabanlı modifiers ile runtime'da yetenekler eklenip çıkarılabilir.

```csharp
public interface IPlayerStats {
float MoveSpeed { get; }
float StaminaRegen { get; }
float SoundRadius { get; }
}
public class SpeedBoostDecorator : IPlayerStats {
private readonly IPlayerStats _inner;
private readonly float _boost;
public SpeedBoostDecorator(IPlayerStats inner, float boost) {
_inner = inner; _boost = boost;
}
public float MoveSpeed    => _inner.MoveSpeed * _boost;
public float StaminaRegen => _inner.StaminaRegen;
public float SoundRadius  => _inner.SoundRadius;
}
```

### 3.7 Strategy Pattern — AI Davranış Değişimi

Farklı düşman tipleri için farklı navigasyon veya saldırı stratejileri, Strategy pattern ile runtime'da değiştirilebilir hale getirilir.

```csharp
public interface INavigationStrategy {
void Navigate(NavMeshAgent agent, Transform target);
}
public class DirectChaseStrategy    : INavigationStrategy { ... }
public class FlankingStrategy       : INavigationStrategy { ... }
public class AmbushStrategy         : INavigationStrategy { ... }
```

## 4. Temel Oyun Sistemleri

### 4.1 FPS Karakter Kontrolleri

| Sistem | Uygulama Detayı |
| --- | --- |
| Hareket | CharacterController + custom gravity (Rigidbody değil — daha güvenilir) |
| Kamera / Bakış | Cinemachine Brain + Virtual Camera — input ile birleştirilmiş mouse look |
| Ayak Sesi | Physics.Raycast ile zemin tipi tespiti — ağaç/beton/su sesi |
| Nefes Efekti | Cinemachine Noise Profiles — yorgunlukta kamera sallanması |
| Bob Efekti | ProceduralAnimation bileşeni — yürürken baş hareketi |
| Eğilme (Lean) | Kamera X rotasyonu + collider ayarı — köşeden bakma |

### 4.2 Sanity (Akıl Sağlığı) Sistemi

Korku oyununun kalbindeki sanity sistemi, oyuncunun psikolojik durumunu temsil eder ve tüm görsel/işitsel efektleri besler.

```csharp
public class SanitySystem : MonoBehaviour, IService {
[Range(0f,100f)] private float _sanity = 100f;
[SerializeField] private SanityConfig _config;
public void Decrease(float amount) {
_sanity = Mathf.Clamp(_sanity - amount, 0, 100);
EventBus<PlayerSanityChangedEvent>.Publish(new() { NewSanity = _sanity });
ApplyEffects(_sanity);
}
private void ApplyEffects(float sanity) {
// Post-processing vignette, chromatic aberration
// Hallucination objelerini spawn et
// Kalp atışı sesi frekansını artır
}
}
```

| Sanity Seviyesi | Görsel / İşitsel Efektler |
| --- | --- |
| 100-75 (Normal) | Temiz görüntü, normal müzik |
| 75-50 (Tedirgin) | Hafif vignette, ara sıra gürültü sesi |
| 50-25 (Korkmuş) | Chromatic aberration, film grain, yanlış sesler |
| 25-0 (Panik) | Halüsinasyonlar, görüntü distorsiyon, kalp sesi, düşman sesi artışı |

### 4.3 Düşman AI Sistemi

NavMesh tabanlı AI, birden fazla algı kanalı (görme, duyma, koku izi) ile desteklenmelidir. Unity AI Navigation paketi (yeni NavMesh API) kullanılmalıdır.

| AI Bileşeni | Görev |
| --- | --- |
| SightSensor | Raycast + FOV açısı — görme algısı, ışık seviyesi dikkate alınır |
| HearingSensor | Oyuncu SoundRadius'u dinle — adım sesi, düşme, nesne sesi |
| SmellSensor (isteğe bağlı) | Zamana dayalı iz bırakma — iz noktaları takibi |
| NavMeshAgent | Unity AI Navigation — yol bulma, kaçınma |
| EnemyStateMachine | Patrol/Investigate/Chase/Attack/Search döngüsü |
| AnimationController | Animator + Root Motion — Animator Override Controller |

### 4.4 Ses Sistemi

Korku oyunlarında ses en kritik unsurdur. Bu projede FMOD/Wwise yerine Unity'nin yerel ses araçları kullanılmalıdır: AudioSource, AudioListener, Audio Mixer, Audio Mixer Snapshot, Audio Reverb Zone ve Timeline/Signal tabanlı tetikleme.

Unity Yerel Ses Mimarisi (Önerilen)

AudioSource + AudioClip: Ses kaynakları, 3D spatial ayarlar ve clip yönetimi Unity içinde tutulur

Adaptive Music: Sanity/Tehlike seviyesine göre müzik katmanları Audio Mixer exposed parameter'ları ve crossfade mantığı ile dinamik değişir

3D Spatialization: AudioSource Spatial Blend, rolloff eğrileri ve gerekirse Unity Spatializer ayarları ile atmosferik mekan hissi

Snapshot System: Audio Mixer Snapshot'ları ile 'Gizlenme', 'Kovalama', 'Sakin' gibi durumlar arası geçiş

Reverb Zones: Audio Reverb Zone ve mixer send'leri ile oda boyutuna göre reverb kontrolü

Stinger / Event Tetikleme: Timeline Signal, Animation Event veya gameplay event'leri üzerinden tek seferlik korku sesleri

| Ses Kategorisi | Örnekler |
| --- | --- |
| Ambiance | HVAC uğultusu, su sızıntısı, uzak gürültüler — loop |
| Music (Adaptive) | Tehlike yok: minimal piano, Tehlike var: string tremolo |
| Player SFX | Adım sesleri (zemin tipine göre), nefes, kalp atışı |
| Enemy SFX | Adım, vücut sürünme, nefes, ses tonu — sanity'e göre yaklaştırma |
| Jumpscare SFX | Anlık yüksek-frekans çarpıcılar — stinger sesleri |
| UI SFX | Envanter açma, not okuma, kapı kilidi |

### 4.5 Kayıt / Yükleme Sistemi (Save/Load)

Production seviyesinde bir kayıt sistemi, hem checkpoint hem de manuel save desteklemeli; şifrelenmiş JSON dosyasına yazmalıdır.

```csharp
[System.Serializable]
public class GameSaveData {
public string   SceneName;
public float    Sanity;
public float    Health;
public float[]  PlayerPosition;   // Vector3 serialize edilemiyor
public List<string> CollectedItems;
public Dictionary<string, bool> DoorStates;
public long     SaveTimestamp;
}
// Kayıt
var json = JsonUtility.ToJson(data);
var encrypted = AesEncrypt(json, _secretKey);
File.WriteAllText(savePath, encrypted);
```

## 5. Grafik ve Görsel Teknolojiler

### 5.1 Render Pipeline Seçimi

| Pipeline | Kullanım Senaryosu |
| --- | --- |
| HDRP (High Definition) | PC/Console — Photorealistic aydınlatma, ray tracing, volumetric fog |
| URP (Universal) | Geniş platform desteği — daha performanslı, mobile de çalışır |

HDRP Önerilen Özellikler: Volumetric Lighting, Screen Space Reflections (SSR), Ambient Occlusion (GTAO), Contact Shadows, Planar Reflection, Motion Blur.

### 5.2 Post-Processing Etkileri

| Efekt | Korku Oyunundaki Rolü |
| --- | --- |
| Vignette | Sanity düşükken ekran kenarlarını karartır |
| Chromatic Aberration | Stres altında renk kanalı ayrışması |
| Film Grain | Sinema filmi hissi — düşük sanity |
| Lens Distortion | Halüsinasyon efekti, şiddetli distorsiyon |
| Depth of Field | Odak noktası dışını bulanıklaştırma — sinematik |
| Bloom | Işık kaynaklarında hale — el feneri, mum |
| Color Grading (LUT) | Soğuk-mavi palet — distopik/korku atmosferi |
| Screen Space AO | Köşe ve çatlakları koyulturak derinlik hissi |

### 5.3 Aydınlatma Stratejisi

Baked Lighting: Statik objeler için — Enlighten veya Progressive Lightmapper

Mixed Lighting: Hareketli düşman gölgeleri için

Real-time Shadows: Oyuncu el feneri — Shadow Atlas optimizasyonu

Light Probes: Dinamik objelerin ortam ışığını alması için

Reflection Probes: Islak zemin, metal yüzey yansımaları

Emissive Materials: Acil çıkış işaretleri, ekranlar — halo efekti

## 6. Performans Optimizasyonu

### 6.1 CPU Optimizasyonu

| Teknik | Açıklama |
| --- | --- |
| ECS / DOTS (isteğe bağlı) | Yüzlerce düşman için Data-Oriented Technology Stack — Unity Entities paketi |
| Job System | Paralel C# iş parçacıkları — AI pathfinding, fizik hesaplamaları |
| Burst Compiler | LLVM tabanlı native kod derlemesi — matematiksel hesaplamalar |
| Coroutine Optimizasyonu | Sık çalışan coroutine yerine Update() scheduling veya UniTask |
| GetComponent Cache | Awake() içinde tüm referansları önbelleğe al, Update'te asla çağırma |
| LOD (Level of Detail) | Uzak objelerde düşük polygon mesh — LOD Group bileşeni |

### 6.2 GPU Optimizasyonu

| Teknik | Açıklama |
| --- | --- |
| GPU Instancing | Aynı prefab'ın binlerce kopyası (ağaç, taş) tek draw call ile |
| Static Batching | Statik objeleri bir mesh'te topla — build zamanında |
| Dynamic Batching | Küçük dynamic meshler için otomatik birleştirme |
| Texture Atlas | Birden fazla doku tek bir atlasta — material sayısını azalt |
| Mipmapping | Uzak dokular için düşük çözünürlük seçimi — GPU bandwidth |
| Shader LOD | Uzak objeler için basit shader — Surface Shader yerine Unlit/Simple |
| Occlusion Culling | Kamera görüş alanı dışındaki objeleri render etme — bake gerekli |

### 6.3 Bellek Optimizasyonu

| Teknik | Açıklama |
| --- | --- |
| Addressables | Asset yönetimi — sahneleri/prefabları async yükle/boşalt |
| Object Pooling | Mermi, kan efekti, ses kaynakları — GC baskısını azalt |
| Texture Compression | ASTC (mobile), BC7 (PC) — platform bazlı sıkıştırma |
| Audio Compression | Büyük müzikler: Vorbis streaming; SFX: ADPCM/PCM |
| Mesh Compression | Model import ayarlarında mesh compression: Medium/High |

### 6.4 Profiling Araçları

| Araç | Kullanım Amacı |
| --- | --- |
| Unity Profiler | CPU/GPU/Memory anlık analizi — builtin |
| Frame Debugger | Her draw call'un adım adım incelenmesi |
| Memory Profiler | Heap snapshot — bellek sızıntısı tespiti |
| RenderDoc | GPU frame capture — HDRP shader debug |
| Deep Profile | C# method bazlı CPU maliyeti — allocation hotspot |

## 7. Gerekli Paketler ve Araçlar

### 7.1 Unity Resmi Paketleri

| Paket | Kullanım |
| --- | --- |
| Cinemachine 3.x | Kamera sistemi — shake, follow, dolly, virtual cam yönetimi |
| Input System (new) | Action-based input — rebinding, gamepad/KB desteği, context actions |
| AI Navigation (NavMesh) | NavMesh Surface, NavMesh Agent, NavMesh Obstacle |
| Addressables | Asenkron asset yükleme/boşaltma — scene ve prefab yönetimi |
| TextMeshPro | Yüksek kaliteli UI text — SDF font rendering |
| Post Processing (HDRP/URP built-in) | Volume system ile post-process efektleri |
| Timeline + Cinemachine | Cutscene animasyonları — sinematik sahneler |
| VFX Graph | GPU tabanlı partikül sistemleri — büyük efektler |
| Shader Graph | Node tabanlı shader editörü — programlama gerektirmez |
| Unity Localization | Çoklu dil desteği — string/asset localization |
| Plastic SCM / VCS | Yerleşik versiyon kontrolü (Git alternatifleri) |

### 7.2 Üçüncü Parti (Asset Store) Araçlar

| Asset/Araç | Kullanım Amacı |
| --- | --- |
| DOTween | UI ve transform animasyonları — yüksek performanslı tween kütüphanesi |
| UniTask | Async/await tabanlı coroutine alternatifi — GC-free async |
| ProBuilder | In-editor level design/prototyping — hızlı mimari oluşturma |

Bu projede aşağıdaki Asset Store araçları kullanılmayacaktır; aynı ihtiyaçlar Unity'nin yerel araçlarıyla karşılanacaktır.

| Kullanılmayacak Araç | Unity Yerel Karşılığı |
| --- | --- |
| Behavior Designer | C# tabanlı EnemyStateMachine + Unity AI Navigation (NavMeshAgent/NavMeshSurface) |
| Rewired | Unity Input System (Action Maps, rebinding, gamepad/klavye-fare desteği) |
| Amplify Shader Editor | Shader Graph + URP/HDRP Volume ve Renderer Feature akışı |
| FMOD Studio Unity | AudioSource, Audio Mixer, Audio Mixer Snapshot, Audio Reverb Zone |
| Odin Inspector | Custom Editor, PropertyDrawer, EditorWindow, ScriptableObject tabanlı editör araçları |

### 7.3 Test Araçları

| Araç | Kullanım Amacı |
| --- | --- |
| Unity Test Framework | Unit ve Integration testleri — NUnit tabanlı |
| Unity Test Runner (EditMode) | Editor'da script mantığı testleri |
| Unity Test Runner (PlayMode) | Runtime davranış testleri — sahne içi |
| Moq (uyumlu fork) | Interface mock'lama — ServiceLocator ile test izolasyonu |

## 8. Versiyon Kontrol ve İş Akışı

### 8.1 Git Yapılandırması

.gitignore ve .gitattributes dosyaları Unity projeleri için kritik öneme sahiptir. Unity Hub'dan edinilen template kullanılmalıdır.

```csharp
# .gitattributes — binary dosyaları LFS'e yönlendir
*.unity  filter=lfs diff=lfs merge=lfs -text
*.prefab filter=lfs diff=lfs merge=lfs -text
*.fbx    filter=lfs diff=lfs merge=lfs -text
*.png    filter=lfs diff=lfs merge=lfs -text
*.wav    filter=lfs diff=lfs merge=lfs -text
*.mp3    filter=lfs diff=lfs merge=lfs -text
```

### 8.2 Branch Stratejisi (Git Flow)

| Branch | Amaç |
| --- | --- |
| main | Release-ready, her zaman build edilebilir — sadece merge |
| develop | Aktif geliştirme ana dalı |
| feature/xxx | Her yeni özellik kendi branch'ında — develop'a PR |
| hotfix/xxx | Kritik bug düzeltmeleri — main'den açılır, main'e merge |
| release/xxx | Release öncesi stabilizasyon dalı |

## 9. CI/CD ve Build Pipeline

| Araç | Kullanım |
| --- | --- |
| Unity Build Automation (Cloud Build) | Unity Dashboard üzerinden otomatik build — multiplatform |
| GitHub Actions + GameCI | Self-hosted runner ile Linux/Win build — açık kaynak Unity Docker |
| fastlane | iOS/Android store deploy otomasyonu |
| Semantic Versioning | Major.Minor.Patch — PlayerSettings.bundleVersion otomatik güncelleme |

GitHub Actions örneği (`.github/workflows/build.yml`):

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: StandaloneWindows64
          unityVersion: 6000.0.0f1
```

## 10. Korku Oyununa Özel Mekanikler

### 10.1 Jumpscare Sistemi

| Bileşen | Detay |
| --- | --- |
| JumpscareManager | Cooldown sistemi — ardı ardına jumpscare'den kaçın |
| Trigger Zones | Collider-based tetikleyiciler — kapı, köşe, dolap |
| Audio Stinger | Ani yüksek volümlü ses — Audio Mixer Snapshot |
| Camera Shake | Cinemachine Impulse Source — fiziksel sarsıntı |
| Animator | Düşman animasyonu — lunge/appear |
| PostProcess Flash | Anlık beyaz flash — Vignette + Bloom overexpose |

### 10.2 Gizlenme / Saklanma Sistemi

```csharp
public class HideSpot : MonoBehaviour, IInteractable {
[SerializeField] private Transform _cameraTarget;
[SerializeField] private Collider  _playerCollider;
public void Interact(PlayerController player) {
if (player.IsHiding) player.ExitHide(this);
else                 player.EnterHide(this, _cameraTarget);
}
// Düşman bu noktayı geçince PlayerSound.SoundRadius = 0
}
```

### 10.3 Ortam Etkileşim Sistemi

Tüm etkileşilebilir nesneler IInteractable arayüzünü implemente etmelidir. Raycast-based interaction tercih edilir.

```csharp
public interface IInteractable {
string InteractPrompt { get; }
void Interact(PlayerController player);
bool CanInteract(PlayerController player);
}
// Uygulayan sınıflar:
// DoorController, DrawerController, NotePickup,
// LightSwitch, KeyItem, HideSpot, PuzzleObject
```

### 10.4 Prosedürel Korku Sistemi

Düşman pozisyonlarını ve sesleri yarı-rastgele yerleştiren bir sistem oyun tekrarlanabilirliğini artırır ve tahmin edilemezliği korur.

| Mekanik | Uygulama |
| --- | --- |
| Waypoint Randomizasyonu | Düşman devriye yolları her oturumda farklı — NavMesh random sampling |
| Ses Tetikleyici Rastgeleliği | Ambient korkutucu sesler rastgele aralıklarda — Poisson disk dağılımı |
| Işık Titremesi | Coroutine ile rastgele ışık kesilmeleri — sanity etkisi |
| Olay Zamanlama | Jumpscare ve düşman görünmeleri Gaussian dağılımla zamanlama |

## 11. Production-Ready Kontrol Listesi

Mimari & Kod Kalitesi

Service Locator ile Singleton bağımlılıkları kaldırıldı

EventBus ile cross-system iletişim sağlandı

Tüm servisler interface arkasında — Unit test edilebilir

ScriptableObject tabanlı konfigürasyon — hardcode değer yok

Object Pool tüm sık oluşturulan objeler için aktif

Assembly Definition Files (.asmdef) ile compile süreleri optimize edildi

Performans

60 FPS @ 1080p (PC Low), 30 FPS @ 4K (PC Ultra) hedefi

Draw call sayısı < 300 (mid-range GPU hedef)

Bellek kullanımı < 4 GB RAM

Addressables ile asset streaming — sahne yükleme < 3 saniye

Occlusion culling aktif

Kullanıcı Deneyimi

Erişilebilirlik: Alt yazı, colorblind modu, motion sickness azaltma seçeneği

Grafik ayarları: Quality preset (Düşük/Orta/Yüksek/Ultra)

Input rebinding: Tüm eylemler yeniden atanabilir

Save sistemi: Otomatik checkpoint + manuel kayıt

Build & Release

CI/CD pipeline aktif — her commit'te test ve build

Tüm platform hedefleri (PC, Console) CI'da doğrulandı

Crash reporter entegre (Backtrace, Sentry veya Unity Cloud Diagnostics)

Analytics entegre (Unity Analytics veya GameAnalytics) — funnel analizi

Localization: TR/EN/DE/FR/ES minimum

## 12. Önerilen Kaynaklar

| Kaynak | İçerik |
| --- | --- |
| Unity Documentation (docs.unity3d.com) | Resmi API referansı ve manual |
| Unity Learn Premium | Proje tabanlı kurslar — resmi içerik |
| Game Programming Patterns (Robert Nystrom) | Oyun tasarım desenleri — ücretsiz online |
| Clean Code (Robert Martin) | C# kod kalitesi için temel eser |
| GDC Vault (gdcvault.com) | AAA studio teknik sunumları — AI, rendering |
| Catlike Coding (catlikecoding.com) | İleri düzey Unity/shader öğreticileri |
| Freya Holmer — YouTube | Matematik ve shader konuları — görsel öğretim |
| Infallible Code — YouTube | Unity mimari ve best practices |
| Jason Weimann — YouTube | Unity game architecture patterns |

İyi kodlamalar — Korkunun keyfi, mimarinin sağlamlığında!  🎮

Unity 3D FPS Horror Game — Architecture Reference v1.0
