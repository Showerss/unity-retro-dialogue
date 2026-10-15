using UnityEngine;

namespace Retro.Dialogue
{
    /// <summary>
    /// One character's conversation: a name, a voice, and the lines they say, shown one at a time.
    /// </summary>
    [CreateAssetMenu(menuName = "Retro/Dialogue", fileName = "SO_Dialogue_")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [Tooltip("Name shown in the dialogue box and in the 'Talk to ...' prompt.")]
        [SerializeField] private string _speakerName = "Stranger";

        [Tooltip("How this character mumbles and how fast their text types. Empty = the dialogue box's default voice.")]
        [SerializeField] private SpeechVoice _voice;

        [Tooltip("Lines shown one at a time. Interact finishes the typing, then advances; the box closes after the last line.")]
        [TextArea(2, 5)]
        [SerializeField] private string[] _lines = new string[0];

        public string SpeakerName => _speakerName;
        public SpeechVoice Voice => _voice;
        public int LineCount => _lines != null ? _lines.Length : 0;
        public string GetLine(int index) => _lines[index];
    }
}
