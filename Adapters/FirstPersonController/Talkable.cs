using FirstPerson.Interaction;
using UnityEngine;

namespace Retro.Dialogue.Adapters
{
    /// <summary>
    /// A character the player can talk to with Interact. Opens the scene's dialogue box with this character's lines.
    /// The player needs an <see cref="IDialogueHost"/> (e.g. <see cref="FpcDialogueHost"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Talkable : MonoBehaviour, IInteractable
    {
        [Tooltip("What this character says.")]
        [SerializeField] private DialogueDefinition _dialogue;

        [Tooltip("The scene's dialogue box (under --- UI ---).")]
        [SerializeField] private DialogueBoxUI _dialogueBox;

        private InteractionPrompt _prompt;

        private void Awake()
        {
            if (_dialogue == null || _dialogueBox == null)
            {
                Debug.LogError($"[Talkable] '{name}' needs a Dialogue and a Dialogue Box assigned.", this);
                return;
            }

            _prompt = new InteractionPrompt($"Talk to {_dialogue.SpeakerName}");
        }

        public InteractionPrompt GetPrompt(PlayerInteraction interactor)
        {
            return _dialogueBox != null && !_dialogueBox.IsOpen ? _prompt : InteractionPrompt.None;
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (_dialogueBox == null) return;

            IDialogueHost host = interactor.GetComponent<IDialogueHost>();
            if (host == null)
            {
                Debug.LogError($"[Talkable] '{interactor.name}' has no {nameof(FpcDialogueHost)} (or other IDialogueHost).", this);
                return;
            }

            _dialogueBox.Open(_dialogue, host);
        }
    }
}
