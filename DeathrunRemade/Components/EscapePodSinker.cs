using System.Collections;
using DeathrunRemade.Objects;
using HootLib;
using Nautilus.Utility;
using UnityEngine;

namespace DeathrunRemade.Components
{
    [RequireComponent(typeof(EscapePod), typeof(WorldForces))]
    internal class EscapePodSinker : MonoBehaviour
    {
        private EscapePod _pod;
        private Rigidbody _rigidbody;
        private WorldForces _wf;
        private float _avgDistance;
        private Vector3 _previousPos;
        private SaveData _saveData => SaveData.Main;
        
        public float freezeDistance = 120f;
        public bool IsAnchored { get; private set; }
        public bool IsSinking { get; private set; }

        private void Awake()
        {
            _pod = GetComponent<EscapePod>();
            _rigidbody = _pod.rigidbodyComponent;
            _wf = GetComponent<WorldForces>();
        }

        private void Update()
        {
            // Don't do anything if we're still in the loading screen.
            if (WaitScreen.IsWaiting || uGUI.main.intro.showing || _pod.IsPlayingIntroCinematic())
                return;
            
            // If this is a first time startup, sink!
            if (_saveData.Config.SinkLifepod && !_saveData.EscapePod.isAnchored && !IsSinking)
                SinkPod();
            
            // Ensure no other component (like the Stabiliser!) interferes with the rotation.
            if (_saveData.EscapePod.isToppled)
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            
            // Check whether this component has finished its job.
            if (IsAnchored)
            {
                // If we have just repaired the pod, make it stand back upright.
                if (_saveData.EscapePod.isToppled && DeathrunUtils.IsPodRepaired(_pod))
                {
                    SetKinematic(false);
                    // Undoing the rotation lock allows the stabiliser component to do the work for us.
                    _rigidbody.constraints = RigidbodyConstraints.None;
                    _saveData.EscapePod.isToppled = false;
                }
                // We have done everything we needed to. There's no reason to keep this component running.
                if (!_saveData.Config.ToppleLifepod || (_saveData.Config.ToppleLifepod && !_saveData.EscapePod.isToppled))
                    Destroy(this);
            }
        }

        private void FixedUpdate()
        {
            // Don't do anything if we're still in the loading screen.
            if (WaitScreen.IsWaiting || uGUI.main.intro.showing || _pod.IsPlayingIntroCinematic())
                return;
            // All of the logic below only applies to a sinking pod.
            if (IsAnchored || !IsSinking)
                return;

            // Ensure the pod does not sink through terrain.
            FreezeIfTooFar();
            // Make the pod move smoothly if the player is inside it. Something else keeps resetting this so it has to
            // be done every update.
            Player player = Player.main;
            _rigidbody.interpolation = player.currentEscapePod is null ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            // If the pod stopped moving, assume it hit the ground and anchor it.
            if (IsSinking && HasHitBottom())
            {
                if (_saveData.Config.ToppleLifepod)
                    TopplePod();
                AnchorPod();
                // Only shake the camera if the player is inside the pod.
                if (player.currentEscapePod != null)
                    MainCameraControl.main.ShakeCamera(3f, 1f, MainCameraControl.ShakeMode.Cos);
                // Play an impact sound centred on the pod to really sell it.
                var asset = AudioUtils.GetFmodAsset("event:/sub/cyclops/impact_solid_hard");
                FMODUWE.PlayOneShot(asset, transform.position);

                DeathrunInit._Log.InGameMessage("The lifepod has hit bottom!");
            }
        }
        
        /// <summary>
        /// Re-apply physics changes to the pod when a saved game is loaded.
        /// </summary>
        public static void OnSavedGameLoaded()
        {
            EscapePod.main.GetComponent<EscapePodSinker>().SetupSavedGame();
        }

        private void SetupSavedGame()
        {
            // If the pod is still sinking for some reason, don't re-anchor just yet.
            if (_saveData.Config.SinkLifepod && !_saveData.EscapePod.isAnchored)
                return;
            
            DeathrunInit._Log.Debug("Re-anchoring life pod.");
            AnchorPod();
            Destroy(this);
        }

