using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Code.Scripts.Roulette
{
    public sealed partial class RouletteSectorVFX
    {
        [Header("Drop entrance / retract exit")]
        [SerializeField] private bool animatePresentation = true;
        [SerializeField, Min(1f)] private float retractHeight = 20f;
        [SerializeField, Range(0.2f, 2f)] private float dropDuration = 0.65f;
        [SerializeField, Range(0.2f, 2f)] private float retractDuration = 0.5f;
        [SerializeField, Range(0f, 0.4f)] private float landingBounce = 0.09f;
        [Header("Whole roulette spin shake")]
        [SerializeField, Range(0f, 0.1f)] private float spinShakeAmplitude = 0.025f;
        [SerializeField, Range(2f, 35f)] private float spinShakeFrequency = 18f;
        private bool spinShakeApplied;
        private Transform presentationRoot;
        private Vector3 presentationHome;
        private Tween presentationTween;
        private bool presentationVisible;
        public bool IsPresentationBusy => presentationTween != null && presentationTween.IsActive();

        private bool PreparePresentation()
        {
            if (!animatePresentation || sectors == null || sectors.transform.parent == null) return false;
            if (presentationRoot != null) return true;
            // Move the complete existing model: frame, disc and logical pointer together.
            presentationRoot = sectors.transform.parent.parent;
            if (presentationRoot == null || transform.IsChildOf(presentationRoot))
            {
                presentationRoot = null;
                return false;
            }
            presentationHome = presentationRoot.localPosition;
            return true;
        }

        private Vector3 LocalLift(float height)
        {
            return presentationRoot.parent != null
                ? presentationRoot.parent.InverseTransformVector(Vector3.up * height)
                : Vector3.up * height;
        }

        public void PrepareNumberLayout()
        {
            if (!isActiveAndEnabled || !PreparePresentation() || presentationVisible) return;
            // TMP must initialize in an active hierarchy, still above the visible stage.
            presentationRoot.localPosition = presentationHome + LocalLift(retractHeight);
            presentationRoot.gameObject.SetActive(true);
        }

        public void Present(System.Action completed)
        {
            if (!isActiveAndEnabled || !PreparePresentation()) { completed?.Invoke(); return; }
            if (presentationVisible) { completed?.Invoke(); return; }
            presentationTween?.Kill();
            presentationVisible = true;
            presentationRoot.localPosition = presentationHome + LocalLift(retractHeight);
            presentationRoot.gameObject.SetActive(true);
            var sequence = DOTween.Sequence().SetUpdate(true);
            presentationTween = sequence;
            sequence.Append(presentationRoot.DOLocalMove(presentationHome, dropDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(() =>
            {
                // Reuse the latch impact, warm bulbs and short JMO sparks, without a gameplay result.
                RevealCarnival(false);
            });
            sequence.Append(presentationRoot.DOPunchPosition(LocalLift(-landingBounce), 0.22f, 3, 0.3f));
            sequence.OnComplete(() =>
            {
                presentationRoot.localPosition = presentationHome;
                presentationTween = null;
                completed?.Invoke();
            });
        }

        public void Dismiss(System.Action completed)
        {
            if (!PreparePresentation() || !presentationVisible) { completed?.Invoke(); return; }
            presentationTween?.Kill();
            presentationVisible = false;
            ResetCarnival();
            presentationTween = presentationRoot.DOLocalMove(presentationHome + LocalLift(retractHeight), retractDuration)
                .SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() =>
                {
                    presentationTween = null;
                    presentationRoot.gameObject.SetActive(false);
                    completed?.Invoke();
                });
        }

        public void HidePresentationImmediately()
        {
            presentationTween?.Kill(); presentationTween = null;
            presentationVisible = false;
            if (!PreparePresentation()) return;
            presentationRoot.gameObject.SetActive(false);
            presentationRoot.localPosition = presentationHome + LocalLift(retractHeight);
            OnSpinCancelled();
        }

        [Header("Carnival rim (original Roulette mesh)")]
        [SerializeField] private Shader carnivalShader;
        [SerializeField] private MeshRenderer frame;
        [SerializeField, ColorUsage(false, true)] private Color amber = new Color(1f, 0.42f, 0.07f);
        [SerializeField, ColorUsage(false, true)] private Color bossRed = new Color(1f, 0.025f, 0.01f);
        [SerializeField, Min(0f)] private float idleIntensity = 0.6f;
        [SerializeField, Min(0f)] private float chaseIntensity = 5f;
        [SerializeField, Min(0f)] private float resultIntensity = 7f;
        [Tooltip("Bulbs per second at maximumSpinSpeed; slows with the measured wheel speed.")]
        [SerializeField, Min(0f)] private float chaseSpeed = 22f;
        [SerializeField, Range(1, 15)] private int chaseGroupSize = 3;
        [SerializeField, Min(0.1f)] private float resultFlashDuration = 0.85f;
        [SerializeField, Range(1f, 8f)] private float bossFlashSpeed = 3f;
        [SerializeField, Range(0f, 8f)] private float pointerBounceDegrees = 3f;
        [SerializeField, Range(0f, 1f)] private float fastVibrationDegrees = 0.22f;
        [SerializeField, Range(1f, 3f)] private float brakingTickMultiplier = 1.6f;
        [SerializeField, Range(0f, 8f)] private float stopImpactDegrees = 4f;
        [SerializeField, Range(0.1f, 0.6f)] private float stopImpactDuration = 0.24f;
        [Header("Restrained sparks / Bloom")]
        [SerializeField] private Material sparkMaterial;
        [SerializeField] private ParticleSystem sparkPrefab;
        [SerializeField] private GameObject winnerImpactPrefab;
        [Tooltip("Effect size relative to the original model, independent of scene scale.")]
        [SerializeField, Range(0.0001f, 0.001f)] private float winnerImpactScale = 0.0004f;
        [SerializeField, Range(0.1f, 0.6f)] private float winnerImpactLifetime = 0.3f;
        [Tooltip("Original model-local pointer tip, before its visual bounce.")]
        [SerializeField] private Vector3 sparkTip = new Vector3(0f, 0.000675f, 0.00330f);
        [SerializeField, Range(1, 6)] private int tickSparkCount = 2;
        [SerializeField, Range(4, 24)] private int impactSparkCount = 12;
        [SerializeField, Range(0.005f, 0.1f)] private float sparkSize = 0.025f;
        [SerializeField, Range(0.05f, 0.5f)] private float sparkLifetime = 0.2f;
        [SerializeField, Range(0.1f, 2f)] private float sparkSpeed = 0.7f;
        [SerializeField, Range(0f, 0.5f)] private float bloomIntensity = 0.18f;
        [SerializeField, Min(0.02f)] private float sparkCooldown = 0.06f;

        private Material[] originalFrameMaterials;
        private Material bulbMaterial, pointerMaterial;
        private ParticleSystem sparks;
        private Volume bloomVolume;
        private VolumeProfile bloomProfile;
        private UniversalAdditionalCameraData bloomCamera;
        private bool previousPostProcessing;
        private float chasePhase, resultClock = -1f, bounceClock = 1f, nextSpark;
        private float previousSectorAngle;
        private bool carnivalBoss;
        private readonly System.Random visualRandom = new System.Random();
        private Tween stopTween;
        private float stopBounce, tickStrength, currentBounce;
        private GameObject sparkRoot;
        private GameObject winnerImpactRoot;
        private ParticleSystem[] winnerParticles;
        private float winnerImpactClock = -1f;

        private void InitializeCarnival()
        {
            if (carnivalShader == null) return;
            // The model is the parent of Roulette Presentation, never the rotating Quad.
            if (frame == null && sectors.transform.parent != null)
            {
                Transform model = sectors.transform.parent.parent;
                if (model != null)
                    foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                        if (renderer.sharedMaterials.Length == 7) { frame = renderer; break; }
            }
            if (frame == null || frame.sharedMaterials.Length != 7)
            {
                Debug.LogWarning("[Roulette VFX] Expected the original seven-material roulette model.", this);
                return;
            }
            originalFrameMaterials = frame.sharedMaterials;
            var materials = (Material[])originalFrameMaterials.Clone();
            // Slot 4 contains studs AND the hub: the shader masks only the rim studs.
            bulbMaterial = MakeCarnivalMaterial(materials[4], 0f);
            pointerMaterial = MakeCarnivalMaterial(materials[5], 1f);
            materials[4] = bulbMaterial;
            materials[5] = pointerMaterial;
            frame.sharedMaterials = materials;
            previousSectorAngle = view.GetPointerSectorAngle();
            if (sparkPrefab != null || sparkMaterial != null)
            {
                sparkRoot = new GameObject("Roulette Pointer Sparks");
                sparkRoot.SetActive(false);
                sparkRoot.transform.SetParent(frame.transform, false);
                sparkRoot.transform.localPosition = sparkTip;
                // Particle size and speed are in world units, independent of the FBX scale.
                sparks = sparkPrefab != null
                    ? Instantiate(sparkPrefab, sparkRoot.transform, false)
                    : sparkRoot.AddComponent<ParticleSystem>();
                // Own lifecycle here: no prefab camera shake, auto-destroy or automatic emission.
                foreach (var behaviour in sparks.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                sparks.transform.localPosition = Vector3.zero;
                sparks.transform.localRotation = Quaternion.identity;
                sparks.transform.localScale = Vector3.one;
                sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = sparks.main;
                main.playOnAwake = false; main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.gravityModifier = 0.12f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Shape;
                main.useUnscaledTime = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.035f);
                main.maxParticles = 64;
                var emission = sparks.emission; emission.enabled = false;
                var shape = sparks.shape; shape.enabled = false;
                var velocity = sparks.velocityOverLifetime; velocity.enabled = false;
                var noise = sparks.noise; noise.enabled = false;
                var trails = sparks.trails; trails.enabled = false;
                var subEmitters = sparks.subEmitters; subEmitters.enabled = false;
                var color = sparks.colorOverLifetime; color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;
                var renderer = sparks.GetComponent<ParticleSystemRenderer>();
                if (sparkMaterial != null) renderer.sharedMaterial = sparkMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 1.5f; renderer.velocityScale = 0.04f;
                sparkRoot.SetActive(true);
            }
            ResetCarnival();
            if (winnerImpactPrefab != null)
            {
                winnerImpactRoot = new GameObject("Roulette JMO Winner Impact");
                winnerImpactRoot.SetActive(false);
                winnerImpactRoot.transform.SetParent(frame.transform, false);
                winnerImpactRoot.transform.localPosition = sparkTip;
                var instance = Instantiate(winnerImpactPrefab, winnerImpactRoot.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
                // Reuse the authored star/spikes; discard its expanding ring for this prop.
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    if (child.name == "Ring") child.gameObject.SetActive(false);
                winnerParticles = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var particles in winnerParticles)
                {
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = particles.main;
                    main.loop = false; main.playOnAwake = false;
                    main.stopAction = ParticleSystemStopAction.None;
                    main.useUnscaledTime = true;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    main.startDelay = 0f;
                    main.startLifetime = winnerImpactLifetime;
                    var tint = particles.colorOverLifetime; tint.enabled = true;
                    var fade = new Gradient();
                    fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                    tint.color = fade;
                }
            }
        }

        private Material MakeCarnivalMaterial(Material source, float pointer)
        {
            var material = new Material(carnivalShader) { name = source.name + " (Carnival runtime)" };
            material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            material.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
            material.SetFloat("_PointerMode", pointer);
            return material;
        }

        private void UpdateCarnival(float dt, bool spinning)
        {
            if (bulbMaterial == null) return;
            bool visible = sectors.gameObject.activeInHierarchy;
            SetBloom(visible);
            if (!visible) { ResetCarnival(); return; }
            if (winnerImpactClock >= 0f)
            {
                winnerImpactClock += dt;
                if (winnerImpactClock >= winnerImpactLifetime) ClearWinnerImpact();
            }
            if (spinning && !wasSpinning)
            {
                ClearWinnerImpact();
                stopTween?.Kill(); stopBounce = 0f; tickStrength = 0f;
                if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                resultClock = -1f;
                previousSectorAngle = view.GetPointerSectorAngle();
            }
            float speed = Quaternion.Angle(previousRotation, sectors.transform.rotation) / Mathf.Max(dt, 0.001f);
            UpdateSpinShake(spinning, speed);
            if (spinning)
            {
                chasePhase = Mathf.Repeat(chasePhase + dt * chaseSpeed * Mathf.Clamp01(speed / maximumSpinSpeed), 15f);
                sectors.GetPropertyBlock(block);
                int count = Mathf.Max(1, Mathf.RoundToInt(block.GetFloat("_SliceCount")));
                float angle = view.GetPointerSectorAngle();
                if (Mathf.FloorToInt(angle * count / 360f) != Mathf.FloorToInt(previousSectorAngle * count / 360f))
                {
                    bounceClock = 0f;
                    tickStrength = pointerBounceDegrees * Mathf.Lerp(brakingTickMultiplier, 0.35f, Mathf.Clamp01(speed / maximumSpinSpeed));
                    if (Time.unscaledTime >= nextSpark)
                    {
                        Color tint = angle / 360f < block.GetFloat("_BossFraction") ? bossRed : amber;
                        EmitSparks(tickSparkCount, tint);
                        nextSpark = Time.unscaledTime + sparkCooldown;
                    }
                }
                previousSectorAngle = angle;
            }
            bounceClock += dt;
            float vibration = spinning ? Mathf.Sin(Time.unscaledTime * 110f) * fastVibrationDegrees * Mathf.Clamp01(speed / maximumSpinSpeed) : 0f;
            currentBounce = Mathf.Sin(bounceClock * 65f) * Mathf.Exp(-bounceClock * 22f) * tickStrength + vibration + stopBounce;
            pointerMaterial.SetFloat("_Bounce", currentBounce);
            float reveal = 0f;
            if (resultClock >= 0f)
            {
                resultClock += dt;
                reveal = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((resultClock - 0.08f) / Mathf.Max(0.02f, resultFlashDuration - 0.08f)));
                if (carnivalBoss) reveal *= 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Cos(resultClock * bossFlashSpeed * Mathf.PI * 2f));
                if (resultClock >= resultFlashDuration) resultClock = -1f;
            }
            float facing = Camera.main != null && frame.transform.InverseTransformPoint(Camera.main.transform.position).z > 0f ? -1f : 1f;
            bulbMaterial.SetFloat("_Facing", facing);
            bulbMaterial.SetFloat("_ChasePhase", chasePhase);
            bulbMaterial.SetFloat("_ChaseSize", chaseGroupSize);
            bulbMaterial.SetFloat("_Chasing", spinning ? 1f : 0f);
            bulbMaterial.SetFloat("_Idle", idleIntensity);
            bulbMaterial.SetFloat("_Intensity", chaseIntensity);
            bulbMaterial.SetFloat("_Reveal", reveal * resultIntensity);
            bulbMaterial.SetColor("_GlowColor", resultClock >= 0f && carnivalBoss ? bossRed : amber);
        }

        private void UpdateSpinShake(bool spinning, float speed)
        {
            if (presentationRoot == null || !presentationVisible || IsPresentationBusy) return;
            if (!spinning)
            {
                if (spinShakeApplied) presentationRoot.localPosition = presentationHome;
                spinShakeApplied = false;
                return;
            }

            float amplitude = spinShakeAmplitude * Mathf.Clamp01(speed / maximumSpinSpeed);
            float phase = Time.unscaledTime * spinShakeFrequency * Mathf.PI * 2f;
            // Translation in the face plane only: wheel and pointer keep their relative alignment.
            Vector3 worldOffset = presentationRoot.right * (Mathf.Sin(phase) * amplitude)
                + presentationRoot.up * (Mathf.Sin(phase * 1.37f) * amplitude * 0.55f);
            Vector3 localOffset = presentationRoot.parent != null
                ? presentationRoot.parent.InverseTransformVector(worldOffset) : worldOffset;
            presentationRoot.localPosition = presentationHome + localOffset;
            spinShakeApplied = true;
        }

        private void EmitSparks(int count, Color tint)
        {
            if (sparks == null) return;
            Vector3 pivot = new Vector3(0f, 0.00267497f, sparkTip.z);
            Vector3 tip = pivot + Quaternion.AngleAxis(currentBounce, Vector3.forward) * (sparkTip - pivot);
            sparkRoot.transform.position = frame.transform.TransformPoint(tip);
            if (!sparks.isPlaying) sparks.Play();
            for (int i = 0; i < count; i++)
            {
                var emit = new ParticleSystem.EmitParams();
                emit.position = sparks.transform.position;
                emit.velocity = sparkSpeed * frame.transform.TransformDirection(new Vector3(
                    (float)visualRandom.NextDouble() * 1.4f - 0.7f,
                    (float)visualRandom.NextDouble() - 0.8f, 0.2f));
                emit.startSize = sparkSize * (0.7f + (float)visualRandom.NextDouble() * 0.6f);
                emit.startLifetime = sparkLifetime * (0.65f + (float)visualRandom.NextDouble() * 0.7f);
                emit.startColor = new Color(tint.r, tint.g, tint.b, 1f);
                sparks.Emit(emit, 1);
            }
        }

        private void RevealCarnival(bool boss)
        {
            carnivalBoss = boss; resultClock = 0f;
            stopTween?.Kill();
            tickStrength = 0f;
            stopTween = DOVirtual.Float(0f, 1f, stopImpactDuration,
                t => stopBounce = Mathf.Sin(t * Mathf.PI * 4f) * Mathf.Pow(1f - t, 2f) * stopImpactDegrees)
                .SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => stopBounce = 0f);
            EmitSparks(impactSparkCount, boss ? bossRed : amber);
            if (winnerImpactRoot != null)
            {
                ClearWinnerImpact();
                winnerImpactRoot.transform.localPosition = sparkTip;
                winnerImpactRoot.transform.localScale = Vector3.one * winnerImpactScale;
                winnerImpactRoot.SetActive(true);
                foreach (var particles in winnerParticles)
                {
                    if (!particles.gameObject.activeInHierarchy) continue;
                    var main = particles.main;
                    main.startColor = boss ? bossRed : amber;
                    main.startLifetime = winnerImpactLifetime;
                    particles.Play(false);
                }
                winnerImpactClock = 0f;
            }
        }

        private void ClearWinnerImpact()
        {
            if (winnerParticles != null)
                foreach (var particles in winnerParticles)
                    if (particles != null) particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (winnerImpactRoot != null) winnerImpactRoot.SetActive(false);
            winnerImpactClock = -1f;
        }

        private void ResetCarnival()
        {
            UpdateSpinShake(false, 0f);
            spinShakeApplied = false;
            ClearWinnerImpact();
            resultClock = -1f; bounceClock = 1f;
            stopTween?.Kill(); stopTween = null;
            stopBounce = 0f; tickStrength = 0f; currentBounce = 0f; nextSpark = 0f;
            if (bulbMaterial != null)
            {
                bulbMaterial.SetFloat("_Chasing", 0f); bulbMaterial.SetFloat("_Reveal", 0f);
                bulbMaterial.SetFloat("_Idle", idleIntensity); bulbMaterial.SetColor("_GlowColor", amber);
                pointerMaterial.SetFloat("_Bounce", 0f);
            }
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetBloom(false);
        }

        private void SetBloom(bool active)
        {
            if (active && bloomVolume == null && Camera.main != null)
            {
                bloomCamera = Camera.main.GetUniversalAdditionalCameraData();
                previousPostProcessing = bloomCamera.renderPostProcessing;
                var obj = new GameObject("Roulette subtle Bloom");
                obj.transform.SetParent(transform, false);
                int mask = bloomCamera.volumeLayerMask.value;
                for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) { obj.layer = i; break; }
                bloomVolume = obj.AddComponent<Volume>();
                bloomVolume.isGlobal = true; bloomVolume.priority = 10f;
                bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = bloomProfile.Add<Bloom>();
                bloom.intensity.Override(bloomIntensity); bloom.threshold.Override(1.2f); bloom.scatter.Override(0.45f);
                bloomVolume.sharedProfile = bloomProfile;
            }
            if (bloomVolume != null) bloomVolume.enabled = active;
            if (bloomCamera != null) bloomCamera.renderPostProcessing = active || previousPostProcessing;
        }

        private void OnDestroy()
        {
            presentationTween?.Kill();
            if (presentationRoot != null) presentationRoot.localPosition = presentationHome;
            stopTween?.Kill();
            SetBloom(false);
            if (frame != null && originalFrameMaterials != null) frame.sharedMaterials = originalFrameMaterials;
            if (bulbMaterial != null) Destroy(bulbMaterial);
            if (pointerMaterial != null) Destroy(pointerMaterial);
            if (sparkRoot != null) Destroy(sparkRoot);
            if (winnerImpactRoot != null) Destroy(winnerImpactRoot);
            if (bloomVolume != null) Destroy(bloomVolume.gameObject);
            if (bloomProfile != null)
            {
                foreach (var component in bloomProfile.components) Destroy(component);
                Destroy(bloomProfile);
            }
        }
    }
}
