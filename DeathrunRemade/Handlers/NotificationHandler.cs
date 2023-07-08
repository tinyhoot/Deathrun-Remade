using System;
using System.Collections.Generic;
using DeathrunRemade.Objects;
using HootLib;
using Nautilus.Utility;
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
        private ILogHandler _log;
        private Dictionary<string, BasicText> _textSlots;
        private List<Message> _messages;
        
        public NotificationHandler(ILogHandler logger)
        {
            _log = logger;
            _textSlots = new Dictionary<string, BasicText>();
            _messages = new List<Message>();
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
                DeathrunInit._Log.Debug($"Updated state: {state}");
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
        public Message AddMessage(string slotId, string text, bool showImmediately = true)
        {
            var message = new Message(slotId, text);
            _messages.Add(message);
            if (showImmediately)
                message.SetDisplayTime(Time.time);
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
            if (fade is null || !fade.sequence.active)
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
        public BasicText CreateSlot(string id)
        {
            if (_textSlots.ContainsKey(id))
                throw new ArgumentException($"Cannot create text slot with duplicate id {id}!");
            RectTransform rect = (RectTransform)uGUI.main.intro.transform;
            int y = (int)(rect.rect.height / 2) - 50;
            BasicText text = new BasicText(0, y);
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
    }
}