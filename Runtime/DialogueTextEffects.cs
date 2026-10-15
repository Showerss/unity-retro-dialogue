using TMPro;
using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>
    /// Animates the <c>&lt;shake&gt;</c> and <c>&lt;wave&gt;</c> words of a TMP text (see <see cref="DialogueMarkup"/>).
    /// Put it next to the text it animates. It works alongside <see cref="Typewriter"/>: letters only move once they're revealed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DialogueTextEffects : MonoBehaviour
    {
        private enum Effect { None, Shake, Wave }

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
        }

        private void LateUpdate()
        {
            // Nothing to animate unless the current line has shake or wave words.
            if (!ReferenceEquals(_text.text, _cachedText)) CacheLinks();
            if (_linkEffects.Length == 0) return;

            // Rebuild the mesh from scratch so offsets don't accumulate, then displace the animated letters.
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            int visible = Mathf.Min(_text.maxVisibleCharacters, info.characterCount);
            float size = _text.fontSize;
            float time = Time.time;
            int shakeTick = Mathf.FloorToInt(time * _shakeRate);
            bool changed = false;

            for (int l = 0; l < info.linkCount && l < _linkEffects.Length; l++)
            {
                Effect effect = _linkEffects[l];
                if (effect == Effect.None) continue;

                TMP_LinkInfo link = info.linkInfo[l];
                int end = Mathf.Min(link.linkTextfirstCharacterIndex + link.linkTextLength, visible);
                for (int c = link.linkTextfirstCharacterIndex; c < end; c++)
                {
                    TMP_CharacterInfo character = info.characterInfo[c];
                    if (!character.isVisible) continue;

                    Vector3 offset = effect == Effect.Shake
                        ? new Vector3(Noise(c * 2, shakeTick), Noise(c * 2 + 1, shakeTick), 0f) * (_shakeAmount * size)
                        : new Vector3(0f, Mathf.Sin(time * _waveSpeed - c * _waveSpacing) * _waveHeight * size, 0f);

                    Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                    int v = character.vertexIndex;
                    vertices[v] += offset;
                    vertices[v + 1] += offset;
                    vertices[v + 2] += offset;
                    vertices[v + 3] += offset;
                    changed = true;
                }
            }

            if (changed) _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
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
