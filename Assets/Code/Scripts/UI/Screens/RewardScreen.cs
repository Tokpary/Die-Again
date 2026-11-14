using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public abstract class RewardScreen<T> : UIScreen
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

        public void ShowChoices(IReadOnlyList<T> items, Action<int> onPick)
        {
            ClearChoices();
            SetSkipAvailable(true);
            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                Button card = Instantiate(_choiceTemplate, _choicesRoot);
                FillCard(card, items[i]);
                card.onClick.AddListener(() => onPick(index));
                card.gameObject.SetActive(true);
                _choices.Add(card);
            }
        }

        public void ClearChoices()
        {
            foreach (Button card in _choices)
                if (card != null) Destroy(card.gameObject);
            _choices.Clear();
        }

        public void SetSkipAvailable(bool available) => _skipButton.interactable = available;

        protected abstract void FillCard(Button card, T item);
    }
}