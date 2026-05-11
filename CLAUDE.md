# Horror Game — Geliştirme ve Doğrulama Protokolü (Claude)

Bu proje kesin bir geliştirme protokolüne tabidir. Claude Code olarak, herhangi bir mekanik geliştirmeye başlamadan önce aşağıdaki kurallara ve iş akışına KESİNLİKLE uymalısın.

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
Her mekaniğe başlamadan önce `prd.md` dosyasındaki ilgili bölümü oku.
- Kabul kriterlerini tespit et
- Önkoşulları (bağımlılıkları) kontrol et
- Önkoşullar tamamlanmamışsa mekaniğe BAŞLAMA

### Kural 2: Önce Altyapı, Sonra Mekanik
M0 (Altyapı) tamamlanmadan hiçbir mekaniğe geçilemez.
Bağımlılık haritasındaki sıralamaya uy.

### Kural 3: Test Yazmadan Tamamlanmış Sayma
Her mekanik için:
- En az 1 Unit Test (EditMode) — iş mantığı
- En az 1 PlayMode Test (mümkünse) — runtime davranış
- Compile check — hata sıfır olmalı

### Kural 4: Kullanıcıya Test Raporu Ver
Her mekanik tamamlandığında kullanıcıya:
- Ne yapıldığını özetle
- Otomatik test sonuçlarını paylaş
- Manuel test talimatları ver (ne açılacak, ne denenecek, ne beklenmeli)
- Bilinen sınırlamaları belirt

### Kural 5: Mimari İhlal Yapma
- `Singleton` kullanma → `ServiceLocator` kullan
- `Update()` içinde `GetComponent()` çağırma → `Awake()` içinde cache'le
- Sistemler arası doğrudan referans verme → `EventBus<T>` kullan
- Hardcode değer kullanma → `ScriptableObject` veya `[SerializeField]` kullan
- `MonoBehaviour` dışında test edilebilir mantık yaz

---

## 📋 MEKANİK GELİŞTİRME İŞ AKIŞI (Zorunlu Sıralama)

Her mekanik için aşağıdaki 7 adım sırayla uygulanır:

### Adım 1: 📖 Analiz ve Planlama
- PRD'den ilgili bölümü oku (kabul kriterleri, önkoşullar)
- Mimari rehberden ilgili pattern'i oku
- Bağımlılıkların tamamlanmış olduğunu doğrula

### Adım 2: 🏗️ Interface ve Yapı Tasarımı
- Gerekli interface'leri tanımla (IInteractable, IService vb.)
- Sınıf yapısını belirle (hangi pattern kullanılacak)
- EventBus event struct'larını tanımla
- ScriptableObject config'leri tanımla

### Adım 3: ✍️ Kod Yazımı
- Interface implementasyonlarını yaz
- Core iş mantığını (MonoBehaviour dışı) yaz
- MonoBehaviour bileşenlerini yaz
- ServiceLocator kaydını ekle (gerekiyorsa)
- EventBus publish/subscribe bağlantılarını kur

### Adım 4: 🧪 Test Yazımı
- Her kabul kriteri için en az 1 Unit Test yaz
- Edge case testleri yaz (sınır değerler, null, boş)
- EventBus entegrasyon testi yaz (publish → subscribe doğru çalışıyor mu)
- PlayMode test yaz (runtime davranış gerektiren kriterler için)

### Adım 5: 🔨 Derleme ve Test Çalıştırma
- Projeyi derle — hata 0 olmalı
- EditMode testlerini çalıştır — tümü geçmeli
- PlayMode testlerini çalıştır (varsa) — tümü geçmeli
- Hata varsa düzelt ve tekrar çalıştır

### Adım 6: 🐛 Debug Araçları Ekleme
- Console log'ları ekle (Debug.Log ile state geçişleri, önemli olaylar)
- Gizmo çizimleri ekle (görsel debug: raycast, FOV, algı alanı vb.)
- #if UNITY_EDITOR koruması altında debug kodu yaz
- Gerekirse on-screen debug HUD bileşeni ekle

### Adım 7: 📊 Raporlama ve Kullanıcı Testi
- Kullanıcıya TEST RAPORU ver
- Kullanıcının manuel test sonucunu bekle
- Sorun varsa düzelt, Adım 4'e dön

---

## 🏛️ MİMARİ KURALLAR

### Kod Yapısı
- Tüm global servisler `IService` interface'ini implemente etmeli
- `Awake()` → ServiceLocator.Register, `Start()` → ServiceLocator.Get
- `Awake()` içinde tüm `GetComponent` çağrılarını cache'le
- Her sistem kendi Assembly Definition (.asmdef) altında olmalı

### Klasör Yapısı
Assets/_Project/ altında Scripts/Core, Player, Enemy, Gameplay, Audio, UI, Save, Utils şeklinde organize olmalı.

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

### Minimum Test Kapsamı
Her mekanik için:
- ✅ Her kabul kriteri → en az 1 test
- ✅ Sınır değerler (0, max, negatif)
- ✅ EventBus publish doğrulaması

---

## ⚡ HIZLI KONTROL LİSTESİ (Her Mekanik Sonunda)

- [ ] PRD kabul kriterleri karşılandı mı?
- [ ] Unit testler yazıldı ve geçti mi?
- [ ] Compile hatası yok mu?
- [ ] ServiceLocator kaydı yapıldı mı? (gerekiyorsa)
- [ ] EventBus bağlantıları kuruldu mu?
- [ ] Debug araçları eklendi mi?
- [ ] GetComponent Awake()'te cache'lendi mi?
- [ ] Hardcode değer var mı? (olmamalı)
- [ ] Singleton kullanıldı mı? (kullanılmamalı)
- [ ] Kullanıcıya test raporu verildi mi?

**Tüm kutucuklar ✅ olmadan mekaniği TAMAMLANMIŞ SAYMA.**
