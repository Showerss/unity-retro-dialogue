using UnityEditor;
using UnityEngine;

namespace Retro.Dialogue.Editor
{
    /// <summary>Draws a <see cref="SpeechVoice"/> with its 26 letter slots labelled A to Z.</summary>
    [CustomEditor(typeof(SpeechVoice))]
    public sealed class SpeechVoiceEditor : UnityEditor.Editor
    {
        const string LetterClipsField = "_letterClips";

        private bool _lettersExpanded = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(property);
                }
                else if (property.name == LetterClipsField)
                {
                    DrawLetterClips(property);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, includeChildren: true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLetterClips(SerializedProperty clips)
        {
            if (clips.arraySize != SpeechVoice.LetterCount) clips.arraySize = SpeechVoice.LetterCount;

            _lettersExpanded = EditorGUILayout.Foldout(_lettersExpanded, new GUIContent("Letter Clips", clips.tooltip), toggleOnLabelClick: true);
            if (!_lettersExpanded) return;

            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < SpeechVoice.LetterCount; i++)
                {
                    string letter = ((char)('A' + i)).ToString();
                    EditorGUILayout.PropertyField(clips.GetArrayElementAtIndex(i), new GUIContent(letter));
                }
            }
        }
    }
}
