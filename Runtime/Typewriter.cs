using System;
using TMPro;
using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>
    /// Reveals a TMP text one character at a time, pausing longer after commas and sentence ends,
    /// and speaks each character through a <see cref="SpeechVoicePlayer"/>. Rich-text tags are never typed or voiced.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Typewriter : MonoBehaviour
    {
        [Tooltip("The text to reveal.")]
        [SerializeField] private TMP_Text _text;

        [Tooltip("Speaks each character. Optional: without it the text types silently.")]
        [SerializeField] private SpeechVoicePlayer _voicePlayer;

        [Tooltip("Voice (sound and pacing) for lines whose dialogue has none. If this is empty too, lines appear instantly.")]
        [SerializeField] private SpeechVoice _defaultVoice;

        [Tooltip("How many times faster the rest of the line types (pauses included) after the player presses advance mid-line.")]
        [Min(1f)]
        [SerializeField] private float _speedUpMultiplier = 4f;

        private SpeechVoice _voice;
        private int _visibleCount;
        private int _totalCount;
        private float _timer;
        private float _speed = 1f;

        /// <summary>Raised when the whole line is visible, whether it typed out or was completed early.</summary>
        public event Action Finished;

        public bool IsTyping { get; private set; }

        /// <summary>Starts typing <paramref name="text"/> with <paramref name="voice"/> (or the default voice if null).</summary>
        public void Play(string text, SpeechVoice voice)
        {
            _voice = voice != null ? voice : _defaultVoice;
            _text.text = text;
            _text.maxVisibleCharacters = 0;
            _text.ForceMeshUpdate(ignoreActiveState: true);

            _totalCount = _text.textInfo.characterCount;
            _visibleCount = 0;
            _timer = 0f;
            _speed = 1f;
            IsTyping = true;

            if (_voice == null || _totalCount == 0) Complete();
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
            if (!IsTyping) return;

            _timer -= Time.deltaTime * _speed;
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
            if (char.IsWhiteSpace(c)) return _voice.SpaceDelay;

            // Punctuation only pauses where it ends a phrase, so "3.5", "..." and "?!" run on until their last mark.
            bool endsPhrase = EndsPhrase(index + 1);
            switch (c)
            {
                case '.':
                case '!':
                case '?':
                    return endsPhrase ? _voice.SentenceEndDelay : _voice.LetterDelay;
                case ',':
                case ';':
                case ':':
                case '—': // em dash
                    return endsPhrase ? _voice.CommaDelay : _voice.LetterDelay;
                default:
                    return _voice.LetterDelay;
            }
        }

        private bool EndsPhrase(int nextIndex)
        {
            if (nextIndex >= _totalCount) return true;
            char next = _text.textInfo.characterInfo[nextIndex].character;
            return char.IsWhiteSpace(next) || IsClosingMark(next);
        }

        private static bool IsClosingMark(char c)
        {
            return c == '"' || c == '\'' || c == ')' || c == '”' || c == '’';
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
