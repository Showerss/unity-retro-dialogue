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

        [Tooltip("Default text mood for this conversation. Inline <angry>, <happy>, and <embarrassed> tags override it for individual phrases.")]
        [SerializeField] private DialogueMood _mood;

        [Tooltip("Lines shown one at a time. Interact speeds up a line that is still typing, then advances; the box closes after the last line.")]
        [TextArea(2, 5)]
        [SerializeField] private string[] _lines = new string[0];

        public string SpeakerName => _speakerName;
        public SpeechVoice Voice => _voice;
        public DialogueMood Mood => _mood;
        public int LineCount => _lines != null ? _lines.Length : 0;
        public string GetLine(int index) => _lines[index];
    }
}