        public void AnchorPod()
        {
            // Reset gravity and movement precision.
            _wf.aboveWaterGravity = 9.81f;
            _wf.underwaterGravity = 0f;
            _pod.rigidbodyComponent.interpolation = RigidbodyInterpolation.Interpolate;
            // Freeze the pod in place.
            _pod.anchorPosition = transform.position;
            SetKinematic(true);
            IsAnchored = true;
            IsSinking = false;
            _saveData.EscapePod.isAnchored = true;
        }

        /// <summary>
        /// Check whether the lifepod has reached the bottom.
        /// </summary>
        private bool HasHitBottom()
        {
            // If the pod is currently frozen (e.g. by the player moving too far away) hold off on any calculations.
            if (_rigidbody.isKinematic)
                return false;

            // Average the distance moved over the past second. Use sqrMagnitude to avoid slow sqrRoot calculations.
            float distance = Vector3.SqrMagnitude(transform.position - _previousPos);
            _avgDistance = Hootils.AverageOverTime(_avgDistance, distance, Time.fixedDeltaTime, 0.5f);
            _previousPos = transform.position;
            // If the average distance is very low and we're too low to have just started sinking we probably hit bottom.
            return _avgDistance < Mathf.Pow(0.2f, 2f) && Ocean.GetDepthOf(gameObject) > 10f;
        }

        /// <summary>
        /// If the player goes too far away from the pod there is a danger of it clipping through the floor.
        /// Freeze it temporarily to prevent this.
        ///
        /// There is a <see cref="FreezeRigidbodyWhenFar"/> component that does a similar thing but it also updates the
        /// interpolation mode which causes serious stuttering when inside the pod.
        /// </summary>
        private void FreezeIfTooFar()
        {
            float sqrDistance = (MainCamera.camera.transform.position - transform.position).sqrMagnitude;
            // Don't update any kinematics when the player is very close, i.e. practically inside the pod.
            if (sqrDistance < freezeDistance)
                return;
            bool kinematic = sqrDistance > Mathf.Pow(freezeDistance, 2f);
            SetKinematic(kinematic);
        }

        private void SetKinematic(bool kinematic)
        {
            // DeathrunInit._Log.Debug($"Setting lifepod kinematic: {kinematic}");
            _rigidbody.isKinematic = kinematic;
            _rigidbody.collisionDetectionMode = kinematic
                ? CollisionDetectionMode.ContinuousSpeculative
                : CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>
        /// Start sinking the lifepod.
        /// </summary>
        public void SinkPod()
        {
            DeathrunInit._Log.Debug("Sinking lifepod.");
            // Sinking works by setting the gravity values super high.
            _wf.aboveWaterGravity = 50f;
            _wf.underwaterGravity = 30f;
            // Temporarily disable interpolation to enable precision movement and prevent jitter while sinking.
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            IsSinking = true;
        }

        /// <summary>
        /// Simulate a pushing force which makes the pod roll over.
        /// </summary>
        public void TopplePod()
        {
            // Move the pod back up a bit to give it some space to fall over.
            transform.Translate(0f, 3f, 0f);
            // This doesn't seem to move the player with it so do it manually.
            Player.main.transform.Translate(0f, 3f, 0f);
            // Figure out a random rotation to topple to such that the lifepod is sideways or upside down.
            float angle = Random.Range(0.35f, 1f) * 180f;
            float tiltX = Random.Range(0f, 1f);
            float tiltZ = Random.Range(0f, 1f);
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, new Vector3(tiltX, 0f, tiltZ));
            // Start rotating the pod over time.
            StartCoroutine(RotatePod(targetRotation));
            _saveData.EscapePod.isToppled = true;
        }

        /// <summary>
        /// Rotate the pod towards the target angle over several frames.
        /// </summary>
        private IEnumerator RotatePod(Quaternion targetRotation)
        {
            Quaternion upright = transform.rotation;
            float timePassed = 0f;
            while (!Hootils.RotationsApproximately(transform.rotation, targetRotation, 1e-6f))
            {
                timePassed += Time.deltaTime;
                // This function starts at 0 and reaches 1 in roughly one second. It is fast at the start and then
                // tails off more slowly towards the end.
                float progress = Mathf.Log(timePassed, 30f) + 1;
                transform.rotation = Quaternion.Lerp(upright, targetRotation, progress);
                // Run this every frame rather than every fixed update for smoother movement.
                yield return null;
            }
            DeathrunInit._Log.Debug("Lifepod topple has finished rotating.");
        }
    }
}