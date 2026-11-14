
using UnityEngine;

namespace Code.Scripts.Roulette
{
    public sealed partial class RouletteSectorVFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RouletteView view;
        [SerializeField] private RouletteManager manager;
        [SerializeField] private MeshRenderer sectors;

        [Header("Animation")]
        [SerializeField, Min(0.1f)]
        private float revealDuration = 1.1f;

        [SerializeField, Min(100f)]
        private float maximumSpinSpeed = 850f;

        [Header("Sector under the fixed pointer")]
        [SerializeField, Range(0f, 3f)] private float passingSectorIntensity = 0f;

        private static readonly int SpinId =
            Shader.PropertyToID("_SpinEnergy");
        private static readonly int TimeId =
            Shader.PropertyToID("_FXTime");
        private static readonly int WinnerId =
            Shader.PropertyToID("_WinningSlice");
        private static readonly int BossId =
            Shader.PropertyToID("_ResultIsBoss");
        private static readonly int RevealId =
            Shader.PropertyToID("_RevealEnergy");
        private static readonly int ProgressId =
            Shader.PropertyToID("_RevealProgress");

        private MaterialPropertyBlock block;
        private Quaternion previousRotation;

        private bool wasSpinning;
        private float spinEnergy;
        private int winningSlice = -1;
        private float bossResult;
        private float revealTime = -1f;
        private float revealEnergy;
        private float revealProgress;

        private void OnEnable()
        {
            if (manager == null) return;

            manager.OnSpinCompleted += OnSpinCompleted;
            manager.OnSpinCancelled += OnSpinCancelled;
        }

        private void Start()
        {
            if (view == null || manager == null || sectors == null)
            {
                Debug.LogError(
                    "[RouletteSectorVFX] Missing references.", this);
                enabled = false;
                return;
            }

            if (sectors.sharedMaterial == null ||
                !sectors.sharedMaterial.HasProperty(SpinId))
            {
                Debug.LogError(
                    "[RouletteSectorVFX] Assign the updated shader.",
                    this);
                enabled = false;
                return;
            }

            block = new MaterialPropertyBlock();
            previousRotation = sectors.transform.rotation;

            InitializeCarnival();
            ApplyShaderProperties();
        }

        private void LateUpdate()
        {
            if (block == null || view == null || sectors == null)
                return;

            float dt = Time.unscaledDeltaTime;
            bool spinning = view.IsSpinning;

            if (spinning && !wasSpinning)
            {
                winningSlice = -1;
                bossResult = 0f;
                revealTime = -1f;
                revealEnergy = 0f;
                revealProgress = 0f;

                previousRotation = sectors.transform.rotation;
            }

            UpdateCarnival(dt, spinning);
            if (spinning)
            {
                float degrees = Quaternion.Angle(
                    previousRotation,
                    sectors.transform.rotation
                );

                float speed = premiumRig != null ? measuredSpinSpeed : degrees / Mathf.Max(dt, 0.001f);

                float target = Mathf.Clamp01(
                    speed / maximumSpinSpeed
                );

                float smoothing =
                    1f - Mathf.Exp(-12f * dt);

                spinEnergy = Mathf.Lerp(
                    spinEnergy, target, smoothing
                );
            }
            else
            {
                spinEnergy = Mathf.MoveTowards(
                    spinEnergy, 0f, dt * 4f
                );
            }

            previousRotation = sectors.transform.rotation;
            wasSpinning = spinning;

            if (revealTime >= 0f)
            {
                revealTime += dt;

                float t = Mathf.Clamp01(
                    revealTime / revealDuration
                );

                revealProgress = t;
                revealEnergy = Mathf.Pow(1f - t, 1.2f);

                if (t >= 1f)
                {
                    revealTime = -1f;
                    revealEnergy = 0f;
                }
            }

            ApplyShaderProperties();
        }

        private void OnSpinCompleted(RouletteResult result)
        {
            winningSlice = result.SliceIndex;
            RevealCarnival(result.IsBoss);
            bossResult = result.IsBoss ? 1f : 0f;

            spinEnergy = 0f;
            revealTime = 0f;
            revealProgress = 0f;
            revealEnergy = 1f;

            ApplyShaderProperties();
        }

        private void OnSpinCancelled()
        {
            ResetCarnival();
            winningSlice = -1;
            bossResult = 0f;
            revealTime = -1f;
            revealProgress = 0f;
            revealEnergy = 0f;
            spinEnergy = 0f;

            ApplyShaderProperties();
        }

        private void ApplyShaderProperties()
        {
            if (block == null || sectors == null)
                return;

            // Preserve RouletteView's probability and slice data.
            sectors.GetPropertyBlock(block);

            int count = Mathf.Max(1, Mathf.RoundToInt(block.GetFloat("_SliceCount")));
            int passingSlice = isActiveAndEnabled && view.IsSpinning
                ? RouletteRules.GetSliceIndex(view.GetPointerSectorAngle(), count) : -1;
            block.SetFloat("_PassingSlice", passingSlice);
            block.SetFloat("_PassingIntensity", passingSectorIntensity);

            block.SetFloat(TimeId, Time.unscaledTime);
            block.SetFloat(SpinId, spinEnergy);
            block.SetFloat(WinnerId, winningSlice);
            block.SetFloat(BossId, bossResult);
            block.SetFloat(RevealId, revealEnergy);
            block.SetFloat(ProgressId, revealProgress);

            sectors.SetPropertyBlock(block);
        }

        private void OnDisable()
        {
            HidePresentationImmediately();
            ResetCarnival();
            if (manager != null)
            {
                manager.OnSpinCompleted -= OnSpinCompleted;
                manager.OnSpinCancelled -= OnSpinCancelled;
            }

            spinEnergy = 0f;
            winningSlice = -1;
            bossResult = 0f;
            revealEnergy = 0f;
            revealProgress = 0f;

            ApplyShaderProperties();
        }
    }
}
