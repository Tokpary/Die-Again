
using System;
using System.Collections.Generic;
using Code.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Code.Scripts.Roulette
{
    public class RouletteManager : MonoBehaviour
    {
        [SerializeField] private RouletteSO _settings;
        [SerializeField] private RouletteView _view;
        [SerializeField] private TMP_Text _luckyNumberText;
        [SerializeField] private TMP_Text[] _luckyNumberRangeTexts = new TMP_Text[6];
        [SerializeField] private Color _winningRangeColor = Color.green;
        private Color _luckyNumberDefaultColor;
        private Color[] _luckyNumberRangeDefaultColors;
        private bool _hasLuckyNumberDefaultColor;
        private readonly System.Random _random = new System.Random();
        private readonly List<RaycastResult> _pointerHits = new List<RaycastResult>();
        private PointerEventData _pointerEvent;
        private EventSystem _pointerEventSystem;
        public event Action<RouletteResult> OnSpinCompleted;
        public event Action OnSpinCancelled;
        public event Action SpinRequested;
        public bool IsSpinning => _view != null && _view.IsSpinning;

        private void Awake()
        {
            CacheDefaultColors();
            if (_view == null) { Debug.LogError("RouletteView is missing.", this); return; }
            _view.SpinCompleted += HandleSpinCompleted;
            _view.SpinCancelled += HandleSpinCancelled;
            _view.Hide();
        }

        private void CacheDefaultColors()
        {
            if (_luckyNumberText != null)
            {
                _luckyNumberDefaultColor = _luckyNumberText.color;
                _hasLuckyNumberDefaultColor = true;
            }

            if (_luckyNumberRangeTexts == null) return;

            _luckyNumberRangeDefaultColors = new Color[_luckyNumberRangeTexts.Length];
            for (int i = 0; i < _luckyNumberRangeTexts.Length; i++)
                if (_luckyNumberRangeTexts[i] != null)
                    _luckyNumberRangeDefaultColors[i] = _luckyNumberRangeTexts[i].color;
        }

        private void HandleSpinCompleted(RouletteResult result)
        {
            SetLuckyNumberText(result);
            OnSpinCompleted?.Invoke(result);
        }

        private void HandleSpinCancelled() => OnSpinCancelled?.Invoke();

        private void SetLuckyNumberText(RouletteResult result)
        {
            if (_luckyNumberText != null)
                _luckyNumberText.text = result.LuckyNumber.ToString();
        }

        public void ShowWinningRange(int minimumScore, int maximumScore)
        {
            if (minimumScore > maximumScore)
            {
                Debug.LogError("The winning range minimum cannot be greater than its maximum.", this);
                return;
            }

            if (_luckyNumberRangeTexts == null || _luckyNumberRangeTexts.Length != 6)
            {
                Debug.LogError("Assign exactly six TMP text references for the lucky number range.", this);
                return;
            }

            string rangeText = $"{minimumScore}-{maximumScore}";
            for (int i = 0; i < _luckyNumberRangeTexts.Length; i++)
                SetWinningRangeText(i, rangeText);

            SetWinningRangeHighlighted(false);
        }

        public void ClearRoundDisplay()
        {
            if (_luckyNumberText != null)
            {
                _luckyNumberText.text = string.Empty;
                if (_hasLuckyNumberDefaultColor)
                    _luckyNumberText.color = _luckyNumberDefaultColor;
            }
            else
            {
                Debug.LogError("Lucky Number TMP reference is not assigned.", this);
            }

            if (_luckyNumberRangeTexts == null ||
                _luckyNumberRangeDefaultColors == null ||
                _luckyNumberRangeTexts.Length != _luckyNumberRangeDefaultColors.Length)
            {
                Debug.LogError("Lucky number range TMP references are not initialized.", this);
                return;
            }

            for (int i = 0; i < _luckyNumberRangeTexts.Length; i++)
            {
                if (_luckyNumberRangeTexts[i] == null)
                {
                    Debug.LogError($"Lucky number range TMP reference {i + 1} is not assigned.", this);
                    continue;
                }

                _luckyNumberRangeTexts[i].text = string.Empty;
                _luckyNumberRangeTexts[i].color = _luckyNumberRangeDefaultColors[i];
            }
        }

        private void SetWinningRangeText(int index, string text)
        {
            if (_luckyNumberRangeTexts[index] == null)
            {
                Debug.LogError($"Lucky number range TMP reference {index + 1} is not assigned.", this);
                return;
            }

            _luckyNumberRangeTexts[index].text = text;
        }

        public void SetWinningRangeHighlighted(bool highlighted)
        {
            if (_luckyNumberText == null || !_hasLuckyNumberDefaultColor)
                Debug.LogError("Lucky Number TMP reference is not assigned.", this);
            else
                _luckyNumberText.color = highlighted ? _winningRangeColor : _luckyNumberDefaultColor;

            if (_luckyNumberRangeTexts == null ||
                _luckyNumberRangeDefaultColors == null ||
                _luckyNumberRangeTexts.Length != _luckyNumberRangeDefaultColors.Length)
            {
                Debug.LogError("Lucky number range TMP references are not initialized.", this);
                return;
            }

            for (int i = 0; i < _luckyNumberRangeTexts.Length; i++)
            {
                if (_luckyNumberRangeTexts[i] == null)
                {
                    Debug.LogError($"Lucky number range TMP reference {i + 1} is not assigned.", this);
                    continue;
                }

                _luckyNumberRangeTexts[i].color = highlighted
                    ? _winningRangeColor
                    : _luckyNumberRangeDefaultColors[i];
            }
        }

        private void Update()
        {
            if (SpinRequested == null || _view == null || IsSpinning) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            var events = EventSystem.current;
            if (events != null)
            {
                if (_pointerEvent == null || _pointerEventSystem != events)
                {
                    _pointerEventSystem = events;
                    _pointerEvent = new PointerEventData(events);
                }
                _pointerEvent.Reset();
                _pointerEvent.position = mouse.position.ReadValue();
                _pointerHits.Clear();
                events.RaycastAll(_pointerEvent, _pointerHits);
                foreach (var hit in _pointerHits)
                    if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return;
            }
            var camera = UnityEngine.Camera.main;
            if (camera != null && _view.ContainsScreenPoint(camera, mouse.position.ReadValue()))
                SpinRequested.Invoke();
        }

        private void OnDestroy()
        {
            if (_view == null) return;
            _view.SpinCompleted -= HandleSpinCompleted;
            _view.SpinCancelled -= HandleSpinCancelled;
        }



        public void PrepareRound(GameProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            if (_settings == null)
                throw new InvalidOperationException("Missing RouletteSO.");

            if (!_settings.TryValidate(out string error))
                throw new InvalidOperationException(error);

            // Seleccionar el rango de la semana y día actuales.
            RouletteSO.NumberRange range = _settings.GetNumberRange(
                progress.Week,
                progress.Day
            );

            progress.PrepareNextRound(
                range.minimum,
                range.maximum,
                _settings.maximumSlices,
                _random
            );

            Debug.Log(
                $"[Roulette] Week {progress.Week} | " +
                $"Day {progress.Day} | " +
                $"Range: {range.minimum}-{range.maximum}"
            );
        }



        public int GetBossProbability(int day, IReadOnlyList<Code.Scripts.CursedObject.CursedObject> passives = null, bool logEffects = false)
        {
            int probability = _settings.GetBossProbability(day);
            if (passives != null)
                foreach (var passive in passives)
                    {
                    if (passive == null) continue;
                    int before = probability;
                    probability = passive.ModifyBossProbability(day, probability);
                    if (logEffects)
                        Debug.Log($"[Objetos/Jefe] {passive.displayName} | Día {day} | {before}% -> {probability}%", passive);
                }
            return day == 4 ? 100 : Mathf.Clamp(probability, 0, 100);
        }

        public bool TryValidate(GameProgress progress, out string error)
        {
            error = null;
            if (_settings == null) error = "Falta el RouletteSO.";
            else if (_view == null) error = "Falta la RouletteView.";
            else if (progress == null) error = "Falta el GameProgress.";
            else if (_settings.TryValidate(out error)) _view.TryValidate(out error);
            return error == null;
        }

        public void Show(GameProgress progress, IReadOnlyList<Code.Scripts.CursedObject.CursedObject> passives = null, Action onPresented = null)
        {
            if (progress == null || !progress.IsRoundPrepared)
                throw new InvalidOperationException("Prepare the round before showing the roulette.");
            _view.Show(GetBossProbability(progress.Day, passives), progress.SelectedLuckyNumbers, onPresented);
        }

        public void ShowResult(RouletteResult result)
        {
            SetLuckyNumberText(result);
            _view.ShowResult(result);
        }
        public void Hide() => _view.Hide();
        public void Dismiss(Action completed) => _view.Dismiss(completed);

        public RouletteResult Spin(GameProgress progress, IReadOnlyList<Code.Scripts.CursedObject.CursedObject> passives = null)
        {
            if (!TryValidate(progress, out string error)) throw new InvalidOperationException(error);
            if (_view.IsSpinning) throw new InvalidOperationException("The roulette is already spinning.");
            if (progress.HasResult || progress.IsRoundStarted)
                throw new InvalidOperationException("Esta ronda ya tiene un resultado de ruleta.");
            RouletteResult result = RouletteRules.Roll(progress, GetBossProbability(progress.Day, passives), _random);
            _view.Show(result.BossProbability, progress.SelectedLuckyNumbers);
            _view.Spin(result);
            return result;
        }
    }
}
