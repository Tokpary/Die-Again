using System;
using System.Collections.Generic;
using Code.Scripts.Bosses;
using Code.Scripts.Dice;
using Code.Scripts.Roulette;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class GameOverScreen : UIScreen
    {
        [SerializeField] private Button _returnMainMenuButton;
        [SerializeField] private Button _replayButton;

        [Header("Frase")]
        [SerializeField] private TMP_Text _defeatText;
        [SerializeField] private string _luckyNumberFormat = "Defeated by the Lucky Number {0}, on day {1} of week {2}";
        private string _bossFormat = "Defeated by {0}, on day {1} of week {2}";

        [Header("Victoria")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private string _gameOverTitle = "GAME OVER";
        [SerializeField] private string _victoryTitle = "YOU WIN";
        [SerializeField] private string _victoryFormat = "You defeated {0} and survived all {1} weeks";
        
        [Header("Dados del estuche")]
        [SerializeField] private Transform _diceRoot;
        [SerializeField] private Image _diceIconTemplate;

        private readonly List<Image> _diceIcons = new List<Image>();

        public event Action ReturnToMainMenuClicked;
        public event Action ReplayClicked;

        protected override void Awake()
        {
            base.Awake();

            if (_returnMainMenuButton != null)
                _returnMainMenuButton.onClick.AddListener(() => ReturnToMainMenuClicked?.Invoke());
            if (_replayButton != null)
                _replayButton.onClick.AddListener(() => ReplayClicked?.Invoke());
            if (_diceIconTemplate != null)
                _diceIconTemplate.gameObject.SetActive(false);
        }

        public void ShowDefeat(RouletteResult result, BossSO boss)
        {
            if (_titleText != null) _titleText.text = _gameOverTitle;
            if (_defeatText == null) return;
            _defeatText.text = boss != null
                ? string.Format(_bossFormat, boss.DisplayName, result.Day, result.Week)
                : string.Format(_luckyNumberFormat, result.LuckyNumber, result.Day, result.Week);
        }
        
        public void ShowVictory(RouletteResult result, BossSO finalBoss)
        {
            if (_titleText != null) _titleText.text = _victoryTitle;
            if (_defeatText == null) return;
            string bossName = finalBoss != null ? finalBoss.DisplayName : "the last boss";
            _defeatText.text = string.Format(_victoryFormat, bossName, result.Week);
        }

        public void ShowDice(IReadOnlyList<DiceInstance> dice)
        {
            foreach (Image icon in _diceIcons)
                if (icon != null) Destroy(icon.gameObject);
            _diceIcons.Clear();

            if (_diceRoot == null || _diceIconTemplate == null) return;
            foreach (DiceInstance die in dice)
            {
                Image icon = Instantiate(_diceIconTemplate, _diceRoot);
                icon.sprite = die.DiceData.preview;
                icon.preserveAspect = true;
                icon.gameObject.SetActive(true);
                _diceIcons.Add(icon);
            }
        }
    }
}