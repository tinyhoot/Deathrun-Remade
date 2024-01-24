using System;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HootLib.Objects;
using UnityEngine;

namespace DeathrunRemade.Components
{
    [RequireComponent(typeof(NitrogenHandler))]
    internal class FastAscent : MonoBehaviour
    {
        private const float GraceTime = 2f; // Number of seconds at fast speeds before consequences set in.
        private const float PunishInterval = 0.25f; // Number of seconds between bad effects from too much speed.
        private const float PunishRampUp = 2f; // Number of seconds to pass until bad effects are at full force.
        private const float FlatNitrogenPerSec = 5f;
        private const float SafeDepthBufferLoss = 0.25f;
        private const float SafeDepthLoss = 0.05f;
        private readonly AnimationCurve _punishMultCurve = new AnimationCurve(new[]
        {
            new Keyframe(0f, 0.5f), // Bad effects will start at half their full strength.
            new Keyframe(1f, 1f)
        });
        
        public float AscentRate { get; private set; }
        public const float ResetThreshold = 1.5f;
        public const float WarnThreshold = 4f;
        public const float DamageThreshold = 4.5f;
        
        private float _dangerTime;
        private NitrogenHandler _nitrogenHandler;
        private Rigidbody _rigidbody;
        private SaveData _saveData;
        private Hootimer _timer;

        public static event Action<float> OnAscentRateChanged;

        private void Awake()
        {
            _nitrogenHandler = GetComponent<NitrogenHandler>();
            _rigidbody = GetComponent<Rigidbody>();
            _saveData = SaveData.Main;
            _timer = new Hootimer(() => Time.deltaTime, PunishInterval);
        }

        private void Update()
        {
            if (_saveData is null)
                return;
            
            switch (AscentRate)
            {
                case <= ResetThreshold:
                    // Reset punishments if we're moving at comfortable speeds.
                    DecreaseDangerTime();
                    break;
                case > WarnThreshold and <= DamageThreshold:
                    // Increase danger time without direct consequences, for now.
                    IncreaseDangerTime();
                    break;
                case > DamageThreshold:
                    // Increase danger time and cause issues for the player.
                    IncreaseDangerTime();
                    DoConsequences();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null)
                return;

            float oldRate = AscentRate;
            // Average the player's vertical speed over the past second.
            float speed = _rigidbody.velocity.y;
            float ups = (1 / Time.fixedDeltaTime);
            AscentRate = (AscentRate * (ups - 1) + speed) / ups;
            // Notify all listeners if the rate has changed.
            if (!Mathf.Approximately(oldRate, AscentRate))
                OnAscentRateChanged?.Invoke(AscentRate);
        }
        
        private void DecreaseDangerTime()
        {
            _dangerTime = Mathf.Max(_dangerTime - Time.deltaTime, 0f);
        }

        private void IncreaseDangerTime()
        {
            // Starting to get too fast. Start accruing "danger time".
            _dangerTime += Time.deltaTime;
            WarningHandler.ShowWarning(Warning.AscentSpeed);
        }

        private void DoConsequences()
        {
            // Only do consequences after danger time exceeds the grace period.
            if (_dangerTime <= GraceTime)
                return;
            // Don't inflict effects every frame, wait for a small cooldown period before doing this again.
            if (!_timer.Tick())
                return;
            
            // Idea of the maths:
            // - Every second, lose 25% of your buffer between current depth and safe depth.
            // - Additionally, lose 5% of your current safe depth.
            // - Additionally, lose a flat amount.
            // At 1000 depth, you lose 60+40+5 = 105/s.
            // At 50 depth, you lose 5+1+5 = 11/s.
            float safetyDiff = Player.main.GetDepth() - _saveData.Nitrogen.safeDepth;
            float nitrogenPerSec = (safetyDiff * SafeDepthBufferLoss)
                                   + (_saveData.Nitrogen.safeDepth * SafeDepthLoss)
                                   + FlatNitrogenPerSec;
            
            // Ramp up the strength of the effect over time.
            float totalDanger = _dangerTime - GraceTime;
            float dangerMult = _punishMultCurve.Evaluate(PunishRampUp / Mathf.Min(totalDanger, PunishRampUp));
            
            float nitrogen = dangerMult * PunishInterval * nitrogenPerSec;
            _nitrogenHandler.AddNitrogen(nitrogen);
        }
    }
}