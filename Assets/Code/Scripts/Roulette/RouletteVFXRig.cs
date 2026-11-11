
using UnityEngine;
using UnityEngine.Rendering;

namespace Code.Scripts.Roulette
{
    // Prefab-owned visual rig. Driven by the existing VFX controller, never by gameplay RNG.
    public sealed class RouletteVFXRig : MonoBehaviour
    {
        [Header("Existing model references")]
        [SerializeField] private Transform wheel;
        [SerializeField] private Shader trailShader;

        [Header("Pointer spring")]
        [SerializeField, Range(4f, 18f)] private float springFrequency = 10f;
        [SerializeField, Range(0.2f, 0.9f)] private float springDamping = 0.42f;
        [SerializeField, Range(1f, 8f)] private float pointerLimit = 6f;

        [Header("Mechanical weight")]
        [SerializeField, Range(0f, 0.06f)] private float startKick = 0.025f;
        [SerializeField, Range(0f, 0.04f)] private float stopRecoil = 0.015f;
        [SerializeField, Range(0f, 0.4f)] private float wobbleDegrees = 0.12f;

        [Header("One shadowless theatrical light")]
        [SerializeField, Range(0f, 5f)] private float spinLightIntensity = 1.4f;
        [SerializeField, Range(0f, 6f)] private float resultLightIntensity = 2.5f;
        [SerializeField, Range(0f, 0.25f)] private float contactLightPulse = 0.08f;

        [Header("Three short rim streaks")]
        [SerializeField, Range(0f, 1f)] private float trailIntensity = 0.45f;
        [SerializeField, Range(5f, 25f)] private float trailArcDegrees = 16f;
        [SerializeField, Range(0.001f, 0.01f)] private float trailWidthRatio = 0.003f;
        [SerializeField, Range(0f, 15f)] private float rimSparksPerSecond = 5f;
        [SerializeField, Range(1f, 6f)] private float trailEmission = 3f;

        private const float RimRadius = 0.01055f;
        private static readonly Vector3 Centre = new Vector3(0f, -0.00884f, 0.00355f);

        private readonly Vector3[] points = new Vector3[18];
        private readonly LineRenderer[] arcs = new LineRenderer[3];

        private Material trailMaterial;
        private Light accent;

        private float travel, startAngle, springPosition, springVelocity, spinClock, flashClock = -1f;
        private float speedEnergy, pulse, sparkBudget;
        private bool boss, winner;
        private Vector3 winnerLocal;

        public float PointerAngle => springPosition;
        public float Energy => speedEnergy;

        public float Wobble =>
            wobbleDegrees * speedEnergy * Mathf.Sin(spinClock * 8f);

        public float Recoil =>
            -startKick * Mathf.Sin(spinClock * 36f) * Mathf.Exp(-spinClock * 16f)
            + (flashClock >= 0f
                ? stopRecoil * Mathf.Sin(flashClock * 30f) * Mathf.Exp(-flashClock * 14f)
                : 0f);

        public Vector3 WinnerWorld => transform.TransformPoint(winnerLocal);

        public void Initialize()
        {
            if (accent != null || wheel == null) return;

            var obj = new GameObject("Roulette Accent Light");
            obj.transform.SetParent(transform, false);

            accent = obj.AddComponent<Light>();
            accent.type = LightType.Spot;
            accent.shadows = LightShadows.None;
            accent.renderMode = LightRenderMode.Auto;
            accent.intensity = 0f;
            accent.enabled = false;

            if (trailShader == null) return;

            trailMaterial = new Material(trailShader)
            {
                name = "Roulette Rim Streaks (runtime)"
            };

            for (int i = 0; i < arcs.Length; i++)
            {
                var arc = new GameObject("Rim Streak " + (i + 1));
                arc.transform.SetParent(transform, false);

                var line = arc.AddComponent<LineRenderer>();

                line.sharedMaterial = trailMaterial;
                line.useWorldSpace = false;
                line.positionCount = points.Length;
                line.numCapVertices = 2;

                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;

                line.enabled = false;
                arcs[i] = line;
            }

            Debug.Log(
                "[Roulette VFX] Premium activo: muelle, luz y 3 estelas inicializados en "
                + gameObject.scene.name + ".",
                this
            );
        }

