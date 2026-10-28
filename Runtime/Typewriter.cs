using System;
using TMPro;
using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>
    /// Reveals a TMP text one character at a time, pausing longer after commas and sentence ends,
    /// and speaks each character through a <see cref="SpeechVoicePlayer"/>. Rich-text tags are never typed or voiced.
    /// Lines may use the emphasis tags described in <see cref="DialogueMarkup"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Typewriter : MonoBehaviour
    {
        [Tooltip("The text to reveal.")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("Speaks each character. Optional: without it the text types silently.")]
        [SerializeField] private SpeechVoicePlayer _voicePlayer;

        [Tooltip("Voice (sound and pacing) for lines whose dialogue has none. Without a voice, fallback pacing types silently.")]
        [SerializeField] private SpeechVoice _defaultVoice;

        [Tooltip("How many times faster the rest of the line types (pauses included) after the player presses advance mid-line.")]
        [Min(1f)]
        [SerializeField] private float _speedUpMultiplier = 4f;

        [Header("Silent dialogue pacing (seconds)")]
        [Min(0.001f)] [SerializeField] private float _letterDelay = 0.04f;
        [Min(0f)] [SerializeField] private float _spaceDelay = 0.02f;
        [Min(0f)] [SerializeField] private float _commaDelay = 0.18f;
        [Min(0f)] [SerializeField] private float _sentenceEndDelay = 0.4f;

        [Tooltip("Minimum pause after an ellipsis, with or without a voice.")]
        [Min(0f)] [SerializeField] private float _ellipsisDelay = 0.6f;

        private SpeechVoice _voice;
        private int _visibleCount;
        private int _totalCount;
        private float _timer;
        private float _speed = 1f;

        /// <summary>Raised when the whole line is visible, whether it typed out or was completed early.</summary>
        public event Action Finished;

        public bool IsTyping { get; private set; }

        /// <summary>The text being typed into.</summary>
        public TMP_Text Text => _text;

        /// <summary>Starts typing <paramref name="text"/> with <paramref name="voice"/> (or the default voice if null).</summary>
        public void Play(string text, SpeechVoice voice)
        {
            Play(text, voice, DialogueMood.Neutral);
        }

        public void Play(string text, SpeechVoice voice, DialogueMood mood)
        {
            Stop();
            _voice = voice != null ? voice : _defaultVoice;
            var effects = _text.GetComponent<DialogueTextEffects>();
            if (effects == null && (mood != DialogueMood.Neutral || DialogueMarkup.HasAnimatedText(text)))
                effects = _text.gameObject.AddComponent<DialogueTextEffects>();
            if (effects != null) effects.Mood = mood;
            Color emphasis = _voice != null ? _voice.EmphasisColor : DialogueMarkup.DefaultEmphasisColor;
            _text.text = DialogueMarkup.ToRichText(text, emphasis);
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate(ignoreActiveState: true);

            _totalCount = _text.textInfo.characterCount;
            _visibleCount = 0;
            _timer = 0f;
            _speed = 1f;
            IsTyping = true;

            if (_totalCount == 0) Complete();
        }

        /// <summary>Types the rest of the line faster. Every letter is still typed and voiced.</summary>
        public void SpeedUp()
        {
            if (IsTyping) _speed = _speedUpMultiplier;
        }

        /// <summary>Shows the rest of the line at once.</summary>
        public void Complete()
        {
            if (!IsTyping) return;
            _visibleCount = _totalCount;
            _text.maxVisibleCharacters = _totalCount;
            if (_voicePlayer != null) _voicePlayer.Stop();
            Finish();
        }

        /// <summary>Stops typing and silences the voice without raising <see cref="Finished"/>.</summary>
        public void Stop()
        {
            IsTyping = false;
            if (_voicePlayer != null) _voicePlayer.Stop();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void Tick(float deltaTime)
        {
            if (!IsTyping) return;

            _timer -= deltaTime * _speed;
            while (IsTyping && _timer <= 0f) RevealNext();
        }

        private void RevealNext()
        {
            int index = _visibleCount;
            char c = _text.textInfo.characterInfo[index].character;

            _visibleCount++;
            _text.maxVisibleCharacters = _visibleCount;
            if (_voicePlayer != null) _voicePlayer.Speak(c, _voice, IsInQuestionWord(index));

            if (_visibleCount >= _totalCount) Finish();
            else _timer += DelayAfter(index, c);
        }

        private void Finish()
        {
            IsTyping = false;
            Finished?.Invoke();
        }

        private float DelayAfter(int index, char c)
        {
            float letter = Mathf.Max(0.001f, _voice != null ? _voice.LetterDelay : _letterDelay);
            float comma = Mathf.Max(letter, _voice != null ? _voice.CommaDelay : _commaDelay);
            float sentence = Mathf.Max(comma, _voice != null ? _voice.SentenceEndDelay : _sentenceEndDelay);
            if (char.IsWhiteSpace(c)) return Mathf.Max(0f, _voice != null ? _voice.SpaceDelay : _spaceDelay);

            // Consume punctuation clusters before waiting, even when no space follows ("Wait!Really?").
            if (index + 1 < _totalCount && IsPunctuation(_text.textInfo.characterInfo[index + 1].character)
                && IsPunctuation(c)) return letter;
            // Decimal points and thousands separators are not phrase boundaries.
            if ((c == '.' || c == ',') && index > 0 && index + 1 < _totalCount
                && char.IsDigit(_text.textInfo.characterInfo[index - 1].character)
                && char.IsDigit(_text.textInfo.characterInfo[index + 1].character)) return letter;

            // Mixed punctuation and ellipses pause once, on the last mark.
            switch (c)
            {
                case '.':
                    return index > 0 && _text.textInfo.characterInfo[index - 1].character == '.'
                        ? Mathf.Max(sentence, _ellipsisDelay) : sentence;
                case '…':
                    return Mathf.Max(sentence, _ellipsisDelay);
                case '!':
                case '?':
                    return sentence;
                case ',':
                    return comma;
                case ';':
                case ':':
                case '—': // em dash
                case '–':
                    return Mathf.Lerp(comma, sentence, 0.5f);
                default:
                    return letter;
            }
        }

        private static bool IsPunctuation(char c)
        {
            return c == '.' || c == ',' || c == '!' || c == '?' || c == '…'
                || c == ';' || c == ':' || c == '—' || c == '–';
        }

        // True when the word holding this letter runs straight into a '?'.
        private bool IsInQuestionWord(int index)
        {
            TMP_CharacterInfo[] chars = _text.textInfo.characterInfo;
            if (!char.IsLetter(chars[index].character)) return false;

            for (int i = index + 1; i < _totalCount; i++)
            {
                char c = chars[i].character;
                if (char.IsLetter(c) || c == '\'' || c == '’') continue;
                return c == '?';
            }
            return false;
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
