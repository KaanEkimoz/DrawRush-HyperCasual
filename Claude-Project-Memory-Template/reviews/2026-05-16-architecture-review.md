# DrawRush — Architecture / Clean-Code Review

Reviewer hat: Senior C# engineer, no Unity bias. Files referenced are under
`/Users/kaanusta/Unity Projects/DrawRush/Assets/_Project/`.

## SOLID violations (with file:line)

- **SRP — `GameManager.cs`** does (a) scene flow / level selection
  (`PlayerPrefs`, `SceneManager.LoadScene`), (b) win/lose UI panel toggling, and
  (c) post-win cleanup (`FindObjectsOfType<EnemyCombat>`, kill trails, destroy
  `LineRenderer`s, fire particles). Three reasons to change ⇒ split.
- **SRP — `DrawPart.cs`**: it is simultaneously the *part data*, the *trail-host
  lifecycle owner*, and the *trail-catch-up animator* (`LerpTrailTowardsPlayer`,
  Update). Three responsibilities in one MonoBehaviour.
- **OCP — `EnemyCombat.OnTriggerEnter`** checks `GameServices.State.IsGameWon`
  inline. Every new "stop the world" state (pause, cutscene) requires editing
  this method. Should subscribe to a single "combat allowed" gate.
- **ISP — `DrawPart`** exposes both `IInteractable.Interact()` (the contract
  consumers use) **and** a public `MarkCompleted()`, **and** a public field
  `isPlayerEntered`. Callers see three doors when only one was intended.
- **DIP — `GameManager` (Core) → `EnemyCombat` (Enemy)**: Core, the lowest
  layer, takes a hard dependency on a higher feature module. Direction is
  inverted. Same problem for `GameBootstrap` → `PlayerHealth`.
- **LSP — `PlayerHealth.Apply(int delta)`**: contract is "applies any signed
  delta", but the body has a guard `if (_current <= 0) return;` that silently
  drops heals after death. A consumer calling `Apply(+1)` cannot predict
  behaviour from the signature.

## Coupling smells & dependency direction issues

- **`GameServices` is a public ambient global.** Every read site (`EnemyFollow`,
  `EnemyCombat`, `ThirdPersonMovement`, `DrawPart`) hard-binds to a static.
  This is a singleton wearing a different hat — it satisfies the *letter* of
  rules.md but not the spirit.
- **`Drawing → Player`** (`DrawPart` imports `PlayerInteract`) is *peer-to-peer*
  coupling that should go through an abstraction. `DrawPart` only needs "a
  trail attach transform" — not a `PlayerInteract` reference. Drop the field;
  read `GameServices.TrailPoint` (already done) and delete `_playerInteract`.
- **`Core → Enemy`** in `GameManager` is the worst offender. `EnemyCombat`
  should subscribe to `GameState.GameWonChanged` itself and play the die
  trigger. Same for the LineRenderer/trail cleanup — owners should clean
  themselves up.
- **Public-field write across types**: `PlayerInteract` writes to
  `DrawPart.isPlayerEntered` (`[HideInInspector] public bool`). Field is
  serialized-visible but written from another file → encapsulation breach.
  `DrawPart` should own the value, set internally inside `OnTriggerEnter`.
- **`PlayerInteract.OnTriggerEnter`** reaches into `other.gameObject.GetComponent<DrawPart>()`
  to call `MarkCompleted()` on both parts and `AddComponent<LineRenderer>` on
  the *previous part*. That makes the previous DrawPart visually responsible
  for the line — bizarre. The line is an artefact of the *connection*, not the
  part; spawn it on a dedicated `Connection` GameObject managed by
  `PlayerInteract`.
- **`WallManager`** is a scene-scoped singleton-ish coordinator using
  `Object.FindObjectsByType<DrawPart>()`. Order-of-Awake risk: a `DrawPart`
  spawned after Awake will never be tracked.

## API design recommendations (with proposed signatures)

```csharp
// Drawing
public interface IDrawPart {
    event Action<IDrawPart> Completed;
    bool IsCompleted { get; }
    void OnPlayerEntered();   // replaces public isPlayerEntered write
    void OnPlayerExited();
    void Complete();          // replaces MarkCompleted; idempotent
}
```

- `DrawPart` implements `IDrawPart` + `IInteractable`. `PlayerInteract` depends
  on `IDrawPart`, not the concrete class. Hides the four private booleans and
  the public field entirely.
