using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class ShopScreen : UIScreen
    {
        [SerializeField] private Button _nextButton;
        
        public event Action NextButtonClicked;
        
        protected override void Awake()
        {
            base.Awake();
            _nextButton.onClick.AddListener(() => NextButtonClicked?.Invoke());
        }
    }
}