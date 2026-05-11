# Horror Game — Geliştirme ve Doğrulama Protokolü (Codex / AI Assistant)

Bu proje kesin bir geliştirme protokolüne tabidir. Codex tabanlı bir AI asistanı olarak kod üretirken, herhangi bir mekanik geliştirmeye başlamadan önce aşağıdaki kurallara ve iş akışına KESİNLİKLE uymalısın.

---

## 📍 Proje Bilgileri

| Alan | Değer |
|------|-------|
| Proje Yolu | `c:\Users\Castiel\Desktop\Unity Projects\Horror` |
| Unity Versiyonu | Unity 6 (6000.0.71f1) |
| Render Pipeline | HDRP |
| Dil | C# |
| Mimari | Component-based + Service Locator + Event-driven |
| PRD Dosyası | `prd.md` (proje kökünde) |
| Mimari Rehber | `unity_fps_horror_guide.docx` (proje kökünde) |

---

## 🚨 ATLANMAZ KURALLAR (Her Zaman Geçerli)

Bu kurallar **her koşulda** geçerlidir. İstisna yoktur.

### Kural 1: PRD'yi Oku
Her mekaniğe başlamadan önce `prd.md` dosyasındaki ilgili bölümü referans al.
- Kabul kriterlerini tespit et
- Önkoşulları (bağımlılıkları) kontrol et

### Kural 2: Önce Altyapı, Sonra Mekanik
M0 (Altyapı) tamamlanmadan hiçbir mekaniğe geçilemez.

### Kural 3: Test Yazmadan Tamamlanmış Sayma
Her mekanik için:
- En az 1 Unit Test (EditMode) — iş mantığı
- En az 1 PlayMode Test (mümkünse) — runtime davranış

### Kural 4: Kullanıcıya Test Raporu Ver
Her mekanik tamamlandığında kullanıcıya:
- Ne yapıldığını özetle
- Otomatik test sonuçlarını paylaş
- Manuel test talimatları ver (ne açılacak, ne denenecek, ne beklenmeli)

### Kural 5: Mimari İhlal Yapma
- `Singleton` kullanma → `ServiceLocator` kullan
- `Update()` içinde `GetComponent()` çağırma → `Awake()` içinde cache'le
- Sistemler arası doğrudan referans verme → `EventBus<T>` kullan
- Hardcode değer kullanma → `ScriptableObject` veya `[SerializeField]` kullan
- `MonoBehaviour` dışında test edilebilir mantık yaz

---

## 📋 MEKANİK GELİŞTİRME İŞ AKIŞI

1. **Analiz ve Planlama:** PRD ve mimari rehberi referans al.
2. **Interface ve Yapı Tasarımı:** IInteractable, IService vb. yapıları oluştur.
3. **Kod Yazımı:** Interface implementasyonu, EventBus, ServiceLocator.
4. **Test Yazımı:** Her kabul kriteri için Unit Test yaz.
5. **Debug Araçları:** Gizmo, Console log ve On-screen HUD ekle.
6. **Raporlama:** Kullanıcıya durum raporu ver.

---

## 🏛️ MİMARİ KURALLAR

### Kod Yapısı
- Tüm global servisler `IService` interface'ini implemente etmeli
- `Awake()` → ServiceLocator.Register, `Start()` → ServiceLocator.Get
- `Awake()` içinde tüm `GetComponent` çağrılarını cache'le

### Design Pattern Kullanımı
- Global servis erişimi: Service Locator
- Sistemler arası iletişim: EventBus<T>
- Düşman/Oyuncu durumları: Hierarchical State Machine
- Sık oluşturulan objeler: Object Pool (UnityEngine.Pool)

### EventBus Kuralları
- Subscribe → `OnEnable()` içinde
- Unsubscribe → `OnDisable()` içinde
- Her event bir `struct` olmalı (class değil — GC baskısı)
- Event isimlendirme: `[Sistem][Eylem]Event` → `PlayerSanityChangedEvent`

### ScriptableObject Kuralları
- Tüm konfigürasyon değerleri SO'da tutulmalı
- Hardcode magic number yasak

---

## 🧪 TEST YAZIM KURALLARI

### EditMode Test (Unit Test)
- Konum: `Assets/_Project/Tests/EditMode/`
- MonoBehaviour'a bağımlı OLMAYAN mantık test edilir
- NUnit `[Test]` attribute kullanılır

### PlayMode Test
- Konum: `Assets/_Project/Tests/PlayMode/`
- Runtime davranış gerektiren testler
- `[UnityTest]` attribute + `IEnumerator` kullanılır

---

## ⚡ HIZLI KONTROL LİSTESİ

- [ ] PRD kabul kriterleri karşılandı mı?
- [ ] Unit testler yazıldı mı?
- [ ] ServiceLocator kaydı yapıldı mı?
- [ ] EventBus bağlantıları kuruldu mu?
- [ ] Debug araçları eklendi mi?
- [ ] GetComponent Awake()'te cache'lendi mi?
- [ ] Singleton KULLANILMADIĞINDAN emin misin?
