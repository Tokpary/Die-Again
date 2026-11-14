using Code.Scripts.Bosses;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI
{
    public class BossIconView : UIHoverTarget
    {
        [SerializeField] private Image _icon;

        private BossSO _boss;

        public override object TooltipData => _boss;

        public void Show(BossSO boss)
        {
            _boss = boss;
            gameObject.SetActive(boss != null);
            if (boss == null) return;

            if (_icon == null) _icon = GetComponent<Image>();
            if (_icon != null) _icon.sprite = boss.icon;
        }
    }
}