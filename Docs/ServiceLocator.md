# ServiceLocator API ve Uygulama Plani

## Amac

`ServiceLocator`, singleton kullanmadan sahne ve oyun genelindeki servisleri interface arkasindan erisilebilir yapar. Servis sahipleri `Awake()` icinde kayit olur, servis tuketicileri `Start()` icinde cozer. Bu siralama tum `Awake()` cagirilari tamamlandiktan sonra `Start()` basladigi icin Unity yasam dongusuyle uyumludur.

## API

```csharp
ServiceLocator.Register<IAudioManager>(audioManager);
ServiceLocator.Unregister<IAudioManager>(audioManager);
IAudioManager audio = ServiceLocator.Get<IAudioManager>();
bool found = ServiceLocator.TryGet(out IAudioManager optionalAudio);
bool registered = ServiceLocator.IsRegistered<IAudioManager>();
ServiceLocator.Clear();
```

- `Register<T>(T service)`: Sadece `IService` turevi interface contract kabul eder. Ayni contract ikinci kez kaydedilirse exception firlatir.
- `Register<T>(T service, bool overwrite)`: Test veya kontrollu replacement durumlarinda `overwrite: true` ile mevcut kaydi degistirir.
- `Unregister<T>()` / `Unregister<T>(service)`: Kaydi kaldirir. Instance verilirse sadece ayni instance kayitliyken siler.
- `Get<T>()`: Zorunlu servis cozumleme API'sidir. Kayit yoksa hangi servisin eksik oldugunu ve `Awake`/`Start` sirasini anlatan exception firlatir.
- `TryGet<T>(out T service)`: Opsiyonel servislerde exception firlatmadan `false` doner.
- `IsRegistered<T>()`: Kayit durumunu kontrol eder.
- `Clear()`: Test teardown ve sahne gecisi stratejilerinde tum kayitlari temizler.

## Editor Listeleme

Unity Editor menusu:

`FPS Horror Game > Services > List Registered Services`

Bu menu mevcut kayitlari Console'a `IContract -> Implementation` formatinda yazar. Runtime build'lere dahil edilmez.

## Execution Order

Kod tabanli execution-order slotlari `ServiceExecutionOrder` icinde tanimlidir:

- `CoreServices = -1000`: `AddressableSceneLoader`, `ObjectPoolManager`
- `FeatureServices = -900`: `AudioManager`, ileride `SaveSystem`, `SanitySystem`
- `PlayerServices = -800`: `PlayerController`
- `ServiceConsumers = 0`: UI, gameplay ve ornek consumer scriptleri

Servis provider'lar `Awake()` icinde register eder. Consumer scriptler `Start()` icinde `Get` veya `TryGet` kullanir. Bu nedenle provider `Awake()` kayitlari, consumer `Start()` cozumlemelerinden once tamamlanir.

## Kullanim Ornekleri

```csharp
private void Awake()
{
    ServiceLocator.Register<IAudioManager>(this);
}

private void Start()
{
    audioManager = ServiceLocator.Get<IAudioManager>();
    audioManager.PlaySFX("door_creak", transform.position);
}

private void OnDestroy()
{
    ServiceLocator.Unregister<IAudioManager>(this);
}
```

Opsiyonel servis:

```csharp
if (ServiceLocator.TryGet(out IAudioManager audio))
{
    audio.PlaySFX("hint", transform.position);
}
```

Testte mock servis:

```csharp
[SetUp]
public void SetUp()
{
    ServiceLocator.Clear();
    ServiceLocator.Register<IAudioManager>(new MockAudioManager());
}

[TearDown]
public void TearDown()
{
    ServiceLocator.Clear();
}
```

## Test Stratejisi

EditMode testleri `Register`, `Get`, `TryGet`, `Unregister`, `Clear`, duplicate kayit, concrete contract reddi ve mock servis kaydini kapsar. Her test `SetUp` ve `TearDown` icinde `ServiceLocator.Clear()` cagirarak kayit sizmasini engeller.
