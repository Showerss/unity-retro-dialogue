# Retro Dialogue (`com.retro.dialogue`)

A typewriter dialogue box with **mumble speech** in the style of Animal Crossing and Undertale. As each letter appears, a short voice sound plays. The text pauses longer after commas and at the end of sentences.

## Install

This repo's root is the package root.

- **Local (editable):** add the following to `Packages/manifest.json`:
  `"com.retro.dialogue": "file:../../unity-retro-dialogue"` (path relative to `Packages/`).
- **Git URL:** Package Manager > + > *Add package from git URL* >
  `https://github.com/Showerss/unity-retro-dialogue.git#v0.2.0`

Requires Unity 6000.0+, Input System, and uGUI (TextMeshPro).

## How the speech works

| Mode | Inspired by | What plays |
|---|---|---|
| `Alphabet` | Animal Crossing ("Animalese") | Each letter plays its own short clip, so you need all 26. |
| `VowelsOnly` | — | Only a, e, i, o, u and y speak; consonants are silent. Softer. |
| `SingleBlip` | Undertale | Every letter plays the same blip, picked at random from the blip clips. |

Shared settings on each `SpeechVoice`:

- **Pitch:** base pitch (below 1 is darker), random jitter per letter, and a rise on the last word before a `?`.
- **Pacing:** the delay after each character is set per voice. Defaults are letter 0.1s, space 0.1s, `, ; : —` 0.3s, and `. ! ?` 0.5s. Punctuation pauses only fire where they end a phrase, so `3.5`, `...` and `?!` run on until the last mark.
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

Tags can wrap each other (`<em><shake>no</shake></em>`), but shake and wave can't be nested inside one another. Standard TextMeshPro tags (`<b>`, `<i>`, `<size=120%>`) work too.

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
