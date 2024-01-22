using System.Collections;
using DeathrunRemade.Handlers;
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
        private SaveData _saveData;
        
        public float freezeDistance = 100f;
        public bool IsAnchored { get; private set; }
        public bool IsSinking { get; private set; }

        private void Awake()
        {
            _pod = GetComponent<EscapePod>();
            _rigidbody = _pod.rigidbodyComponent;
            _wf = GetComponent<WorldForces>();
            _saveData = SaveData.Main;
            
            // Ensure the lifepod does not move anywhere until the game has loaded in far enough for us to decide what to do.
            SetKinematic(true);
            // Only handles existing save games. A harmony patch initiates sinking for new games.
            GameEventHandler.OnSavedGameLoaded += OnSavedGameLoaded;
        }
        
        private void FixedUpdate()
        {
            // Make the pod sink smoothly if the player is inside it. Something else keeps resetting this and I can't
            // figure out what so this has to be done every frame.
            if (IsSinking)
                _rigidbody.interpolation = Player.main.currentEscapePod == null ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        }

        /// <summary>
        /// Start sinking the lifepod.
        /// </summary>
        public void SinkPod()
        {
            DeathrunInit._Log.Debug("Sinking lifepod.");
            SetKinematic(false);
            // Sinking works by setting the gravity values super high.
            _wf.aboveWaterGravity = 50f;
            _wf.underwaterGravity = 30f;
            // Temporarily disable interpolation to enable precision movement and prevent jitter while sinking.
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            IsSinking = true;

            StartCoroutine(CheckForGround());
        }

        private IEnumerator CheckForGround()
        {
            // Get a layer mask so we can check exclusively for proximity to terrain.
            LayerMask terrain = LayerMask.GetMask("TerrainCollider");
            while (IsSinking)
            {
                yield return new WaitForSeconds(0.25f);
                // Ensure the pod does not fall through the ground.
                FreezeIfTooFar();
                
                // Check for terrain underneath the pod while leaving enough space for the pod to fall over.
                float distance = _saveData.Config.ToppleLifepod ? 6.5f : 4f;
                if (!Physics.Raycast(_pod.transform.position, Vector3.down, distance, terrain))
                    continue;
                // DeathrunInit._Log.InGameMessage($"Hit '{hitInfo.collider.name}' in {hitInfo.distance:F1}m");
                
                AnchorPod();
                SimulateImpact();
                yield break;
            }
        }
        
        /// <summary>
        /// Anchor the pod in place. Works for after the pod sunk and hit the ground <em>and</em> for re-anchoring after
        /// loading an existing save.
        /// </summary>
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
            
            if (_saveData.Config.ToppleLifepod)
            {
                if (!_saveData.EscapePod.isToppled)
                    TopplePod();
                StartCoroutine(WaitForRepair());
            }
            else
            {
                // With no toppling to do, we're done.
                Destroy(this);
            }
        }

        /// <summary>
        /// Make it <em>feel</em> like the pod just hit the ground.
        /// </summary>
        private void SimulateImpact()
        {
            // Only shake the camera if the player is inside the pod.
            if (Player.main.currentEscapePod != null)
                MainCameraControl.main.ShakeCamera(3f, 1f, MainCameraControl.ShakeMode.Cos);
            // Play an impact sound centred on the pod to really sell it.
            var asset = AudioUtils.GetFmodAsset("event:/sub/cyclops/impact_solid_hard");
            FMODUWE.PlayOneShot(asset, transform.position);
            
            DeathrunInit._Log.InGameMessage("The lifepod has hit bottom!");
        }

        /// <summary>
        /// Simulate an impact which makes the pod roll over.
        /// </summary>
        private void TopplePod()
        {
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
                timePassed += Time.fixedDeltaTime;
                // This function starts at 0 and reaches 1 in roughly one second. It is fast at the start and then
                // tails off more slowly towards the end.
                float progress = Mathf.Log(timePassed, 30f) + 1;
                transform.rotation = Quaternion.Lerp(upright, targetRotation, progress);
                
                // Doing it every frame is smooth, but can slingshot the player depending on position.
                // Doing it on fixed update is safe, but looks jittery due to low update rate.
                yield return new WaitForFixedUpdate();
            }
            DeathrunInit._Log.Debug("Lifepod topple has finished rotating.");
        }

        private IEnumerator WaitForRepair()
        {
            // Do not try to listen to repair events, just in case a save with a repaired but flipped pod is loaded.
            // Instead, check for fully repaired lifepod health.
            while (!_pod.liveMixin.IsFullHealth())
                yield return new WaitForSeconds(1f);
            
            DeathrunInit._Log.Debug("Lifepod has been repaired, undoing rotation lock.");
            SetKinematic(false);
            // Undoing the rotation lock allows the stabiliser component to do the work for us.
            _rigidbody.constraints = RigidbodyConstraints.FreezePosition;
            // Wait a reasonable amount of time for the pod to get into an upright position.
            yield return new WaitForSeconds(10f);
            DeathrunInit._Log.Debug("Lifepod expected to be upright. Setting kinematic.");
            SetKinematic(true);
            Destroy(this);
        }

        /// <summary>
        /// Do the setup for an escape pod that isn't newborn but rather loaded from an existing save.
        /// </summary>
        private void OnSavedGameLoaded()
        {
            if (!_saveData.EscapePod.isAnchored)
            {
                DeathrunInit._Log.Debug("Re-sinking life pod.");
                SinkPod();
            }
            else
            {
                DeathrunInit._Log.Debug("Re-anchoring life pod.");
                AnchorPod();
            }
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
    }
}