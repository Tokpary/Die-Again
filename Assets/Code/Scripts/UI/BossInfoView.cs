using Code.Scripts.Bosses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    public class BossInfoView : InfoView<BossSO>
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _descriptionText;

        public override void Show(BossSO boss)
        {
            if (_nameText != null) _nameText.text = boss.DisplayName;
            if (_descriptionText != null) _descriptionText.text = boss.description;

            if (_iconImage != null)
            {
                _iconImage.sprite = boss.icon;
                _iconImage.enabled = boss.icon != null;
            }
        }
    }
}