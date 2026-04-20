# QATool

QATool is a Unity-based tool for tracking player behavior, visualizing movement, and collecting in-game feedback during playtesting.

> **Requirements:**
> * Unity project with **TextMeshPro** installed

---

To install QATool, Open the package manager and press "Add from git URL", Then paste https://github.com/mjesp20/QATool to get the package

The package automatiaclly installs dependencies.

Included with the package are two sample scenes, these are basic games to display the functionality of QATool

## Setup & Usage in your own project

1. Add the `QAToolPlayerTracker` component to your player character.
2. Open the **QATool** window in Unity.

The tracker automatically records:
* **Position**
* **Velocity**
* **Camera rotation**
* **Player ID** *(currently not customizable)*
* **Time since start**

---

## Player Paths

QATool records player movement over time, allowing you to visualize each player’s path through the game world. Currently these are coloured randomly (maybe add colours based on values?)

---

## Temporal Trail

Select a specific data file.

The selected player's path is highlighted in **white**.

Step through the recording to observe movement over time.

---

## Heatmaps

QATool can generate **3D heatmaps** based on player data.

### Customization Options
* **Cell Size** – Controls resolution.
* **Contrast** – Adjusts intensity differences.
* **Opacity** – Controls visibility.
* **Percentile Range (0–1)** – Filters data distribution.

### Use heatmaps to analyze:
* Time spent in specific areas.
* Player concentration zones.
* Movement trends.

---

## Feedback Notes

Playtesters can leave in-game feedback tied to a specific time and location.

### How it works
* The developer assigns a **keybind**.
* Pressing the key opens a **feedback window**.
* Players can leave comments (e.g., *“This doesn’t work”*).

### Forced Feedback
You can force a feedback prompt:
1. Add `QAToolQuestionPromptZone` to a GameObject.
2. Attach a **Collider**.
3. When triggered, the feedback window appears with a prompt.

**Example use case:** End-of-level feedback prompts.

---

## Events

You can log custom events using the following syntax:

```csharp
QAToolGlobals.Event(Dictionary<string, object>);
```

### Example
```csharp
QAToolGlobals.Event(
    new System.Collections.Generic.Dictionary<string, object> {
        { "event", $"Took {accumulatedDamage} Damage" }
    }
);
```
*If the dictionary contains a key `"event"` (string), its value will be displayed in the editor.*

---

## Flags

Flags allow you to track custom variables during gameplay.

### Supported Types
* `string`
* `bool`
* `int`
* `float`

### Example Use Cases
* `hasKey`
* `health`
* `collectedItems`

### Creating a Flag
1. Open the **Flags Window**.
2. Enter a **name**.
3. Select a **type**.

### Setting a Flag Value
```csharp
QAToolGlobals.SetFlagValue(string flagName, object value);

// Example
public void TakeDamage() {
    QAToolGlobals.SetFlagValue("Health", currentHealth);
    //Code to take damage
}
```

### Getting a Flag Value
```csharp
QAToolGlobals.GetFlagValue(string flagName);
```

---

## Filters

Filters allow you to analyze subsets of player data based on flag values.

**Example Use Cases:**
* Show heatmaps where `health < 20`.
* View paths after `hasKey` becomes `true`.

---

## Summary

QATool helps developers:
* **Track** player movement and behavior.
* **Visualize** gameplay data (paths, heatmaps).
* **Collect** contextual feedback from playtesters.
* **Log** custom gameplay events.
* **Filter** and analyze player data.