using System;
using TMPro;
using UnityEngine;
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
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _winningScoreText;

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
        
        public void ShowScore(int score)
        {
            _scoreText.text = $"Score: {score}";
        }
        
        public void ShowWinningScore(int minimum, int maximum)
        {
            _winningScoreText.text = $"Winning Score: {minimum}-{maximum}";
        }
        
        public void ShowRerollMode(bool active) => _rerollHintText.gameObject.SetActive(active);
        public void SetRerollAvailable(bool available) => _rerollButton.interactable = available;
    }
}