using System.Collections;
using FirstPerson.World;
using UnityEngine;

namespace Retro.Dialogue.Adapters
{
    /// <summary>
    /// Shows a line on the black screen after the controller's <see cref="WorldBoundsWarden"/> puts the player back on the path.
    /// Put it on the same object as the warden.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldBoundsWarden))]
    public sealed class WorldBoundsDialogueWarning : MonoBehaviour, IWorldBoundsWarning
    {
        [Tooltip("What the player is told, e.g. \"I shouldn't wander off the road.\"")]
        [SerializeField] private DialogueDefinition _warning;

        [Tooltip("The scene's dialogue box (under --- UI ---).")]
        [SerializeField] private DialogueBoxUI _dialogueBox;

        [Tooltip("The player's dialogue host.")]
        [SerializeField] private FpcDialogueHost _dialogueHost;

        private void Awake()
        {
            if (_warning == null || _dialogueBox == null || _dialogueHost == null)
                Debug.LogError($"[WorldBoundsDialogueWarning] '{name}' is missing a reference.", this);
        }

        public IEnumerator Show()
        {
            if (_warning == null || _dialogueBox == null || _dialogueHost == null || _dialogueBox.IsOpen) yield break;

            bool open = true;
            void HandleClosed() => open = false;
            _dialogueBox.Closed += HandleClosed;
            _dialogueBox.Open(_warning, _dialogueHost);
            while (open) yield return null;
            _dialogueBox.Closed -= HandleClosed;
        }
    }
}
