# EventBus API ve Kullanim Kurallari

## Amac

`EventBus<TEvent>` farkli sistemler arasinda gevsek bagli bildirim icin kullanilir. Event payload'lari `struct` olur; bu sayede event nesnesi icin heap allocation olusmaz. EventBus static generic kanallar uzerinden calisir ve singleton kullanmaz.

## API

```csharp
EventBus<TEvent>.Subscribe(Action<TEvent> listener);
EventBus<TEvent>.Unsubscribe(Action<TEvent> listener);
EventBus<TEvent>.Publish(TEvent eventData);
EventBus<TEvent>.HasListeners();
EventBus<TEvent>.ListenerCount;
EventBus<TEvent>.ListenerLimit;
EventBus<TEvent>.Clear();
EventBus.ClearAll();
```

`TEvent` her zaman `struct` olmalidir. `ListenerLimit` varsayilan olarak `EventBus.DefaultListenerLimitPerEvent` degerini kullanir ve kanal basina 32 listener ile sinirlidir.

## Yasam Dongusu

MonoBehaviour dinleyicilerinde subscribe/unsubscribe sirasi:

```csharp
private bool subscribed;

private void OnEnable()
{
    if (subscribed)
    {
        return;
    }

    EventBus<SanityChangedEvent>.Subscribe(OnSanityChanged);
    subscribed = true;
}

private void OnDisable()
{
    Unsubscribe();
}

private void OnDestroy()
{
    Unsubscribe();
}

private void Unsubscribe()
{
    if (!subscribed)
    {
        return;
    }

    EventBus<SanityChangedEvent>.Unsubscribe(OnSanityChanged);
    subscribed = false;
}
```

`OnDisable` sahne veya component kapatma durumlarini yakalar. `OnDestroy` ise memory leak riskine karsi son temizlik katmanidir.

## EventBus Ne Zaman Kullanilir?

EventBus kullan:
- Bir olay birden fazla sistemi ilgilendiriyorsa: `SanityChangedEvent -> Audio, UI, PostProcess`.
- Publisher ve listener birbirini bilmemeliyse: `EnemyDetectedPlayerEvent -> MusicStateManager`.
- Yeni listener eklemek publisher kodunu degistirmemeliyse.

Dogrudan referans kullan:
- Ayni sistem icindeki tekil iletisimlerde.
- Her frame veya cok yuksek frekansli veri akislari icin.
- Sonucun hemen geri donmesi gerekiyorsa.
- Publisher sadece tek bir nesneyle net ve sahipli bir iliski kuruyorsa.

## Performans Kurallari

- Event payload'i `struct` olur.
- Listener sayisi kanal basina varsayilan 32 ile sinirlidir.
- `HasListeners()` pahali event hazirliklarini atlamak icin kullanilir.
- `Publish` icinde lambda olusturma, LINQ ve boxing yapilmaz.
- Bir event surekli 32 listener limitine yaklasiyorsa event cok genis tanimlanmis olabilir; daha dar event kanallarina bolunmelidir.

## Debug Loglama

Event loglari varsayilan olarak kapalidir. Editor veya development build icin gecici olarak acilabilir:

```csharp
EventBus.DebugLoggingEnabled = true;
```

Log formati event tipi ve yayin anindaki listener sayisini gosterir. High-frequency event'lerde loglama kapali tutulmalidir.

## M0-04 Event Sozlesmeleri

Core assembly icinde tanimli ana runtime event'leri:

- `PlayerSanityChangedEvent`
- `EnemyDetectedPlayerEvent`
- `EnemyStateChangedEvent`
- `InteractionEvent`
- `PlayerDiedEvent`
- `FootstepEvent`
- `StaminaLowEvent`
- `StaminaChangedEvent`
- `JumpscareTriggeredEvent`
- `PlayerHidingEvent`
- `PlayerHidingEndEvent`
- `HallucinationSpawnEvent`
- `LightFlickerEvent`
- `AmbientStingerEvent`
- `PuzzleSolvedEvent`
- `SanityChangedEvent`
- `EnemyDetectedSoundEvent`
- `ItemCollectedEvent`
- `DoorStateChangedEvent`
- `SaveLoadEvent`

Player controller tarafinda stamina event adlari PRD ile uyumlu olacak sekilde `StaminaLowEvent` ve `StaminaChangedEvent` olarak kullanilir.
