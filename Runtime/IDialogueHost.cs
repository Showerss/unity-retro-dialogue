using UnityEngine.InputSystem;

namespace Retro.Dialogue
{
    /// <summary>
    /// The player side of a conversation: which input advances the dialogue and how to freeze the player
    /// while the box is open. Implemented by an adapter for your controller (see Adapters/FirstPersonController).
    /// </summary>
    public interface IDialogueHost
    {
        /// <summary>Pressed to finish the current line, then to advance. Null if it can't be found.</summary>
        InputAction AdvanceAction { get; }

        /// <summary>Key shown in the hint, e.g. "E".</summary>
        string AdvanceKeyLabel { get; }

        /// <summary>Stops (true) or restores (false) the player's movement, look and interaction.</summary>
        void SetPlayerLocked(bool locked);
    }
}
