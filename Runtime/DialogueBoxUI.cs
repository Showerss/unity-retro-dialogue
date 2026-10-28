using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Retro.Dialogue
{
    /// <summary>
    /// DOS-style dialogue box. Lines type out with the speaker's mumble voice. While it is open the player is locked;
    /// pressing advance mid-line speeds up the typing; once the line is done it shows the next line, and closes the box after the last one.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueBoxUI : MonoBehaviour
    {
        [Tooltip("Root of the box; hidden while closed. Must be a child, not this object.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Shows the speaker's name.")]
        [SerializeField] private TMP_Text _speakerText;

        [Tooltip("Types out the current line (its text is the line body).")]
        [SerializeField] private Typewriter _typewriter;

        [Tooltip("Shows the key hint, e.g. '[E] More'. Empty while a line is still typing.")]
        [SerializeField] private TMP_Text _continueText;

        [Tooltip("Hint after the key while more lines follow.")]
        [SerializeField] private string _moreLabel = "More";

        [Tooltip("Hint after the key on the last line.")]
        [SerializeField] private string _closeLabel = "Close";

        [Tooltip("Colour the speaker's name with their voice's text colour.")]
        [SerializeField] private bool _tintSpeakerName = true;

        [Tooltip("Colour the line with the speaker's voice text colour.")]
        [SerializeField] private bool _tintLine = true;

        [Tooltip("Text colour for dialogue whose speaker has no voice.")]
        [SerializeField] private Color _defaultTextColor = Color.white;

        private DialogueDefinition _dialogue;
        private int _lineIndex;
        private int _openedFrame;
        private IDialogueHost _host;
        private InputAction _advanceAction;
        private TMP_FontAsset _defaultSpeakerFont;
        private TMP_FontAsset _defaultLineFont;

        public bool IsOpen => _dialogue != null;

        /// <summary>Raised after the box closes, once the player has been unlocked.</summary>
        public event Action Closed;

        private void Awake()
        {
            _panel.SetActive(false);
            _defaultSpeakerFont = _speakerText.font;
            _defaultLineFont = _typewriter.Text.font;
        }

        private void OnEnable()
        {
            _typewriter.Finished += HandleLineFinished;
        }

        private void OnDisable()
        {
            _typewriter.Finished -= HandleLineFinished;
            if (IsOpen) Close(restoreImmediately: true);
        }

        public void Open(DialogueDefinition dialogue, IDialogueHost host)
        {
            if (IsOpen || dialogue == null || dialogue.LineCount == 0 || host == null) return;

            _advanceAction = host.AdvanceAction;
            if (_advanceAction == null)
            {
                Debug.LogError("[DialogueBoxUI] The dialogue host has no advance input.", this);
                return;
            }

            _dialogue = dialogue;
            _lineIndex = 0;
            _openedFrame = Time.frameCount;
            _host = host;

            _advanceAction.performed += HandleAdvance;
            _host.SetPlayerLocked(true);

            Color speakerColor = dialogue.Voice != null ? dialogue.Voice.TextColor : _defaultTextColor;
            if (_tintSpeakerName) _speakerText.color = speakerColor;
            if (_tintLine) _typewriter.Text.color = speakerColor;

            TMP_FontAsset voiceFont = dialogue.Voice != null ? dialogue.Voice.Font : null;
            _speakerText.font = voiceFont != null ? voiceFont : _defaultSpeakerFont;
            _typewriter.Text.font = voiceFont != null ? voiceFont : _defaultLineFont;

            _speakerText.text = dialogue.SpeakerName;
            _panel.SetActive(true);
            ShowLine();
        }

        private void HandleAdvance(InputAction.CallbackContext _)
        {
            // Ignore the press that opened the box.
            if (Time.frameCount == _openedFrame) return;

            if (_typewriter.IsTyping)
            {
                _typewriter.SpeedUp();
                return;
            }

            _lineIndex++;
            if (_lineIndex < _dialogue.LineCount) ShowLine();
            else Close(restoreImmediately: false);
        }

        private void ShowLine()
        {
            _continueText.text = string.Empty;
            _typewriter.Play(_dialogue.GetLine(_lineIndex), _dialogue.Voice, _dialogue.Mood);
        }

        private void HandleLineFinished()
        {
            if (!IsOpen) return;
            bool isLast = _lineIndex == _dialogue.LineCount - 1;
            _continueText.text = $"[{_host.AdvanceKeyLabel}] {(isLast ? _closeLabel : _moreLabel)}";
        }

        private void Close(bool restoreImmediately)
        {
            _advanceAction.performed -= HandleAdvance;
            _typewriter.Stop();
            _panel.SetActive(false);
            _dialogue = null;

            IDialogueHost host = _host;
            _host = null;
            _advanceAction = null;

            if (restoreImmediately || !isActiveAndEnabled) Restore(host);
            else StartCoroutine(RestoreNextFrame(host));
        }

        // The closing press is still being dispatched; unlocking now would let it reopen the conversation.
        private IEnumerator RestoreNextFrame(IDialogueHost host)
        {
            yield return null;
            Restore(host);
        }

        private void Restore(IDialogueHost host)
        {
            // A destroyed host component still compares non-null as an interface.
            if (!(host is UnityEngine.Object unityObject && unityObject == null)) host.SetPlayerLocked(false);
            Closed?.Invoke();
        }
    }
}
