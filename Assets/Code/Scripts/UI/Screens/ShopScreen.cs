using System;
using System.Collections.Generic;
using System.Text;
using Code.Scripts.Dice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class ShopScreen : UIScreen
    {
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _claimButton;
        [SerializeField] private TMP_Text _offerText;
        [SerializeField] private TMP_Text _sumText;

        [SerializeField] private TableControlsView _tableControls;
        public TableControlsView TableControls => _tableControls;
        
        public event Action NextButtonClicked;
        public event Action ClaimButtonClicked;
        
        
        protected override void Awake()
        {
            base.Awake();
            _nextButton.onClick.AddListener(() => NextButtonClicked?.Invoke());
            _claimButton.onClick.AddListener(() => ClaimButtonClicked?.Invoke());
        }
        
        public void ShowOffer(IReadOnlyList<DiceSO> offer, int highlightedNumber)
        {
            var text = new StringBuilder();
            for (int i = 0; i < offer.Count; i++)
            {
                int number = i + 1;
                string line = $"{number}. {DiceLabel(offer[i])}";
                text.AppendLine(number == highlightedNumber ? $"<color=#FFD54A><b>> {line}</b></color>" : line);
            }
            _offerText.text = text.ToString();
        }
        
        public void ShowSum(int sum) => _sumText.text = sum > 0 ? $"Suma: {sum}" : "Lanza dados para elegir";

        public void SetClaimAvailable(bool available) => _claimButton.interactable = available;
        public void SetLeaveAvailable(bool available) => _nextButton.interactable = available;
        
        private static string DiceLabel(DiceSO dice) => dice.diceMaterial != null ? $"{dice.DisplayName} ({dice.diceMaterial.displayName})" : dice.DisplayName;
    }
}