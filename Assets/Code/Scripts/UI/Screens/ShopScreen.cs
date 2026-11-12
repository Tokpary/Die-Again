using System;
using System.Collections.Generic;
using System.Text;
using Code.Scripts.Dice;
using Code.Scripts.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class ShopScreen : UIScreen
    {
        private const int MaxOfferSlots = 10;

        [Header("Botón principal")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _nextButtonLabel;
        [SerializeField] private string _nextText = "Next";
        [SerializeField] private string _claimText = "Claim";
        [SerializeField] private string _claimNothingText = "Claim";
        [SerializeField] private TMP_Text _offerText;
        [SerializeField] private TMP_Text _sumText;
        [SerializeField] private Transform _slotsContainer;

        [SerializeField] private TableControlsView _tableControls;
        public TableControlsView TableControls => _tableControls;
        
        public event Action NextButtonClicked;

        private readonly List<ShopOfferSlot> _offerSlots = new List<ShopOfferSlot>();

        protected override void Awake()
        {
            base.Awake();
            _nextButton.onClick.AddListener(() => NextButtonClicked?.Invoke());
            if (_nextButtonLabel == null) _nextButtonLabel = _nextButton.GetComponentInChildren<TMP_Text>(true);
            CacheOfferSlots();
        }
        
        public void ShowOffer(IReadOnlyList<ShopOffer.Entry> offer, int highlightedNumber)
        {
            if (offer == null)
            {
                Debug.LogError("Cannot display a null shop offer.", this);
                return;
            }

            if (_slotsContainer != null)
            {
                ShowOfferInSlots(offer, highlightedNumber);
                return;
            }

            if (_offerText == null)
            {
                Debug.LogError("ShopScreen needs a Slots Container or an Offer Text reference.", this);
                return;
            }

            var text = new StringBuilder();
            for (int i = 0; i < offer.Count; i++)
            {
                int number = offer[i].Number;
                string line = $"{number}. {DiceLabel(offer[i].Dice)}";
                text.AppendLine(number == highlightedNumber ? $"<color=#FFD54A><b>{line}</b></color>" : line);
            }
            _offerText.text = text.ToString();
        }

        private void CacheOfferSlots()
        {
            _offerSlots.Clear();
            if (_slotsContainer == null)
            {
                if (_offerText == null)
                    Debug.LogError("ShopScreen needs a Slots Container or an Offer Text reference.", this);
                return;
            }

            if (_offerText != null)
                _offerText.gameObject.SetActive(false);

            if (_slotsContainer.childCount > MaxOfferSlots)
                Debug.LogWarning($"ShopScreen supports at most {MaxOfferSlots} offer slots.", _slotsContainer);

            int slotCount = Mathf.Min(_slotsContainer.childCount, MaxOfferSlots);
            for (int i = 0; i < slotCount; i++)
            {
                Transform slotRoot = _slotsContainer.GetChild(i);
                Transform numberRoot = FindChild(slotRoot, "Number");
                Transform iconRoot = FindChild(slotRoot, "Icon");
                Transform nameRoot = FindChild(slotRoot, "Name");

                TMP_Text number = numberRoot != null ? numberRoot.GetComponentInChildren<TMP_Text>(true) : null;
                Image icon = iconRoot != null ? iconRoot.GetComponentInChildren<Image>(true) : null;
                TMP_Text name = nameRoot != null ? nameRoot.GetComponentInChildren<TMP_Text>(true) : null;

                if (numberRoot == null || number == null || iconRoot == null || icon == null ||
                    nameRoot == null || name == null)
                {
                    Debug.LogError(
                        $"Offer slot '{slotRoot.name}' needs Number (TMP), Icon (Image), and Name (TMP) children.",
                        slotRoot);
                }

                ShopOfferSlot offerSlot = new ShopOfferSlot(numberRoot, number, iconRoot, icon, nameRoot, name);
                offerSlot.SetContentActive(false);
                _offerSlots.Add(offerSlot);
            }
        }

        private void ShowOfferInSlots(IReadOnlyList<ShopOffer.Entry> offer, int highlightedNumber)
        {
            if (_offerSlots.Count == 0)
            {
                Debug.LogError("Slots Container has no usable offer slots.", _slotsContainer);
                return;
            }

            if (offer.Count > _offerSlots.Count)
                Debug.LogWarning($"Offer has {offer.Count} items but only {_offerSlots.Count} slots are available.", this);

            for (int i = 0; i < _offerSlots.Count; i++)
            {
                ShopOfferSlot slot = _offerSlots[i];
                bool hasOffer = i < offer.Count && offer[i].Dice != null;
                slot.SetContentActive(hasOffer);
                if (!hasOffer)
                {
                    if (i < offer.Count)
                        Debug.LogError($"Shop offer item {i + 1} is null.", this);
                    continue;
                }

                int number = offer[i].Number;
                bool highlighted = number == highlightedNumber;
                string numberLabel = highlighted
                    ? $"<color=#FFD54A><b>{number}.</b></color>"
                    : $"{number}.";
                string nameLabel = DiceLabel(offer[i].Dice);
                slot.SetOffer(numberLabel, offer[i].Dice.preview, highlighted
                    ? $"<color=#FFD54A><b>{nameLabel}</b></color>"
                    : nameLabel);
            }
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private sealed class ShopOfferSlot
        {
            private readonly GameObject _numberRoot;
            private readonly GameObject _iconRoot;
            private readonly GameObject _nameRoot;
            private readonly TMP_Text _number;
            private readonly Image _icon;
            private readonly TMP_Text _name;

            public ShopOfferSlot(Transform numberRoot, TMP_Text number, Transform iconRoot, Image icon,
                Transform nameRoot, TMP_Text name)
            {
                _numberRoot = numberRoot != null ? numberRoot.gameObject : null;
                _number = number;
                _iconRoot = iconRoot != null ? iconRoot.gameObject : null;
                _icon = icon;
                _nameRoot = nameRoot != null ? nameRoot.gameObject : null;
                _name = name;
            }

            public void SetContentActive(bool active)
            {
                if (_numberRoot != null) _numberRoot.SetActive(active);
                if (_iconRoot != null) _iconRoot.SetActive(active);
                if (_nameRoot != null) _nameRoot.SetActive(active);
            }

            public void SetOffer(string number, Sprite icon, string name)
            {
                if (_number != null) _number.text = number;
                if (_icon != null) _icon.sprite = icon;
                if (_name != null) _name.text = name;
            }
        }

        public void SetNextButton(bool claim, bool hasPrize, bool interactable)
        {
            _nextButton.interactable = interactable;
            if (_nextButtonLabel != null)
                _nextButtonLabel.text = !claim ? _nextText : hasPrize ? _claimText : _claimNothingText;
        }
        
        private static string DiceLabel(DiceSO dice) => dice.diceMaterial != null ? $"{dice.DisplayName}" : dice.DisplayName;
    }
}