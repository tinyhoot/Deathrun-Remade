using UnityEngine;

namespace DeathrunRemade.Objects
{
    internal class Message
    {
        public string SlotId { get; }
        public string Text { get; }
        public float DisplayTime;
        public float DisplayDuration = 10;
        public float FadeOutTime;
        public float FadeOutDuration = 1;
        public float EndTime;
        public MessageState State = MessageState.Inactive;

        public Message(string slotId, string text)
        {
            SlotId = slotId;
            Text = text;
        }

        /// <summary>
        /// Get the text and simultaneously set up the timers of this message based on the Unity time right now.
        /// </summary>
        // public string Display()
        // {
        //     DisplayTime = Time.time;
        //     Update();
        //     return Text;
        // }

        public void SetDisplayTime(float time)
        {
            DisplayTime = time;
            Update();
        }

        public void SetDuration(float display, float fadeOut)
        {
            DisplayDuration = display;
            FadeOutDuration = fadeOut;
            Update();
        }

        /// <summary>
        /// Update the fadeout and end times after a change to display durations.
        /// </summary>
        public void Update()
        {
            FadeOutTime = DisplayTime + DisplayDuration;
            EndTime = FadeOutTime + FadeOutDuration;
        }
        
        /// <summary>
        /// Update and return the current state this message should be in.
        /// </summary>
        public MessageState UpdateState(float time)
        {
            MessageState state = MessageState.Inactive;
            if (DisplayTime > time)
                state = MessageState.Waiting;
            else if (DisplayTime <= time && time < FadeOutTime)
                state = MessageState.Display;
            else if (FadeOutTime <= time && time < EndTime)
                state = MessageState.FadeOut;
            else if (time >= EndTime)
                state = MessageState.Ended;
                
            State = state;
            return state;
        }
    }

    internal enum MessageState
    {
        Inactive,
        Waiting,
        FadeIn,  // Unused for now.
        Display,
        FadeOut,
        Ended,
    }
}