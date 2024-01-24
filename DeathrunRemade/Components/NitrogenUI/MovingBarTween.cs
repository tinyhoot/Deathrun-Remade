using UnityEngine;

namespace DeathrunRemade.Components.NitrogenUI
{
    /// <summary>
    /// Responsible for animating the movement of a thin bar across the area of its parent.
    /// </summary>
    internal class MovingBarTween : MonoBehaviour
    {
#pragma warning disable CS0649
        public RectTransform barParent;
        public RectTransform movingBar;
#pragma warning restore CS0649
        
        public Vector2 StartPosition { get; private set; }
        public Vector2 TargetPosition { get; private set; }

        /// <summary>
        /// The number of frames required to reach a target position. Higher values are smoother.
        /// </summary>
        public float SmoothingFrames { get; set; } = 3f;

        private float _framesElapsed;

        private void Update()
        {
            if (StartPosition == TargetPosition || _framesElapsed >= SmoothingFrames)
                return;
            
            _framesElapsed++;
            movingBar.anchoredPosition = Vector2.Lerp(StartPosition, TargetPosition, _framesElapsed * (1 / SmoothingFrames));
        }

        /// <summary>
        /// Set the normalised position of the moving bar. The result may be different depending on the anchors of
        /// this bar's <see cref="RectTransform"/>. This method sets the target position directly, without smoothing.
        /// </summary>
        /// <param name="x">A value between 0 and 1.</param>
        /// <param name="y">A value between 0 and 1.</param>
        public void SetPositionImmediate(float x, float y)
        {
            movingBar.anchoredPosition = Denormalise(x, y);
            _framesElapsed = SmoothingFrames;
        }

        /// <summary>
        /// Set the normalised position of the moving bar. The result may be different depending on the anchors of
        /// this bar's <see cref="RectTransform"/>.
        /// </summary>
        /// <param name="x">A value between 0 and 1.</param>
        /// <param name="y">A value between 0 and 1.</param>
        public void SetTarget(float x, float y)
        {
            StartPosition = movingBar.anchoredPosition;
            TargetPosition = Denormalise(x, y);
            _framesElapsed = 0f;
        }
        
        private Vector2 Denormalise(float x, float y)
        {
            // The maximum range is based on the size of the parent.
            Rect rect = barParent.rect;
            return new Vector2(Mathf.Clamp01(x) * rect.width, Mathf.Clamp01(y) * rect.height);
        }
    }
}