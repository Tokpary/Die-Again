using Code.Scripts.Dice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    public class DiceInfoView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _previewImage;
        [SerializeField] private TMP_Text _facesText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Material")]
        [SerializeField] private GameObject _materialGroup;
        [SerializeField] private TMP_Text _materialNameText;
        [SerializeField] private TMP_Text _materialDescriptionText;

        public void Show(DiceInstance dice)
        {
            DiceSO data = dice.DiceData;

            if (_nameText != null) _nameText.text = data.DisplayName;
            if (_descriptionText != null) _descriptionText.text = data.description;
            if (_facesText != null) _facesText.text = string.Join("  ", dice.FaceValues);

            if (_previewImage != null)
            {
                _previewImage.sprite = data.preview;
                _previewImage.enabled = data.preview != null;
            }

            bool hasMaterial = dice.DiceMaterial != null;
            if (_materialGroup != null) _materialGroup.SetActive(hasMaterial);
            if (!hasMaterial) return;
            if (_materialNameText != null) _materialNameText.text = dice.DiceMaterial.displayName;
            if (_materialDescriptionText != null) _materialDescriptionText.text = dice.DiceMaterial.description;
        }
    }
}