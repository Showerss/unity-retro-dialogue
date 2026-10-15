using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Retro.Dialogue.Editor
{
    /// <summary>
    /// Synthesizes a placeholder Animalese voice: one short clip per letter (A-Z) plus an Undertale-style blip.
    /// Each letter is a crude formant-synth of its English letter name ("bee", "ef", "kay"), at a low pitch.
    /// They sound robotic; record your own alphabet for the real thing (see README).
    /// </summary>
    public sealed class LetterClipBaker : EditorWindow
    {
        public const string BlipName = "Blip";

        const int SampleRate = 22050;
        const float VowelSeconds = 0.065f;

        [SerializeField] private string _folder = "Assets/Audio/Voice";
        [SerializeField] private string _prefix = "SFX_Voice_";
        [SerializeField] private float _fundamentalHz = 110f;
        [SerializeField] private bool _overwrite;
        [SerializeField] private SpeechVoice _voice;

        [MenuItem("Tools/Retro/Dialogue/Bake Placeholder Voice")]
        private static void Open()
        {
            GetWindow<LetterClipBaker>("Placeholder Voice");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Writes A-Z letter clips and a blip as WAV files. Existing files are kept unless Overwrite is on, so clips you recorded are safe.", MessageType.Info);
            _folder = EditorGUILayout.TextField(new GUIContent("Folder", "Project folder under Assets/ to write the clips into."), _folder);
            _prefix = EditorGUILayout.TextField(new GUIContent("File Prefix", "Clips are named <prefix>A.wav ... <prefix>Z.wav, <prefix>Blip.wav."), _prefix);
            _fundamentalHz = EditorGUILayout.Slider(new GUIContent("Voice Pitch (Hz)", "Lower is deeper. ~110 is a low male voice; Animal Crossing sits far higher."), _fundamentalHz, 60f, 300f);
            _overwrite = EditorGUILayout.Toggle(new GUIContent("Overwrite", "Replace clips that already exist."), _overwrite);
            _voice = (SpeechVoice)EditorGUILayout.ObjectField(new GUIContent("Assign To Voice", "Optional. Fills this voice's letter and blip slots with the clips."), _voice, typeof(SpeechVoice), false);

            if (GUILayout.Button("Bake"))
            {
                int written = Bake(_folder, _prefix, _fundamentalHz, _overwrite);
                if (_voice != null) AssignClips(_voice, _folder, _prefix);
                Debug.Log($"[LetterClipBaker] Wrote {written} clip(s) to {_folder}.");
            }
        }

        /// <summary>Writes the placeholder clips; returns how many files were written.</summary>
        public static int Bake(string folder, string prefix, float fundamentalHz, bool overwrite)
        {
            if (!folder.StartsWith("Assets", StringComparison.Ordinal))
                throw new ArgumentException("Folder must be inside Assets/.", nameof(folder));

            string fullFolder = Path.GetFullPath(folder);
            Directory.CreateDirectory(fullFolder);

            int written = 0;
            for (char letter = 'A'; letter <= 'Z'; letter++)
            {
                if (WriteIfNeeded(fullFolder, prefix + letter, overwrite, () => SynthLetter(char.ToLowerInvariant(letter), fundamentalHz)))
                    written++;
            }
            if (WriteIfNeeded(fullFolder, prefix + BlipName, overwrite, () => SynthBlip(fundamentalHz)))
                written++;

            AssetDatabase.Refresh();
            ConfigureImporters(folder, prefix);
            return written;
        }

        /// <summary>Fills a voice's letter and blip slots from clips named by <paramref name="prefix"/> in <paramref name="folder"/>.</summary>
        public static void AssignClips(SpeechVoice voice, string folder, string prefix)
        {
            var serialized = new SerializedObject(voice);
            SerializedProperty letters = serialized.FindProperty("_letterClips");
            letters.arraySize = SpeechVoice.LetterCount;
            for (int i = 0; i < SpeechVoice.LetterCount; i++)
                letters.GetArrayElementAtIndex(i).objectReferenceValue = LoadClip(folder, prefix + (char)('A' + i));

            AudioClip blip = LoadClip(folder, prefix + BlipName);
            SerializedProperty blips = serialized.FindProperty("_blipClips");
            blips.arraySize = blip != null ? 1 : 0;
            if (blip != null) blips.GetArrayElementAtIndex(0).objectReferenceValue = blip;

            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(voice);
        }

        private static AudioClip LoadClip(string folder, string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}/{fileName}.wav");
        }

        private static bool WriteIfNeeded(string fullFolder, string fileName, bool overwrite, Func<float[]> synth)
        {
            string path = Path.Combine(fullFolder, fileName + ".wav");
            if (File.Exists(path) && !overwrite) return false;
            WriteWav(path, synth());
            return true;
        }

        // Short blips must start instantly: uncompressed and preloaded.
        private static void ConfigureImporters(string folder, string prefix)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{prefix} t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                if (settings.compressionFormat == AudioCompressionFormat.PCM && settings.loadType == AudioClipLoadType.DecompressOnLoad && importer.forceToMono)
                    continue;

                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        // ── Synthesis ─────────────────────────────────────────────────────────────

        // Vowel formants (F1, F2) from textbook averages.
        static readonly Vector2 Ah = new Vector2(730, 1090);
        static readonly Vector2 Eh = new Vector2(530, 1840);
        static readonly Vector2 Ee = new Vector2(270, 2290);
        static readonly Vector2 Oh = new Vector2(570, 840);
        static readonly Vector2 Oo = new Vector2(300, 870);
        // Consonant targets for glides, nasals and liquids.
        static readonly Vector2 W = new Vector2(300, 610);
        static readonly Vector2 Labial = new Vector2(250, 1000);
        static readonly Vector2 Alveolar = new Vector2(250, 1700);
        static readonly Vector2 Velar = new Vector2(250, 2000);
        static readonly Vector2 M = new Vector2(250, 1100);
        static readonly Vector2 N = new Vector2(250, 1700);
        static readonly Vector2 L = new Vector2(360, 1000);
        static readonly Vector2 R = new Vector2(490, 1350);

        private static float[] SynthLetter(char letter, float f0)
        {
            var s = new Synth(f0, seed: letter);
            const float V = VowelSeconds;
            switch (letter)
            {
                case 'a': s.Voiced(V, Eh, Ee); break;                                          // "ay"
                case 'b': s.Burst(0.008f, 1200, 0.3f); s.Voiced(0.02f, Labial, Ee, 0.3f, 1f); s.Voiced(V, Ee); break;
                case 'c': s.Hiss(0.05f, 4500, 2f, 0.2f, 0.35f); s.Voiced(V, Ee); break;           // "see"
                case 'd': s.Burst(0.008f, 2500, 0.3f); s.Voiced(0.02f, Alveolar, Ee, 0.3f, 1f); s.Voiced(V, Ee); break;
                case 'e': s.Voiced(V, Ee); break;
                case 'f': s.Voiced(V * 0.8f, Eh); s.Hiss(0.05f, 3000, 0.7f, 0.25f, 0.1f); break;   // "ef"
                case 'g': s.Hiss(0.02f, 2300, 1.5f, 0.25f, 0.1f); s.Voiced(0.02f, Velar, Ee, 0.3f, 1f); s.Voiced(V, Ee); break; // "jee"
                case 'h': s.Voiced(V * 0.8f, Eh, Ee); s.Hiss(0.05f, 2500, 2f, 0.3f, 0.1f); break;  // "aitch"
                case 'i': s.Voiced(V, Ah, Ee); break;                                          // "eye"
                case 'j': s.Hiss(0.02f, 2300, 1.5f, 0.25f, 0.1f); s.Voiced(V, Eh, Ee); break;      // "jay"
                case 'k': s.Burst(0.012f, 1800, 0.5f); s.Hiss(0.015f, 1500, 0.8f, 0.15f, 0.05f); s.Voiced(V, Eh, Ee); break; // "kay"
                case 'l': s.Voiced(V * 0.8f, Eh); s.Voiced(0.04f, Eh, L, 1f, 0.6f); break;         // "el"
                case 'm': s.Voiced(V * 0.8f, Eh); s.Voiced(0.04f, Eh, M, 1f, 0.4f); break;         // "em"
                case 'n': s.Voiced(V * 0.8f, Eh); s.Voiced(0.04f, Eh, N, 1f, 0.4f); break;         // "en"
                case 'o': s.Voiced(V, Oh, Oo); break;
                case 'p': s.Burst(0.012f, 900, 0.5f); s.Hiss(0.015f, 1500, 0.8f, 0.15f, 0.05f); s.Voiced(V, Ee); break;
                case 'q': s.Burst(0.012f, 1800, 0.5f); s.Voiced(0.03f, Ee, Oo); s.Voiced(V, Oo); break; // "cue"
                case 'r': s.Voiced(V * 0.8f, Ah); s.Voiced(0.04f, Ah, R, 1f, 0.6f); break;         // "ar"
                case 's': s.Voiced(V * 0.8f, Eh); s.Hiss(0.06f, 4500, 2f, 0.3f, 0.1f); break;      // "es"
                case 't': s.Burst(0.012f, 3000, 0.5f); s.Hiss(0.015f, 2000, 0.8f, 0.15f, 0.05f); s.Voiced(V, Ee); break;
                case 'u': s.Voiced(0.03f, Ee, Oo); s.Voiced(V, Oo); break;                     // "you"
                case 'v': s.Voiced(0.04f, Labial, Ee, 0.5f, 1f, hissAmp: 0.15f, hissHz: 3000); s.Voiced(V, Ee); break;
                case 'w': s.Voiced(0.03f, W, Ah); s.Voiced(V, Ah); break;                      // "dub"-ish
                case 'x': s.Voiced(V * 0.8f, Eh); s.Burst(0.01f, 1800, 0.4f); s.Hiss(0.05f, 4500, 2f, 0.3f, 0.1f); break; // "ex"
                case 'y': s.Voiced(0.03f, W, Ah); s.Voiced(V, Ah, Ee); break;                  // "why"
                case 'z': s.Voiced(0.04f, Alveolar, Ee, 0.5f, 1f, hissAmp: 0.2f, hissHz: 4500); s.Voiced(V, Ee); break;
            }
            return s.Finish();
        }

        // Undertale-style: a short, dark square-wave beep that decays quickly.
        private static float[] SynthBlip(float f0)
        {
            int count = Mathf.RoundToInt(0.07f * SampleRate);
            var samples = new float[count];
            float frequency = f0 * 2f;
            float lowPass = 0f;
            float lowPassCoefficient = 1f - Mathf.Exp(-2f * Mathf.PI * 2000f / SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / SampleRate;
                float square = Mathf.Repeat(t * frequency, 1f) < 0.5f ? 1f : -1f;
                lowPass += lowPassCoefficient * (square - lowPass);
                samples[i] = lowPass * Mathf.Exp(-t * 30f);
            }
            Shape(samples);
            return samples;
        }

        // Fast attack and release, then normalize to a safe peak.
        private static void Shape(float[] samples)
        {
            int attack = Mathf.RoundToInt(0.004f * SampleRate);
            int release = Mathf.RoundToInt(0.02f * SampleRate);
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float gain = 1f;
                if (i < attack) gain = (float)i / attack;
                int fromEnd = samples.Length - 1 - i;
                if (fromEnd < release) gain *= (float)fromEnd / release;
                samples[i] *= gain;
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }
            if (peak <= 0f) return;
            float scale = 0.8f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
        }

        private sealed class Synth
        {
            private readonly List<float> _samples = new List<float>(4096);
            private readonly System.Random _random;
            private readonly float _lowPassCoefficient = 1f - Mathf.Exp(-2f * Mathf.PI * 2500f / SampleRate);
            private Biquad _formant1;
            private Biquad _formant2;
            private Biquad _noiseBand;
            private float _f0;
            private float _phase;
            private float _lowPass;

            public Synth(float f0, int seed)
            {
                _f0 = f0;
                _random = new System.Random(seed);
            }

            // A pitch-gliding sawtooth through two formant resonators, low-passed for a darker tone.
            public void Voiced(float seconds, Vector2 from, Vector2? to = null, float ampFrom = 1f, float ampTo = 1f, float hissAmp = 0f, float hissHz = 0f)
            {
                Vector2 target = to ?? from;
                int count = Mathf.RoundToInt(seconds * SampleRate);
                float glide = Mathf.Exp(Mathf.Log(0.94f) / (0.1f * SampleRate)); // falls ~6% per 100ms, like a spoken syllable
                if (hissAmp > 0f) _noiseBand.SetBandPass(hissHz, 1.5f);

                for (int i = 0; i < count; i++)
                {
                    float t = count > 1 ? (float)i / (count - 1) : 1f;
                    Vector2 formants = Vector2.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
                    _formant1.SetBandPass(formants.x, formants.x / 90f);
                    _formant2.SetBandPass(formants.y, formants.y / 130f);

                    float saw = 2f * _phase - 1f;
                    _phase = Mathf.Repeat(_phase + _f0 / SampleRate, 1f);
                    _f0 *= glide;

                    float voiced = _formant1.Process(saw) + 0.5f * _formant2.Process(saw);
                    _lowPass += _lowPassCoefficient * (voiced - _lowPass);

                    float sample = _lowPass * Mathf.Lerp(ampFrom, ampTo, t) * 6f;
                    if (hissAmp > 0f) sample += _noiseBand.Process(Noise()) * hissAmp;
                    _samples.Add(sample);
                }
            }

            // Band-limited noise for fricatives (s, f, sh).
            public void Hiss(float seconds, float centerHz, float q, float ampFrom, float ampTo)
            {
                _noiseBand.SetBandPass(centerHz, q);
                int count = Mathf.RoundToInt(seconds * SampleRate);
                for (int i = 0; i < count; i++)
                {
                    float t = count > 1 ? (float)i / (count - 1) : 1f;
                    _samples.Add(_noiseBand.Process(Noise()) * Mathf.Lerp(ampFrom, ampTo, t));
                }
            }

            // A short decaying noise pop for plosives (b, d, k, p, t).
            public void Burst(float seconds, float centerHz, float amp)
            {
                Hiss(seconds, centerHz, 1f, amp, 0f);
            }

            public float[] Finish()
            {
                float[] samples = _samples.ToArray();
                Shape(samples);
                return samples;
            }

            private float Noise() => (float)(_random.NextDouble() * 2.0 - 1.0);
        }

        // RBJ cookbook band-pass (0 dB peak gain). Keeps its state when retuned so sweeps stay smooth.
        private struct Biquad
        {
            private float _b0, _b2, _a1, _a2;
            private float _x1, _x2, _y1, _y2;

            public void SetBandPass(float frequency, float q)
            {
                float w0 = 2f * Mathf.PI * Mathf.Min(frequency, SampleRate * 0.45f) / SampleRate;
                float alpha = Mathf.Sin(w0) / (2f * Mathf.Max(q, 0.1f));
                float a0 = 1f + alpha;
                _b0 = alpha / a0;
                _b2 = -alpha / a0;
                _a1 = -2f * Mathf.Cos(w0) / a0;
                _a2 = (1f - alpha) / a0;
            }

            public float Process(float x)
            {
                float y = _b0 * x + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1;
                _x1 = x;
                _y2 = _y1;
                _y1 = y;
                return y;
            }
        }

        // ── WAV ───────────────────────────────────────────────────────────────────

        private static void WriteWav(string path, float[] samples)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int dataSize = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);                // fmt chunk size
                writer.Write((short)1);          // PCM
                writer.Write((short)1);          // mono
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);    // byte rate
                writer.Write((short)2);          // block align
                writer.Write((short)16);         // bits per sample
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);
                foreach (float sample in samples)
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