- **`PlayerHealth`** — replace the signed-delta API:
  ```csharp
  public void TakeDamage(int amount);   // amount > 0 enforced
  public void Heal(int amount);         // amount > 0 enforced; ignored if !IsAlive
  ```
  `GameConfig.enemyTouchDamage` becomes a positive `int` ("damage on touch =
  1"), removing the sign-as-direction footgun.
- **`GameServices`** — keep the static, but make it explicit and minimal:
  ```csharp
  public static class GameServices {
      public static IPlayerRefs Player { get; }   // Transform + TrailPoint
      public static Transform   MainCamera { get; }
      public static GameConfig  Config { get; }
      public static GameState   State { get; }
      public static void Register(in Registration r);
      public static void Clear();
  }
  public readonly struct Registration { public Transform Player; ... }
  ```
  Bundle the 5-arg `Register` into a `Registration` struct (named fields beat
  positional). The "GetPlayer()/GetCamera()" alternative buys nothing: they
  are still globals, just with parentheses.

## State machine refactor proposal (drawing mechanic)

DrawPart's four booleans encode this:

```
Idle ──player enters──▶ Armed
Armed ──Interact()─────▶ TrailAttached (spawns _currTrail)
TrailAttached ──MarkCompleted──▶ ReturningToPlayer (Lerp in Update)
ReturningToPlayer ──reached───▶ Done (destroy trail; raise Completed)
```

Encode it once:

```csharp
public enum DrawingPhase { Idle, Armed, TrailAttached, Returning, Done }

internal sealed class DrawPartStateMachine {
    public DrawingPhase Phase { get; private set; }
    public event Action<DrawingPhase, DrawingPhase> Transitioned;
    public bool TryTransition(DrawingPhase next) { /* whitelist */ }
}
```

`DrawPart` owns one of these; `Update` reads `Phase`. `PlayerInteract`'s own
implicit state machine (`isDrawing`, `_canDraw`, `_previousPart`) becomes:

```csharp
private enum InteractPhase { OutsideArea, InsideArea_NoAnchor, InsideArea_OneAnchor }
```

with explicit transitions on `OnTriggerEnter`/`OnTriggerExit`. The pure
transition table is unit-testable without a Unity runtime.

## Testability gaps

- `DrawPart.Update` mixes `Time.deltaTime`, `Mathf.Lerp` and a static
  (`GameServices.TrailPoint`). Extract a pure function:
  ```csharp
  internal static Vector3 LerpTrail(Vector3 current, Vector3 target, float lerp, float dt);
  ```
  Now testable with 5 lines of NUnit.
- `PlayerInteract.OnTriggerEnter` is a 30-line decision tree fused with
  `Destroy()`/`AddComponent<LineRenderer>()` calls. Extract:
  ```csharp
  internal static InteractDecision Decide(InteractInput input);  // pure
  ```
  where `InteractDecision` is `enum { Ignore, ArmAnchor, CompleteConnection }`.
  The Unity side becomes a thin dispatcher.
- `GameManager.Update()` polls `player == null` to detect death. That is a
  side-channel; subscribe to `PlayerHealth.Died` instead — then GameManager has
  no Update at all and the lose-flow becomes deterministic in tests.
- `WallManager` uses `FindObjectsByType` at Awake — untestable headless.
  Inject the part list via a `[SerializeField] DrawPart[] parts` (designer-set)
  or via a registration call from each DrawPart's `OnEnable`.

## Naming corrections

| Current | Proposed | Reason |
|---|---|---|
| `PartManager` | `PartGroupGate` (or `WallReveal`) | It does not "manage"; it gates one wall on a part group. |
| `WallManager` | `WinCondition` (or `LevelCompletionWatcher`) | No walls are managed here; it watches all parts and flips `IsGameWon`. The name `WallManager` is a lie inherited from when it toggled walls. |
| `GameManager` | Split into `LevelFlow`, `HudPanels`, `WinSequenceDirector` | Three concerns, three classes. |
| `DrawPart.MarkCompleted` | `Complete` | "Mark" implies a flag flip; the method also raises events and starts return-to-player. The verb should describe the *whole* transition. |
| `PlayerHealth.Apply` | `TakeDamage` / `Heal` | Sign-as-direction is a magic affordance. Two named methods document intent. |
| `DrawPart.isPlayerEntered` | private `_isArmed` | "Player entered" is a transient input event; the *state* is "armed". |
| `PlayerInteract.trail` | private `_activeTrail` | Field is `[HideInInspector] public` — there's no reason for outside writers. |
| `DontDestroyOnLoad` (class) | `PersistentObject` | Class shadows `UnityEngine.Object.DontDestroyOnLoad` — confusing every reader. The README admits "Class name kept to preserve scene refs"; rename + use `[FormerlySerializedAs]`. |
| `CreateJoystick` | `JoystickSpawner` | Class names are nouns, not verbs. |

## Classes that probably should not exist

- **`WallManager`** as currently written. Its sole job is "raise `IsGameWon`
  when all parts complete". That belongs in a single `WinCondition` SO that
  parts *register* with on enable. Delete the scene-scoped MonoBehaviour and
  the `FindObjectsByType` call goes with it.
- **`PartManager`** could be one `DrawPartGroup<T>` helper shared with
  `WallManager` (see Code Reuse below).
- **`DontDestroyOnLoad`** the class — the dedup-by-stable-id pattern is a
  workaround for a missing persistent-root scene. With a proper boot scene the
  class is unnecessary; mark a single root with the Unity API call once.

## Code reuse

- `PartManager` and `WallManager` differ only in (a) where they get their part
  list, and (b) what fires when the count is reached. Extract:
  ```csharp
  internal sealed class DrawPartCompletionWatcher {
      public DrawPartCompletionWatcher(IReadOnlyList<DrawPart> parts);
      public event Action AllCompleted;
      public void Enable(); public void Disable();
  }
  ```
  PartManager passes children + `SetActive(wall, true)` callback; WallManager
  passes scene list + `state.IsGameWon = true` callback. ~30 lines deleted.
- `GameBootstrap.ResolveSceneReferences` runs the same "field-or-tag-fallback"
  pattern three times. Extract `Transform Resolve(Transform inspectorRef, string tag)`.

## Visibility / sealed

- `sealed` everywhere is correct. Keep it.
- Move every `[HideInInspector] public` field to `private` (`[SerializeField]`
  only when designer-set). Affected: `DrawPart.isPlayerEntered`,
  `PlayerInteract.isDrawing`, `PlayerInteract.trail`, `DontDestroyOnLoad.objectID`.
- `GameState.IsGameWon`'s **setter is public** — anyone can flip the win flag.
  Make it `internal set` (or `private set` + a `MarkWon()` method) so only
  `WallManager`/`WinCondition` can write it.

## XML doc coverage

Coverage is decent on Core classes, sparse elsewhere. Public APIs missing
`<summary>`:

- `DrawPart.MarkCompleted`, `DrawPart.Completed` event, `IsDrawCompleted`
- `PlayerHealth.Apply` (also misleading: doesn't document the "drops heal
  after death" guard)
- `GameServices.Register` (param list undocumented; teammates have to read
  the body to know null is permitted for `trailPoint`/`mainCamera`)
- `VoidEventChannel.Raise` / `IntEventChannel.Raise`

## Ordered refactor sequence (keeps 17 EditMode tests green)

1. **Rename `Apply` → split into `TakeDamage(int)` + `Heal(int)`** on
   `PlayerHealth`. Update one test (`Apply_NegativeDelta_ReducesCurrent` →
   `TakeDamage_ReducesCurrent`). Add `[Obsolete] Apply` shim if you want zero
   test-file edits. Run tests.
2. **Introduce `IDrawPart` interface**, make `DrawPart` implement it, change
   `PlayerInteract` to depend on the interface. Make `DrawPart.MarkCompleted`
   `internal` and rename to `Complete`. No test changes (tests only use
   `DrawPart.Completed` event). Run tests.
3. **Encapsulate `DrawPart.isPlayerEntered`**: make private, add
   `OnPlayerEntered()`/`OnPlayerExited()` methods that `PlayerInteract` calls
   instead of writing the field. Run tests.
4. **Extract `LerpTrail` pure helper** in a static class `TrailMath`. Add 2-3
   new unit tests for it (now you have 19-20). Replace the body of
   `LerpTrailTowardsPlayer`. Run tests.
5. **Extract `DrawPartCompletionWatcher`** helper; refactor `PartManager` and
   `WallManager` to delegate. Existing PartManager tests still pass because
   the public `Completed` event behaviour is unchanged. Run tests.
6. **Invert the `GameManager → EnemyCombat` dependency**: make `EnemyCombat`
   subscribe to `GameState.GameWonChanged` and play `t_die` itself. Delete
   the `using Studios208.DrawRush.Enemy;` line in `GameManager`. Run tests.
7. **Split `GameManager`** into `LevelFlow` (scene loads + PlayerPrefs),
   `HudPanels` (win/lose panels), and `WinSequenceDirector` (cleanup +
   particles + delayed panel). Move responsibilities one at a time; run
   tests between each move.
8. **Introduce explicit `DrawingPhase` enum + state-machine struct** inside
   `DrawPart`. Convert the four booleans to a single field. Add 2-3 unit
   tests for the transition table. Run tests — 17 originals untouched.

If only 5 steps are budgeted: do 1, 2, 3, 6, 8. The rest is polish.
