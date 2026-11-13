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
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _rerollButton;
        
        [SerializeField] private TMP_Text _rerollHintText;
        [SerializeField] private TMP_Text _shotsText;
        [SerializeField] private TMP_Text _rerollButtonText;
        [HideInInspector, FormerlySerializedAs("_scoreText")]
        [SerializeField] private TMP_Text _previousScoreText;
        [SerializeField] private TMP_Text[] _scoreTexts;
        [SerializeField] private TMP_Text _winningScoreText;
        [SerializeField] private BossIconView _bossIcon;

        [SerializeField] private TableControlsView _tableControls;
        public TableControlsView TableControls => _tableControls;
        
        public event Action NextButtonClicked;
        public event Action RerollButtonClicked;
        
        protected override void Awake()
        {
            base.Awake();
            _nextButton.onClick.AddListener(() => NextButtonClicked?.Invoke());
            _rerollButton.onClick.AddListener(() => RerollButtonClicked?.Invoke());
        }
        
        public void ShowBudget(int shots, int rerolls)
        {
            _shotsText.text = shots.ToString();
            _rerollButtonText.text = $"REROLL ({rerolls})";
        }
        
        public void ShowBoss(BossSO boss)
        {
            if (_bossIcon != null) _bossIcon.Show(boss);
        }

        public void SetCheckAvailable(bool available) => _nextButton.interactable = available;
        
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
        public void SetRerollAvailable(bool available) => _rerollButton.interactable = available;
    }
}