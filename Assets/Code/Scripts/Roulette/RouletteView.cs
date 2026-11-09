
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Code.Scripts.Roulette
{
    public class RouletteView : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _wheel;
        [SerializeField] private MeshRenderer _sectorRenderer;
        [SerializeField] private Transform _pointer;

        [Header("Spin Animation")]
        [SerializeField, Min(0.1f)]
        private float _spinDuration = 4f;

        [SerializeField, Range(1, 100)]
        private int _fullTurns = 5;

        [Header("Number Placement")]
        [Tooltip("Inner limit of the numbered ring, relative to the Quad radius.")]
        [SerializeField, Range(0f, 0.9f)]
        private float _numberRingInnerRadius = 0.32f;

        [Tooltip("Outer limit of the numbered ring, relative to the Quad radius.")]
        [SerializeField, Range(0.1f, 1f)]
        private float _numberRingOuterRadius = 0.94f;

        [Tooltip("Moves numbers towards the wider outer part of each wedge.")]
        [SerializeField, Range(0f, 0.6f)]
        private float _numberOutwardOffset = 0.35f;

        [Tooltip("Small world-space gap between numbers and the wheel surface.")]
        [SerializeField, Min(0.000001f)]
        private float _numberSurfaceGap = 0.00002f;

        [SerializeField]
        private Color _labelColor =
            new Color(1f, 0.91f, 0.70f, 1f);

        [SerializeField]
        private TMP_FontAsset _numberFont;

        [Header("Number Size")]
        [SerializeField, Min(1f)]
        private float _baseFontSize = 36f;

        [SerializeField, Range(0.1f, 1f)]
        private float _numberSizeMultiplier = 0.65f;

        private static readonly int BossFraction =
            Shader.PropertyToID("_BossFraction");

        private static readonly int SliceCount =
            Shader.PropertyToID("_SliceCount");

        private readonly List<TextMeshPro> _labels =
            new List<TextMeshPro>();

        private readonly List<int> _displayedNumbers =
            new List<int>();

        private MaterialPropertyBlock _properties;
        private Quaternion _initialRotation;
        private Coroutine _spin;

        private float _angle;
        private bool _initialized;

        public bool IsSpinning { get; private set; }

        public event Action<RouletteResult> SpinCompleted;
        public event Action SpinCancelled;

        public bool TryValidate(out string error)
        {
            error = null;

            if (_visualRoot == null ||
                _wheel == null ||
                _sectorRenderer == null)
            {
                error = "RouletteView: missing visual references.";
                return false;
            }

            if (_pointer == null || _pointer.IsChildOf(_wheel))
            {
                error = "RouletteView: pointer must be outside the rotating wheel.";
                return false;
            }

            if (_sectorRenderer.transform != _wheel)
            {
                error = "RouletteView: sector renderer must be on the rotating wheel.";
                return false;
            }

            if (_sectorRenderer.sharedMaterial == null ||
                !_sectorRenderer.sharedMaterial.HasProperty(BossFraction) ||
                !_sectorRenderer.sharedMaterial.HasProperty(SliceCount))
            {
                error = "RouletteView: invalid roulette sector material.";
                return false;
            }

            MeshFilter filter =
                _sectorRenderer.GetComponent<MeshFilter>();

            if (filter == null || filter.sharedMesh == null)
            {
                error = "RouletteView: missing Quad mesh.";
                return false;
            }

            if (_spinDuration <= 0f ||
                float.IsNaN(_spinDuration) ||
                float.IsInfinity(_spinDuration) ||
                _fullTurns < 1 ||
                _fullTurns > 100)
            {
                error = "RouletteView: invalid spin settings.";
                return false;
            }

            if (_numberRingOuterRadius <= _numberRingInnerRadius)
            {
                error = "RouletteView: outer radius must exceed inner radius.";
                return false;
            }

            return true;
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            _initialRotation = _wheel.localRotation;
            _properties = new MaterialPropertyBlock();
            _initialized = true;
        }

        public void Show(int probability, IReadOnlyList<int> numbers)
        {
            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);

            if (numbers == null ||
                numbers.Count < 1 ||
                numbers.Count > 20)
            {
                throw new ArgumentException(
                    "Roulette requires between 1 and 20 numbers.",
                    nameof(numbers)
                );
            }

            Initialize();
            _visualRoot.SetActive(true);

            _sectorRenderer.GetPropertyBlock(_properties);
            _properties.SetFloat(BossFraction, probability / 100f);
            _properties.SetFloat(SliceCount, numbers.Count);
            _sectorRenderer.SetPropertyBlock(_properties);

            RefreshLabels(numbers);
        }

        private void RefreshLabels(IReadOnlyList<int> numbers)
        {
            bool unchanged =
                _displayedNumbers.Count == numbers.Count &&
                _labels.Count == numbers.Count;

            if (unchanged)
            {
                for (int i = 0; i < numbers.Count; i++)
                {
                    if (_displayedNumbers[i] != numbers[i] ||
                        _labels[i] == null)
                    {
                        unchanged = false;
                        break;
                    }
                }
            }

            if (unchanged)
                return;

            ClearLabels();

            MeshFilter filter =
                _sectorRenderer.GetComponent<MeshFilter>();

            Bounds bounds = filter.sharedMesh.bounds;

            float radius = Mathf.Min(
                bounds.extents.x,
                bounds.extents.y
            );

            float sliceAngle = 360f / numbers.Count;
            float halfAngle = Mathf.PI / numbers.Count;

            Transform disc = _sectorRenderer.transform;

            float innerRadius =
                radius * _numberRingInnerRadius;

            float outerRadius =
                radius * _numberRingOuterRadius;

            // Calculate the centroid of the usable annular sector.
            float innerSquared = innerRadius * innerRadius;
            float outerSquared = outerRadius * outerRadius;

            float centroidRadius =
                (2f / 3f) *
                (outerRadius * outerSquared -
                 innerRadius * innerSquared) /
                Mathf.Max(
                    outerSquared - innerSquared,
                    0.000001f
                );

            centroidRadius *=
                Mathf.Sin(halfAngle) /
                Mathf.Max(halfAngle, 0.000001f);

            // Move the number towards the wider outer area
            // without changing its angular position.
            centroidRadius = Mathf.Lerp(
                centroidRadius,
                outerRadius,
                _numberOutwardOffset
            );

            Camera camera = Camera.main;

            Vector3 cameraLocal = camera != null
                ? disc.InverseTransformPoint(camera.transform.position)
                : bounds.center + Vector3.back;

            bool positiveZ =
                cameraLocal.z >= bounds.center.z;

            float worldScaleZ =
                disc.TransformVector(Vector3.forward).magnitude;

            float localGap =
                _numberSurfaceGap /
                Mathf.Max(worldScaleZ, 0.000001f);

            float surfaceZ =
                bounds.center.z +
                (positiveZ ? 1f : -1f) * localGap;

            // All numbers use one common scale so that
            // single-digit and double-digit characters match.
            float availableHeight =
                (outerRadius - innerRadius) * 0.60f;

            float desiredTextHeight =
                availableHeight * _numberSizeMultiplier;

            float commonScale = -1f;

            for (int i = 0; i < numbers.Count; i++)
            {
                float angleDegrees =
                    (i + 0.5f) * sliceAngle;

                float angleRadians =
                    angleDegrees * Mathf.Deg2Rad;

                GameObject obj = new GameObject(
                    "Lucky Number " + numbers[i]
                );

                obj.transform.SetParent(disc, false);

                obj.transform.localPosition =
                    new Vector3(
                        bounds.center.x +
                        Mathf.Sin(angleRadians) * centroidRadius,

                        bounds.center.y +
                        Mathf.Cos(angleRadians) * centroidRadius,

                        surfaceZ
                    );

                // Radial orientation:
                // numbers at the bottom remain upside down.
                Quaternion radialRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        -angleDegrees
                    );

                // Facing correction for the current scene
                // to prevent mirrored numbers.
                Quaternion facingRotation =
                    positiveZ
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.identity;

                obj.transform.localRotation =
                    radialRotation * facingRotation;

                TextMeshPro label =
                    obj.AddComponent<TextMeshPro>();

                if (_numberFont != null)
                    label.font = _numberFont;

                label.text = numbers[i].ToString();
                label.alignment = TextAlignmentOptions.Center;
                label.color = _labelColor;
                label.fontSize = _baseFontSize;
                label.fontStyle = FontStyles.Bold;

                label.enableAutoSizing = false;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Overflow;

                RectTransform rect =
                    label.GetComponent<RectTransform>();

                rect.sizeDelta = new Vector2(4f, 2f);

                label.ForceMeshUpdate();

                if (commonScale < 0f)
                {
                    float referenceHeight = Mathf.Max(
                        label.textBounds.size.y,
                        0.0001f
                    );

                    commonScale =
                        desiredTextHeight / referenceHeight;
                }

                obj.transform.localScale =
                    Vector3.one * commonScale;

                _labels.Add(label);
                _displayedNumbers.Add(numbers[i]);
            }
        }

        private void ClearLabels()
        {
            foreach (TextMeshPro label in _labels)
            {
                if (label == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(label.gameObject);
                else
                    DestroyImmediate(label.gameObject);
            }

            _labels.Clear();
            _displayedNumbers.Clear();
        }

        public void ShowResult(RouletteResult result)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "Show the roulette before displaying a result."
                );
            }

            ValidateResult(result);

            SetAngle(
                CalculateTargetRotation(result.StopAngle)
            );
        }

        public void Spin(RouletteResult result)
        {
            if (IsSpinning ||
                !isActiveAndEnabled ||
                !_initialized)
            {
                throw new InvalidOperationException(
                    "Roulette is busy or not prepared."
                );
            }

            ValidateResult(result);

            float target =
                CalculateTargetRotation(result.StopAngle);

            IsSpinning = true;
            _spin = StartCoroutine(Animate(result, target));
        }

        private void ValidateResult(RouletteResult result)
        {
            if (result.SliceIndex < 0 ||
                result.SliceIndex >= _displayedNumbers.Count ||
                _displayedNumbers[result.SliceIndex] != result.LuckyNumber)
            {
                throw new InvalidOperationException(
                    "Roulette result does not match displayed numbers."
                );
            }
        }

        private float CalculateTargetRotation(double stopAngle)
        {
            Vector3 pointerLocal =
                _wheel.InverseTransformPoint(_pointer.position);

            Vector3 originalDirection =
                Quaternion.AngleAxis(_angle, Vector3.forward) *
                pointerLocal;

            float pointerAngle =
                Mathf.Atan2(
                    originalDirection.x,
                    originalDirection.y
                ) * Mathf.Rad2Deg;

            return Mathf.Repeat(
                (float)stopAngle - pointerAngle,
                360f
            );
        }

        private IEnumerator Animate(RouletteResult result, float target)
        {
            float start = _angle;

            float distance =
                _fullTurns * 360f +
                Mathf.Repeat(target - start, 360f);

            float elapsed = 0f;

            while (elapsed < _spinDuration)
            {
                if (!_visualRoot.activeInHierarchy)
                {
                    _spin = null;
                    IsSpinning = false;
                    SpinCancelled?.Invoke();
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / _spinDuration);
                float remaining = 1f - t;

                float eased =
                    1f - remaining * remaining * remaining;

                SetAngle(start + distance * eased);
                yield return null;
            }

            SetAngle(target);

            _spin = null;
            IsSpinning = false;

            float actualAngle = GetPointerSectorAngle();

            int actualIndex =
                RouletteRules.GetSliceIndex(
                    actualAngle,
                    _displayedNumbers.Count
                );

            bool actualBoss =
                RouletteRules.IsBossAtAngle(
                    actualAngle,
                    result.BossProbability
                );

            if (actualIndex != result.SliceIndex ||
                actualBoss != result.IsBoss)
            {
                Debug.LogError(
                    "Roulette pointer mismatch. " +
                    $"Expected angle: {result.StopAngle:F3}, " +
                    $"Actual angle: {actualAngle:F3}, " +
                    $"Expected slice: {result.SliceIndex}, " +
                    $"Actual slice: {actualIndex}.",
                    this
                );

                SpinCancelled?.Invoke();
                yield break;
            }

            SpinCompleted?.Invoke(result);
        }

        private void SetAngle(float angle)
        {
            _angle = Mathf.Repeat(angle, 360f);

            _wheel.localRotation =
                _initialRotation *
                Quaternion.AngleAxis(
                    _angle,
                    Vector3.forward
                );
        }

        public float GetPointerSectorAngle()
        {
            Vector3 pointer =
                _wheel.InverseTransformPoint(_pointer.position);

            return Mathf.Repeat(
                Mathf.Atan2(pointer.x, pointer.y) * Mathf.Rad2Deg,
                360f
            );
        }

        public void Hide()
        {
            CancelSpin();

            if (_visualRoot != null)
                _visualRoot.SetActive(false);
        }

        public void CancelSpin()
        {
            bool wasSpinning = IsSpinning;

            if (_spin != null)
                StopCoroutine(_spin);

            _spin = null;
            IsSpinning = false;

            if (wasSpinning)
                SpinCancelled?.Invoke();
        }

        private void OnDisable()
        {
            CancelSpin();
        }
    }
}
