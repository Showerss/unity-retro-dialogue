using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>
    /// Plays a <see cref="SpeechVoice"/> one letter at a time. Each new letter cuts off the one before,
    /// as Animalese does, so fast text never piles up into a smear.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SpeechVoicePlayer : MonoBehaviour
    {
        private AudioSource _source;
        private AudioDistortionFilter _distortion;
        private AudioLowPassFilter _lowPass;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _distortion = GetComponent<AudioDistortionFilter>();
            _lowPass = GetComponent<AudioLowPassFilter>();
            _source.playOnAwake = false;
            _source.loop = false;
        }

        /// <summary>Plays the clip for <paramref name="c"/>; silent characters do nothing.</summary>
        /// <param name="rising">True on the last word of a question.</param>
        public void Speak(char c, SpeechVoice voice, bool rising)
        {
            if (voice == null) return;
            AudioClip clip = voice.GetClip(c);
            if (clip == null) return;

            float pitch = voice.BasePitch + Random.Range(-voice.PitchJitter, voice.PitchJitter);
            if (rising) pitch += voice.QuestionRise;

            _source.Stop();
            _source.clip = clip;
            _source.pitch = pitch;
            _source.volume = voice.Volume;
            ApplyGrowl(voice);
            _source.Play();
        }

        // Filters are added only once a voice asks for them, so clean voices cost nothing.
        private void ApplyGrowl(SpeechVoice voice)
        {
            bool distort = voice.Distortion > 0f;
            if (distort && _distortion == null) _distortion = gameObject.AddComponent<AudioDistortionFilter>();
            if (_distortion != null)
            {
                _distortion.enabled = distort;
                _distortion.distortionLevel = voice.Distortion;
            }

            bool muffle = voice.LowPassCutoff < 22000f;
            if (muffle && _lowPass == null) _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            if (_lowPass != null)
            {
                _lowPass.enabled = muffle;
                _lowPass.cutoffFrequency = voice.LowPassCutoff;
            }
        }

        public void Stop()
        {
            if (_source != null) _source.Stop();
        }
    }
}
