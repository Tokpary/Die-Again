using System;
using System.Collections.Generic;
using Code.Scripts.Dice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class CoinRewardScreen : UIScreen
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Transform _choicesRoot;
        [SerializeField] private Button _choiceTemplate;
        [SerializeField] private Button _skipButton;

        private readonly List<Button> _choices = new List<Button>();

        public event Action SkipClicked;

        protected override void Awake()
        {
            base.Awake();
            _choiceTemplate.gameObject.SetActive(false);
            _skipButton.onClick.AddListener(() => SkipClicked?.Invoke());
        }

        public void ShowTitle(string title) => _titleText.text = title;
        public void ShowChoices(IReadOnlyList<DiceInstance> dice, Action<int> onPick)
        {
            ClearChoices();
            SetSkipAvailable(true);
            for (int i = 0; i < dice.Count; i++)
            {
                int index = i;
                Button card = Instantiate(_choiceTemplate, _choicesRoot);
                card.GetComponentInChildren<DiceInfoView>(true).Show(dice[i]);
                card.onClick.AddListener(() => onPick(index));
                card.gameObject.SetActive(true);
                _choices.Add(card);
            }
        }

        public void ClearChoices()
        {
            foreach (Button button in _choices)
                if (button != null) Destroy(button.gameObject);
            _choices.Clear();
        }

        public void SetSkipAvailable(bool available) => _skipButton.interactable = available;
    }
}