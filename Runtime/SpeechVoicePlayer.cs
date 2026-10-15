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

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
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
            _source.Play();
        }

        public void Stop()
        {
            if (_source != null) _source.Stop();
        }
    }
}
