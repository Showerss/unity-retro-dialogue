# Retro Dialogue (`com.retro.dialogue`)

A typewriter dialogue box with **mumble speech** in the style of Animal Crossing and Undertale. As each letter appears, a short voice sound plays. The text pauses longer after commas and at the end of sentences.

## Install

This repo's root is the package root.

- **Local (editable):** add the following to `Packages/manifest.json`:
  `"com.retro.dialogue": "file:../../unity-retro-dialogue"` (path relative to `Packages/`).
- **Git URL:** Package Manager > + > *Add package from git URL* >
  `https://github.com/Showerss/unity-retro-dialogue.git#v0.2.0`

Requires Unity 6000.0+, Input System, and uGUI (TextMeshPro).

## 2.5D and 3D compatibility

This module is intended to work in both 2.5D and 3D games through the same dialogue and speech implementation. Camera perspective does not change how `DialogueDefinition`, `DialogueBoxUI`, `Typewriter`, `SpeechVoice` or `SpeechVoicePlayer` work. Reuse the same dialogue assets, voice assets and UI prefab in either view; speech needs no perspective switch.

Keep controller-specific input locking and camera behavior in an `IDialogueHost` adapter:

- **First-person 3D:** use `FpcDialogueHost`, included in this package's optional FirstPersonController adapter.
- **Top-down / 2.5D:** use a host for that controller. The consuming game's `com.topdown.controller` package supplies `TopDownDialogueHost`; it is not bundled with this dialogue package.
- **Other controllers:** implement `IDialogueHost` to provide the advance action, key label and player input lock. Keep camera framing in the controller adapter.

When switching perspective, select or configure the appropriate host at the controller integration boundary. Keep speech, text pacing and mood effects shared rather than adding 2.5D/3D branches to the dialogue core.

### Enabling speech in either view

1. Assign a populated `SpeechVoice` to the dialogue asset's **Voice** field. Alternatively, set **Default Voice** on the UI's `Typewriter` as a fallback for dialogue with no voice.
2. Connect the `Typewriter` to a `SpeechVoicePlayer` with an enabled `AudioSource`. For screen-space dialogue audio, set the source's **Spatial Blend** to **2D** so camera distance does not affect volume.
3. Supply the clips required by the selected speech mode: A-Z for `Alphabet`, vowel clips for `VowelsOnly`, or blip clips for `SingleBlip`. The scene needs an active audio listener.

If both the dialogue voice and typewriter default voice are empty, the line intentionally types silently. Changing perspective does not assign a voice automatically. A voice also defines pacing and speaker styling, so duplicate and tune it when a character needs a distinct presentation.

## How the speech works

| Mode | Inspired by | What plays |
|---|---|---|
| `Alphabet` | Animal Crossing ("Animalese") | Each letter plays its own short clip, so you need all 26. |
| `VowelsOnly` | — | Only a, e, i, o, u and y speak; consonants are silent. Softer. |
| `SingleBlip` | Undertale | Every letter plays the same blip, picked at random from the blip clips. |

Shared settings on each `SpeechVoice`:

- **Pitch:** base pitch (below 1 is darker), random jitter per letter, and a rise on the last word before a `?`.
- **Pacing:** each voice sets letter, space, comma and sentence delays. Semicolons, colons and dashes pause halfway between comma and sentence delays. Ellipses pause for at least 0.6 seconds (adjustable on `Typewriter`). Punctuation clusters pause once, on the last mark; decimal points and numeric separators (`3.5`, `1,000`) keep normal letter timing. Pauses work even without a following space.
- **Silent dialogue:** when neither the dialogue nor the typewriter has a voice, text still types at 0.04 seconds per letter, with 0.02s spaces, 0.18s commas and 0.4s sentence pauses. Adjust these fallback settings on `Typewriter`. Text and mood animation use unscaled time so a dialogue input lock or paused game clock does not stall them.
- **Cut-off:** each new letter cuts off the previous clip, which keeps fast text crisp.

Pressing advance while a line is still typing speeds up the rest of it (4x by default; set **Speed Up Multiplier** on the `Typewriter`). It never skips to the end. Once the line finishes, the next press moves on.

Rich-text tags (`<b>`, `<color>`) are never typed or voiced.

## Speaker colour and emphasis

Each `SpeechVoice` has a **Text Colour** (the speaker's name and lines) and an **Emphasis Colour**, so every character reads at a glance, the way Mouthwashing colour-codes its cast. Lines can mark words:

| Tag | Effect |
|---|---|
| `<em>word</em>` | Speaker's emphasis colour |
| `<shake>word</shake>` | Letters tremble (needs `DialogueTextEffects` on the body text) |
| `<wave>word</wave>` | Letters bob in a ripple (needs `DialogueTextEffects`) |
| `<angry>word</angry>` | Strong, rapid shaking |
| `<happy>word</happy>` | Springy upward bounces |
| `<embarrassed>word</embarrassed>` | Small nervous tremble and a soft pink blush |

Set **Mood** on a `DialogueDefinition` to animate the whole conversation, or wrap individual phrases in mood tags. Text effects are added to the body automatically when a typewriter encounters a mood or animation tag. Both the first-person and top-down dialogue hosts use this same presentation code.

For example: `<happy>You're here!</happy> I, um... <embarrassed>saved you a seat.</embarrassed>`

Another line: `<angry>Don't touch that!</angry> It belongs to me.`

Tags can wrap emphasis (`<em><angry>no</angry></em>`), but animation/mood tags must not nest inside one another or TMP link tags. Standard TextMeshPro tags (`<b>`, `<i>`, `<size=120%>`) work too. Inline mood tags override the conversation mood only for their enclosed characters. Existing dialogue stays neutral until a mood is authored; moods are explicit rather than guessed from wording.

## Setup

1. **Voice:** *Create > Retro > Speech Voice*. Fill the A–Z slots, or the blip slots for `SingleBlip`.
2. **Dialogue:** *Create > Retro > Dialogue*. Set the speaker name and lines, and assign the voice.
3. **Box:** put a `DialogueBoxUI` on a canvas object. Add a `Typewriter` that points at the body `TMP_Text`, a `SpeechVoicePlayer` with its `AudioSource`, and `DialogueTextEffects` on the body text. Then wire the panel, speaker text, typewriter and continue text.
4. **Player:** something must implement `IDialogueHost`. With `com.firstperson.controller`, add `FpcDialogueHost` to the player and `Talkable` to NPCs. The adapter assembly compiles only when that package is present.

## Placeholder voice

*Tools > Retro > Dialogue > Bake Placeholder Voice* synthesizes A–Z plus a blip as WAV files. Each one is a crude formant voice saying the letter's name, at an adjustable low pitch. It can also fill a `SpeechVoice` for you. Existing files are never overwritten unless you tick **Overwrite**, so your own recordings are safe.

## Recording a real Animalese voice

The placeholder sounds robotic. Animal Crossing's charm comes from a real voice, sped up:

1. Record yourself saying the alphabet slowly: "ay, bee, see, dee…"
2. Cut each letter into its own clip and trim it tight, to about 80–120 ms. Keep the start of the vowel and drop any silence.
3. Name them `SFX_Voice_A.wav` … `SFX_Voice_Z.wav` and drop them in, replacing the placeholders.
4. Use **PCM + Decompress On Load** import settings so blips start instantly.
5. Tune `Base Pitch` on the voice. Above 1 sounds like Animal Crossing; below 1 sounds deeper and darker.
