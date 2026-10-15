using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>How a <see cref="SpeechVoice"/> turns typed letters into sound.</summary>
    public enum SpeechMode
    {
        /// <summary>Animal Crossing: every letter plays its own clip.</summary>
        Alphabet,
        /// <summary>Only a, e, i, o, u and y speak, using their letter clips; consonants are silent.</summary>
        VowelsOnly,
        /// <summary>Undertale: every letter plays the same blip (one of the blip clips at random).</summary>
        SingleBlip,
    }

    /// <summary>
    /// A character's mumble voice: which clips play as the text types, at what pitch, and how fast the text runs.
    /// </summary>
    [CreateAssetMenu(menuName = "Retro/Speech Voice", fileName = "SO_Voice_")]
    public sealed class SpeechVoice : ScriptableObject
    {
        public const int LetterCount = 26;

        [Header("Sound")]
        [Tooltip("Alphabet = Animal Crossing (a clip per letter). VowelsOnly = only vowels speak. SingleBlip = Undertale (one blip on every letter).")]
        [SerializeField] private SpeechMode _mode = SpeechMode.Alphabet;

        [Tooltip("One short clip per letter, A to Z. Used by Alphabet and VowelsOnly.")]
        [SerializeField] private AudioClip[] _letterClips = new AudioClip[LetterCount];

        [Tooltip("Used by SingleBlip; one is picked at random per letter.")]
        [SerializeField] private AudioClip[] _blipClips = new AudioClip[0];

        [Tooltip("Playback volume of each clip.")]
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.6f;

        [Header("Pitch")]
        [Tooltip("1 = the clip as recorded. Lower is deeper and darker (and slightly slower); Animal Crossing villagers sit well above 1.")]
        [Range(0.25f, 3f)]
        [SerializeField] private float _basePitch = 0.8f;

        [Tooltip("Random pitch wobble added to each letter, so repeated letters don't sound mechanical.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pitchJitter = 0.04f;

        [Tooltip("Pitch added to the last word before a '?', so questions rise.")]
        [Range(0f, 1f)]
        [SerializeField] private float _questionRise = 0.1f;

        [Header("Pacing (seconds after each character)")]
        [Tooltip("After a letter, digit or other symbol. Undertale runs about 0.033.")]
        [Min(0f)]
        [SerializeField] private float _letterDelay = 0.1f;

        [Tooltip("After a space or line break.")]
        [Min(0f)]
        [SerializeField] private float _spaceDelay = 0.1f;

        [Tooltip("After , ; : or a dash that ends a phrase.")]
        [Min(0f)]
        [SerializeField] private float _commaDelay = 0.3f;

        [Tooltip("After . ! or ? that ends a sentence.")]
        [Min(0f)]
        [SerializeField] private float _sentenceEndDelay = 0.5f;

        [System.NonSerialized] private bool _warnedMissingClip;

        public SpeechMode Mode => _mode;
        public float Volume => _volume;
        public float BasePitch => _basePitch;
        public float PitchJitter => _pitchJitter;
        public float QuestionRise => _questionRise;
        public float LetterDelay => _letterDelay;
        public float SpaceDelay => _spaceDelay;
        public float CommaDelay => _commaDelay;
        public float SentenceEndDelay => _sentenceEndDelay;

        /// <summary>The clip to play for a typed character, or null if it should be silent.</summary>
        public AudioClip GetClip(char c)
        {
            char lower = char.ToLowerInvariant(c);
            if (lower < 'a' || lower > 'z') return null;

            switch (_mode)
            {
                case SpeechMode.SingleBlip:
                    return _blipClips != null && _blipClips.Length > 0
                        ? _blipClips[Random.Range(0, _blipClips.Length)]
                        : null;

                case SpeechMode.VowelsOnly:
                    if (!IsVowel(lower)) return null;
                    return GetLetterClip(lower);

                default:
                    return GetLetterClip(lower);
            }
        }

        private static bool IsVowel(char lower)
        {
            return lower == 'a' || lower == 'e' || lower == 'i' || lower == 'o' || lower == 'u' || lower == 'y';
        }

        private AudioClip GetLetterClip(char lower)
        {
            int index = lower - 'a';
            AudioClip clip = _letterClips != null && index < _letterClips.Length ? _letterClips[index] : null;
            if (clip == null && !_warnedMissingClip)
            {
                _warnedMissingClip = true;
                Debug.LogWarning($"[SpeechVoice] '{name}' has no clip for '{lower}'; letters without clips are silent.", this);
            }
            return clip;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_letterClips == null || _letterClips.Length != LetterCount)
                System.Array.Resize(ref _letterClips, LetterCount);
            _warnedMissingClip = false;
        }
#endif
    }
}
