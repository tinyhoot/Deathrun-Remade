using System;
using System.Collections.Generic;
using DeathrunRemade.Objects;
using HootLib;
using Nautilus.Utility;
using TMPro;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;
using Object = UnityEngine.Object;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Everything that communicates directly with the player goes here.
    /// </summary>
    internal class NotificationHandler
    {
        public const string TopMiddle = "top_middle";
        public const string LeftMiddle = "left_middle";
        public const string Centre = "centre";

        public static NotificationHandler Main;
        public static bool Ready { get; private set; }
        
        private ILogHandler _log;
        private Dictionary<string, BasicText> _textSlots;
        private List<Message> _messages;

        public NotificationHandler(ILogHandler logger)
        {
            Main = this;
            _log = logger;
            _textSlots = new Dictionary<string, BasicText>();
            _messages = new List<Message>();
        }

        /// <summary>
        /// Prepare all default slots for the rest of the mod to use.
        /// </summary>
        public void SetupSlots()
        {
            RectTransform rect = (RectTransform)uGUI.main.intro.transform;
            CreateSlot(TopMiddle, 0, (int)(rect.rect.height / 2) - 150);
            CreateSlot(LeftMiddle, (int)(rect.rect.width / -2) + 20, 0)
                .SetAlign(TextAlignmentOptions.Left);
            CreateSlot(Centre, 0, 75);
            Ready = true;
        }

        /// <summary>
        /// Perform one update tick on all current messages.
        /// </summary>
        public void Update()
        {
            // Nothing to do.
            if (_textSlots.Count == 0 || _messages.Count == 0)
                return;

            // Copy the list so we don't run into issues with deleting messages as we go.
            foreach (var message in _messages.ShallowCopy())
            {
                MessageState state = message.UpdateState(Time.time);
                switch (state)
                {
                    case MessageState.Display:
                        ShowMessage(message);
                        break;
                    case MessageState.FadeOut:
                        FadeOutMessage(message);
                        break;
                    case MessageState.Ended:
                        DeleteMessage(message);
                        break;
                }
            }
        }

        /// <summary>
        /// Add a message to be displayed later.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the slot id does not exist.</exception>
        public Message AddMessage(string slotId, string text, bool showImmediately = true)
        {
            if (!_textSlots.ContainsKey(slotId))
                throw new ArgumentException($"No text slot with id {slotId} exists!");
            
            var message = new Message(slotId, text);
            _messages.Add(message);
            if (showImmediately)
                message.SetDisplayTime(Time.time);
            _log.Debug($"[{message.SlotId}] {message.Text}");
            return message;
        }

        /// <summary>
        /// Hides a message from the screen and deletes it from the list.
        /// </summary>
        public void DeleteMessage(Message message)
        {
            var slot = GetSlot(message.SlotId);
            //var fade = slot.GetTextFade();
            slot.Hide();
            _messages.Remove(message);
        }

        /// <summary>
        /// Start fading out a message in the requested slot.
        /// </summary>
        public void FadeOutMessage(Message message)
        {
            var slot = GetSlot(message.SlotId);
            var fade = slot.GetTextFade();
            if (fade == null || !fade.sequence.active)
                slot.ShowMessage(message.Text, message.FadeOutDuration);
        }

        /// <summary>
        /// Show the message in its requested slot.
        /// </summary>
        public void ShowMessage(Message message)
        {
            var slot = GetSlot(message.SlotId);
            slot.ShowMessage(message.Text);
        }

        /// <summary>
        /// Create a slot for text to appear in.
        /// </summary>
        /// <param name="id">The unique id to give to this slot.</param>
        /// <param name="x">The x coordinate to anchor the slot to. Centered on the middle of the screen.</param>
        /// <param name="y">The y coordinate to anchor the slot to. Centered on the middle of the screen.</param>
        public BasicText CreateSlot(string id, int x = 0, int y = 0)
        {
            if (_textSlots.ContainsKey(id))
                throw new ArgumentException($"Cannot create text slot with duplicate id {id}!");
            BasicText text = new BasicText(x, y);
            text.SetAlign(TextAlignmentOptions.Center);
            // Force nautilus to build the text object and its components.
            text.ShowMessage("", 0f);
            text.GetTextFade().text.outlineWidth = 0.1f;
            _textSlots.Add(id, text);
            return text;
        }

        /// <summary>
        /// Create a slot for text to appear in, or get a slot with the same id if it already exists.
        /// </summary>
        /// <param name="id">The unique id to give to this slot.</param>
        public BasicText CreateOrGetSlot(string id)
        {
            if (_textSlots.TryGetValue(id, out BasicText slot))
                return slot;
            return CreateSlot(id);
        }

        /// <summary>
        /// Destroy the GameObject of the specified slot.
        /// </summary>
        public void DestroySlot(string id)
        {
            if (_textSlots.TryGetValue(id, out BasicText text))
            {
                Object.Destroy(text.GetTextObject());
                _textSlots.Remove(id);
            }
        }

        /// <summary>
        /// Get the slot with the specified id.
        /// </summary>
        public BasicText GetSlot(string id)
        {
            if (_textSlots.TryGetValue(id, out BasicText text))
                return text;
            throw new KeyNotFoundException($"Cannot find text element with id {id}!");
        }
        
        /// <summary>
        /// Check whether a message is currently being shown in the given slot id.
        /// </summary>
        public bool IsShowingMessage(string slotId)
        {
            var slot = GetSlot(slotId);
            uGUI_TextFade fade = slot.GetTextFade();
            GameObject text = slot.GetTextObject();
            if (fade == null || text == null)
                return false;
            return slot.GetTextFade().enabled && slot.GetTextObject().activeSelf;
        }

        /// <summary>
        /// Register this on main menu load to set up all slots as soon as possible.
        /// </summary>
        public void OnMainMenuLoaded()
        {
            GameEventHandler.OnMainMenuLoaded -= OnMainMenuLoaded;
            SetupSlots();
        }
    }
}