using FirstPerson.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Retro.Dialogue.Adapters
{
    /// <summary>
    /// Bridges the dialogue box to com.firstperson.controller. Put this on the player, next to
    /// <see cref="PlayerInteraction"/>. The Interact action advances dialogue; while a conversation is open,
    /// interaction and the controller (movement and look) are switched off.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInteraction))]
    public sealed class FpcDialogueHost : MonoBehaviour, IDialogueHost
    {
        [Tooltip("Name of the action that advances dialogue.")]
        [SerializeField] private string _advanceActionName = "Interact";

        [Tooltip("Optional. Found on this object if left empty.")]
        [SerializeField] private PlayerInput _playerInput;

        private PlayerInteraction _interaction;
        private FirstPersonController _controller;
        private InputAction _advanceAction;

        public string AdvanceKeyLabel => _interaction.InteractKeyLabel;

        // Resolved on first use: PlayerInput may still be setting up its action asset during Awake.
        public InputAction AdvanceAction
        {
            get
            {
                if (_advanceAction == null && _playerInput != null)
                    _advanceAction = _playerInput.actions.FindAction(_advanceActionName);
                return _advanceAction;
            }
        }

        private void Awake()
        {
            _interaction = GetComponent<PlayerInteraction>();
            _controller = GetComponent<FirstPersonController>();
            if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
                Debug.LogError($"[FpcDialogueHost] '{name}' has no PlayerInput.", this);
        }

        public void SetPlayerLocked(bool locked)
        {
            _interaction.enabled = !locked;
            if (_controller != null) _controller.enabled = !locked;
        }
    }
}