        public void Begin(float pointerAngle)
        {
            Clear();
            startAngle = pointerAngle;
        }

        public float Tick(
            float dt,
            float currentTravel,
            bool spinning,
            int slices,
            float maxSpeed,
            float impact,
            Color gold,
            Color red,
            out int crossings
        )
        {
            dt = Mathf.Max(0f, dt);

            float delta = spinning
                ? Mathf.Max(0f, currentTravel - travel)
                : 0f;

            float speed = dt > 0f ? delta / dt : 0f;
            float step = 360f / Mathf.Max(1, slices);

            float from = startAngle + travel;
            float to = from + delta;

            crossings = Mathf.Max(
                0,
                Mathf.FloorToInt(to / step) - Mathf.FloorToInt(from / step)
            );

            float used = 0f;

            // Integrate each contact at its position within the observed frame interval.
            for (int i = 0; i < crossings; i++)
            {
                float boundary =
                    (Mathf.Floor(from / step) + i + 1) * step;

                float contactTime = delta > 0f
                    ? (boundary - from) / delta * dt
                    : 0f;

                AdvanceSpring(Mathf.Max(0f, contactTime - used));

                springVelocity = Mathf.Min(
                    springVelocity + impact * springFrequency * Mathf.PI * 2f,
                    pointerLimit * springFrequency * Mathf.PI * 2f
                );

                used = contactTime;
            }

            AdvanceSpring(Mathf.Max(0f, dt - used));

            if (spinning)
                travel = currentTravel;

            if (crossings > 0)
                pulse = 1f;

            pulse *= Mathf.Exp(-dt * 22f);

            if (spinning)
                spinClock += dt;

            float target = spinning
                ? Mathf.Clamp01(speed / Mathf.Max(100f, maxSpeed))
                : 0f;

            speedEnergy = Mathf.Lerp(
                speedEnergy,
                target,
                1f - Mathf.Exp(-dt * 12f)
            );

            if (flashClock >= 0f)
            {
                flashClock += dt;

                if (flashClock > 1f)
                    flashClock = -1f;
            }

            UpdateLight(gold, red);
            UpdateTrails(gold, spinning);

            sparkBudget += dt * rimSparksPerSecond *
                Mathf.SmoothStep(0f, 1f, speedEnergy);

            return speed;
        }

        private void AdvanceSpring(float dt)
        {
            float omega = springFrequency * Mathf.PI * 2f;
            float decay = springDamping * omega;

            float damped = omega * Mathf.Sqrt(
                1f - springDamping * springDamping
            );

            float sn = Mathf.Sin(damped * dt);
            float cs = Mathf.Cos(damped * dt);
            float exp = Mathf.Exp(-decay * dt);

            float next = exp * (
                springPosition * cs
                + (springVelocity + decay * springPosition) / damped * sn
            );

            springVelocity = exp * (
                springVelocity * cs
                - (decay * springVelocity + omega * omega * springPosition) / damped * sn
            );

            springPosition = Mathf.Clamp(
                next,
                -pointerLimit,
                pointerLimit
            );

            if (Mathf.Abs(next) > pointerLimit)
                springVelocity *= 0.25f;
        }

        public void Reveal(
            int slice,
            int slices,
            bool isBoss,
            float impulse
        )
        {
            boss = isBoss;
            winner = slice >= 0;
            flashClock = 0f;

            springVelocity +=
                impulse * springFrequency * Mathf.PI * 2f;

            // Fixed location of the already-stopped winning wedge,
            // below the number ring.
            float angle =
                (slice + 0.5f) * Mathf.PI * 2f
                / Mathf.Max(1, slices);

            winnerLocal = winner && wheel != null
                ? transform.InverseTransformPoint(
                    wheel.TransformPoint(
                        new Vector3(
                            Mathf.Sin(angle) * 0.25f,
                            Mathf.Cos(angle) * 0.25f,
                            0f
                        )
                    )
                )
                : new Vector3(0f, 0.000675f, 0.00355f);

            winnerLocal.z = 0.00355f;
        }

