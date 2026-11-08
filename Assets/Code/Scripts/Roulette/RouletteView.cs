using System;
using System.Collections;
using UnityEngine;

namespace Code.Scripts.Roulette
{
    public class RouletteView : MonoBehaviour
    {
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private Transform _wheel;
        [SerializeField] private MeshRenderer _sectorRenderer;
        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float _spinDuration = 4f;
        [SerializeField, Min(1)] private int _fullTurns = 5;

        private static readonly int BossFraction = Shader.PropertyToID("_BossFraction");
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
            if (_visualRoot == null || _wheel == null || _sectorRenderer == null)
                error = "RouletteView requires its visual root, wheel and sector renderer.";
            else if (_sectorRenderer.sharedMaterial == null ||
                     !_sectorRenderer.sharedMaterial.HasProperty(BossFraction))
                error = "The sector material must use the DiceMaster/Roulette Sectors shader.";
            else if (_spinDuration <= 0 || float.IsNaN(_spinDuration) || float.IsInfinity(_spinDuration) ||
                     _fullTurns < 1 || _fullTurns > 100)
                error = "Spin duration must be finite and positive; full turns must be between 1 and 100.";
            return error == null;
        }

        public void Show(int probability)
        {
            if (!TryValidate(out string error)) throw new InvalidOperationException(error);
            if (!_initialized)
            {
                _initialRotation = _wheel.localRotation;
                _properties = new MaterialPropertyBlock();
                _initialized = true;
            }

            _visualRoot.SetActive(true);
            _sectorRenderer.GetPropertyBlock(_properties);
            _properties.SetFloat(BossFraction, probability / 100f);
            _sectorRenderer.SetPropertyBlock(_properties);
        }

        public void ShowResult(RouletteResult result)
        {
            Show(result.BossProbability);
            SetAngle((float)RouletteRules.GetSectorAngle(result.IsBoss, result.BossProbability, 0.5));
        }

        public void Spin(RouletteResult result)
        {
            if (IsSpinning || !isActiveAndEnabled)
                throw new InvalidOperationException("RouletteView is busy or disabled.");
            Show(result.BossProbability);
            float target = (float)RouletteRules.GetSectorAngle(result.IsBoss,
                result.BossProbability, UnityEngine.Random.value);
            IsSpinning = true;
            _spin = StartCoroutine(Animate(result, target));
        }

        private IEnumerator Animate(RouletteResult result, float target)
        {
            float start = _angle;
            float distance = _fullTurns * 360f + Mathf.Repeat(target - start, 360f);
            float duration = _spinDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!_visualRoot.activeInHierarchy)
                {
                    _spin = null;
                    IsSpinning = false;
                    SpinCancelled?.Invoke();
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float remaining = 1f - t;
                SetAngle(start + distance * (1f - remaining * remaining * remaining));
                yield return null;
            }

            SetAngle(target);
            _spin = null;
            IsSpinning = false;
            // The same angular convention is used by the shader, target selection and this check.
            if (RouletteRules.IsBossAtAngle(_angle, result.BossProbability) != result.IsBoss)
            {
                Debug.LogError("Roulette stopped outside the selected sector.", this);
                SpinCancelled?.Invoke();
                yield break;
            }
            SpinCompleted?.Invoke(result);
        }

        private void SetAngle(float angle)
        {
            _angle = Mathf.Repeat(angle, 360f);
            _wheel.localRotation = _initialRotation * Quaternion.AngleAxis(_angle, Vector3.forward);
        }

        public void Hide()
        {
            CancelSpin();
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        public void CancelSpin()
        {
            bool wasSpinning = IsSpinning;
            if (_spin != null) StopCoroutine(_spin);
            _spin = null;
            IsSpinning = false;
            if (wasSpinning) SpinCancelled?.Invoke();
        }

        private void OnDisable()
        {
            CancelSpin();
        }
    }
}
