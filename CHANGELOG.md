# Changelog

## [0.3.0] - 2026-10-16
### Added
- `WorldBoundsDialogueWarning` (FirstPersonController adapter): shows a line on the black screen after the controller's `WorldBoundsWarden` returns the player to the path.

## [0.2.0] - 2026-10-14
### Added
- Per-speaker text colour on `SpeechVoice`. `DialogueBoxUI` tints the speaker name and line with it (each can be turned off), as Mouthwashing colour-codes each character.
- Emphasis tags in lines (`DialogueMarkup`): `<em>` (the speaker's emphasis colour), `<shake>` and `<wave>`.
- `DialogueTextEffects`: animates `<shake>`/`<wave>` letters as they're revealed.
- `Typewriter.Text` exposes the TMP text it types into.

## [0.1.1] - 2026-10-14
### Changed
- Pressing advance while a line is typing now **speeds it up** (Typewriter `Speed Up Multiplier`, default 4x, pauses included) instead of jumping to the end. Every letter is still typed and voiced. `Typewriter.Complete()` is still there for code that needs an instant reveal.

## [0.1.0] - 2026-10-14
### Added
- `DialogueDefinition`, `DialogueBoxUI`: one-line-at-a-time dialogue box. Moved from the 90s retro shader game; script GUIDs are the same.
- `Typewriter`: types text letter by letter, with longer pauses after commas and sentence ends.
- `SpeechVoice` + `SpeechVoicePlayer`: mumble speech in three modes. Alphabet (Animal Crossing), VowelsOnly, and SingleBlip (Undertale).
- `IDialogueHost`, with the First Person Controller adapter (`FpcDialogueHost`, `Talkable`).
- Editor: a labelled A–Z voice inspector and a placeholder voice generator (Tools > Retro > Dialogue > Bake Placeholder Voice).
