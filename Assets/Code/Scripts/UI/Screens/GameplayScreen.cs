using System;
using Code.Scripts.Bosses;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class GameplayScreen : UIScreen
    {
        public enum MainButtonMode { Next, Reroll, CancelReroll }

        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _mainButtonLabel;
        [SerializeField] private string _nextText = "NEXT";
        [SerializeField] private string _rerollFormat = "REROLL ({0})";
        [SerializeField] private string _cancelRerollText = "CANCEL";
        [SerializeField] private Button _rerollButton;
        
        [SerializeField] private TMP_Text _rerollHintText;
        [SerializeField] private TMP_Text _shotsText;
        [HideInInspector, FormerlySerializedAs("_scoreText")]
        [SerializeField] private TMP_Text _previousScoreText;
        [SerializeField] private TMP_Text[] _scoreTexts;
        [SerializeField] private TMP_Text _winningScoreText;
        [SerializeField] private BossIconView _bossIcon;

        [SerializeField] private TableControlsView _tableControls;
        public TableControlsView TableControls => _tableControls;
        
        public event Action MainButtonClicked;

        private int _rerolls;
        
        protected override void Awake()
        {
            base.Awake();
            _nextButton.onClick.AddListener(() =>
            {
                if (!InGameSettingsCard.BlocksGameplayInput)
                    MainButtonClicked?.Invoke();
            });
            if (_mainButtonLabel == null) _mainButtonLabel = _nextButton.GetComponentInChildren<TMP_Text>(true);
            if (_rerollButton != null) _rerollButton.gameObject.SetActive(false);
        }
        
        public void ShowBudget(int shots, int rerolls)
        {
            _shotsText.text = shots.ToString();
            _rerolls = rerolls;
        }
        
        public void ShowBoss(BossSO boss)
        {
            if (_bossIcon != null) _bossIcon.Show(boss);
        }

        public void SetMainButton(MainButtonMode mode, bool interactable)
        {
            _nextButton.interactable = interactable;
            if (_mainButtonLabel == null) return;
            _mainButtonLabel.text = mode == MainButtonMode.Next ? _nextText
                : mode == MainButtonMode.CancelReroll ? _cancelRerollText
                : string.Format(_rerollFormat, _rerolls);
        }
        
        public void ShowScore(int score)
        {
            if (_previousScoreText != null)
                _previousScoreText.gameObject.SetActive(false);

            if (_scoreTexts == null || _scoreTexts.Length == 0)
            {
                Debug.LogError("Assign the score TMP texts in the GameplayScreen Inspector.", this);
                return;
            }

            foreach (TMP_Text scoreText in _scoreTexts)
            {
                if (scoreText == null)
                {
                    Debug.LogError("A score TMP text is not assigned in the GameplayScreen Inspector.", this);
                    continue;
                }

                scoreText.text = score.ToString();
            }
        }

        public void ClearScoreDisplay()
        {
            if (_previousScoreText != null)
            {
                _previousScoreText.text = string.Empty;
                _previousScoreText.gameObject.SetActive(false);
            }

            if (_scoreTexts == null || _scoreTexts.Length == 0)
            {
                Debug.LogError("Assign the score TMP texts in the GameplayScreen Inspector.", this);
                return;
            }

            foreach (TMP_Text scoreText in _scoreTexts)
            {
                if (scoreText == null)
                {
                    Debug.LogError("A score TMP text is not assigned in the GameplayScreen Inspector.", this);
                    continue;
                }

                scoreText.text = string.Empty;
            }
        }
        
        public void ShowWinningScore(int minimum, int maximum)
        {
            _winningScoreText.text = $"Winning Score: {minimum}-{maximum}";
        }
        
        public void ShowRerollMode(bool active) => _rerollHintText.gameObject.SetActive(active);
    }
}