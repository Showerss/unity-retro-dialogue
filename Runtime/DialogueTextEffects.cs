using TMPro;
using UnityEngine;

namespace Retro.Dialogue
{
    public enum DialogueMood { Neutral, Angry, Happy, Embarrassed }

    /// <summary>
    /// Animates mood, <c>&lt;shake&gt;</c> and <c>&lt;wave&gt;</c> spans of a TMP text (see <see cref="DialogueMarkup"/>).
    /// Put it next to the text it animates. It works alongside <see cref="Typewriter"/>: letters only move once they're revealed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DialogueTextEffects : MonoBehaviour
    {
        private enum Effect { None, Shake, Wave, Angry, Happy, Embarrassed }

        [Header("Mood")]
        [SerializeField] private DialogueMood _mood;
        [Range(0f, 0.3f)] [SerializeField] private float _angryAmount = 0.075f;
        [Range(0f, 0.5f)] [SerializeField] private float _happyHeight = 0.14f;
        [Range(0f, 0.15f)] [SerializeField] private float _embarrassedAmount = 0.018f;
        [SerializeField] private Color _blushColor = new Color(1f, 0.65f, 0.75f);

        public DialogueMood Mood { get => _mood; set => _mood = value; }

        [Header("Shake")]
        [Tooltip("How far letters tremble, as a fraction of the font size.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _shakeAmount = 0.05f;

        [Tooltip("How many times per second each letter jumps to a new position.")]
        [Range(1f, 60f)]
        [SerializeField] private float _shakeRate = 18f;

        [Header("Wave")]
        [Tooltip("How high letters bob, as a fraction of the font size.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _waveHeight = 0.12f;

        [Tooltip("How fast the wave moves.")]
        [Range(0f, 20f)]
        [SerializeField] private float _waveSpeed = 6f;

        [Tooltip("Phase offset between neighbouring letters; higher makes a tighter ripple.")]
        [Range(0f, 2f)]
        [SerializeField] private float _waveSpacing = 0.6f;

        private static readonly Effect[] NoEffects = new Effect[0];

        private TMP_Text _text;
        private string _cachedText;
        private Effect[] _linkEffects = NoEffects;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnDisable()
        {
            _cachedText = null;
            if (_text != null) _text.ForceMeshUpdate();
        }

        private void LateUpdate()
        {
            // Animate only lines with a conversation mood or animated spans.
            if (!ReferenceEquals(_text.text, _cachedText)) CacheLinks();
            if (_linkEffects.Length == 0 && _mood == DialogueMood.Neutral) return;

            // Rebuild the mesh from scratch so offsets don't accumulate, then displace the animated letters.
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            int visible = Mathf.Min(_text.maxVisibleCharacters, info.characterCount);
            float time = Time.unscaledTime;
            int shakeTick = Mathf.FloorToInt(time * _shakeRate);
            bool changed = false;

            for (int c = 0; c < visible; c++)
            {
                Effect effect = _mood == DialogueMood.Angry ? Effect.Angry
                    : _mood == DialogueMood.Happy ? Effect.Happy
                    : _mood == DialogueMood.Embarrassed ? Effect.Embarrassed : Effect.None;
                for (int l = 0; l < info.linkCount && l < _linkEffects.Length; l++)
                {
                    TMP_LinkInfo link = info.linkInfo[l];
                    if (_linkEffects[l] != Effect.None && c >= link.linkTextfirstCharacterIndex
                        && c < link.linkTextfirstCharacterIndex + link.linkTextLength)
                    {
                        effect = _linkEffects[l];
                        break;
                    }
                }
                if (effect == Effect.None) continue;
                TMP_CharacterInfo character = info.characterInfo[c];
                if (!character.isVisible) continue;
                float size = character.pointSize;
                Vector3 offset;
                switch (effect)
                {
                    case Effect.Angry:
                        offset = new Vector3(Noise(c * 2, shakeTick), Noise(c * 2 + 1, shakeTick), 0f) * (_angryAmount * size);
                        break;
                    case Effect.Happy:
                        offset = new Vector3(0f, Mathf.Max(0f, Mathf.Sin(time * 9f - c * 0.65f)) * _happyHeight * size, 0f);
                        break;
                    case Effect.Embarrassed:
                        offset = new Vector3(Mathf.Sin(time * 14f + c * 1.7f), Mathf.Sin(time * 3f - c * 0.5f) * 0.4f, 0f) * (_embarrassedAmount * size);
                        break;
                    case Effect.Shake:
                        offset = new Vector3(Noise(c * 2, shakeTick), Noise(c * 2 + 1, shakeTick), 0f) * (_shakeAmount * size);
                        break;
                    default:
                        offset = new Vector3(0f, Mathf.Sin(time * _waveSpeed - c * _waveSpacing) * _waveHeight * size, 0f);
                        break;
                }

                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                int v = character.vertexIndex;
                vertices[v] += offset;
                vertices[v + 1] += offset;
                vertices[v + 2] += offset;
                vertices[v + 3] += offset;
                if (effect == Effect.Embarrassed)
                {
                    Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
                    float blush = 0.3f + Mathf.Sin(time * 2f) * 0.1f;
                    for (int i = 0; i < 4; i++)
                    {
                        Color original = colors[v + i];
                        Color tinted = Color.Lerp(original, _blushColor, blush);
                        tinted.a = original.a;
                        colors[v + i] = tinted;
                    }
                }
                changed = true;
            }

            if (changed) _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        // Runs once per line, not per frame.
        private void CacheLinks()
        {
            _cachedText = _text.text;
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;

            bool any = false;
            var effects = new Effect[info.linkCount];
            for (int l = 0; l < info.linkCount; l++)
            {
                string id = info.linkInfo[l].GetLinkID();
                effects[l] = id == DialogueMarkup.ShakeLinkId ? Effect.Shake
                    : id == DialogueMarkup.WaveLinkId ? Effect.Wave
                    : id == DialogueMarkup.AngryLinkId ? Effect.Angry
                    : id == DialogueMarkup.HappyLinkId ? Effect.Happy
                    : id == DialogueMarkup.EmbarrassedLinkId ? Effect.Embarrassed
                    : Effect.None;
                any |= effects[l] != Effect.None;
            }
            _linkEffects = any ? effects : NoEffects;
        }

        // Cheap repeatable hash noise in [-1, 1], so a letter holds still between shake ticks.
        private static float Noise(int seed, int tick)
        {
            int n = seed * 374761393 + tick * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            n ^= n >> 16;
            return (n & 0xFFFF) / 32767.5f - 1f;
        }
    }
}