        private void UpdateLight(Color gold, Color red)
        {
            if (accent == null) return;

            float flash = flashClock >= 0f
                ? Mathf.Pow(1f - flashClock, 3f)
                : 0f;

            accent.intensity =
                speedEnergy * spinLightIntensity *
                (1f + pulse * contactLightPulse)
                + flash * resultLightIntensity *
                (boss ? 1.15f : 1f);

            accent.color =
                boss && flash > 0f ? red : gold;

            Vector3 target =
                winner && flash > 0f ? winnerLocal : Centre;

            accent.transform.localPosition =
                target + Vector3.forward *
                (winner ? 0.008f : 0.013f);

            accent.transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);

            accent.spotAngle =
                winner && flash > 0f ? 38f : 85f;

            accent.innerSpotAngle =
                accent.spotAngle * 0.55f;

            accent.range =
                transform.TransformVector(
                    Vector3.forward * 0.03f
                ).magnitude;

            accent.enabled =
                accent.intensity > 0.005f;
        }

        private void UpdateTrails(Color gold, bool spinning)
        {
            if (trailMaterial != null)
                trailMaterial.SetFloat("_Emission", trailEmission);

            float alpha = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.18f, 0.75f, speedEnergy)
            ) * trailIntensity;

            for (int i = 0; i < arcs.Length; i++)
            {
                var line = arcs[i];

                if (line == null)
                    continue;

                float head = Mathf.Repeat(
                    -travel + i * 120f + 35f,
                    360f
                );

                bool nearPointer =
                    Mathf.Abs(
                        Mathf.DeltaAngle(
                            head + trailArcDegrees * 0.5f,
                            0f
                        )
                    ) < 15f + trailArcDegrees * 0.5f;

                line.enabled =
                    alpha > 0.002f
                    && !nearPointer
                    && (spinning || speedEnergy > 0.35f);

                if (!line.enabled)
                    continue;

                for (int j = 0; j < points.Length; j++)
                {
                    float angle = (
                        head + trailArcDegrees * j
                        / (points.Length - 1f)
                    ) * Mathf.Deg2Rad;

                    points[j] = Centre + new Vector3(
                        Mathf.Sin(angle),
                        Mathf.Cos(angle),
                        0f
                    ) * RimRadius;
                }

                line.SetPositions(points);

                line.widthMultiplier =
                    transform.TransformVector(
                        Vector3.right * RimRadius
                    ).magnitude * trailWidthRatio;

                // HDR emission keeps the streaks readable
                // even against the bright environment.
                line.startColor = new Color(
                    gold.r, gold.g, gold.b, alpha
                );

                line.endColor = new Color(
                    gold.r, gold.g, gold.b, 0f
                );
            }
        }

        public bool TakeRimSpark(out Vector3 position)
        {
            float angle = Mathf.Repeat(
                -travel + 135f,
                360f
            ) * Mathf.Deg2Rad;

            position = transform.TransformPoint(
                Centre + new Vector3(
                    Mathf.Sin(angle),
                    Mathf.Cos(angle),
                    0f
                ) * RimRadius
            );

            if (sparkBudget < 1f || speedEnergy < 0.35f)
                return false;

            sparkBudget = Mathf.Min(
                sparkBudget - 1f,
                1f
            );

            return true;
        }

        public Vector3 LandingWorld =>
            transform.TransformPoint(
                Centre + Vector3.down * 0.0105f
            );

        public void Clear()
        {
            travel = 0f;
            springPosition = 0f;
            springVelocity = 0f;
            spinClock = 0f;

            speedEnergy = 0f;
            pulse = 0f;
            sparkBudget = 0f;
            flashClock = -1f;

            winner = false;
            boss = false;

            if (accent != null)
            {
                accent.intensity = 0f;
                accent.enabled = false;
            }

            foreach (var line in arcs)
            {
                if (line != null)
                    line.enabled = false;
            }
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            if (trailMaterial != null)
                Destroy(trailMaterial);
        }
    }
}
