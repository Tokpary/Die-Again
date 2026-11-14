using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    // Ficha de un objeto maldito: nombre, imagen y descripción. Todos los campos son opcionales.
    public class CursedObjectInfoView : InfoView<CursedObject.CursedObject>
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _previewImage;
        [SerializeField] private TMP_Text _descriptionText;

        public override void Show(CursedObject.CursedObject item)
        {
            if (_nameText != null) _nameText.text = item.DisplayName;
            if (_descriptionText != null) _descriptionText.text = item.description;

            if (_previewImage != null)
            {
                _previewImage.sprite = item.preview;
                _previewImage.enabled = item.preview != null;
            }
        }
    }
}