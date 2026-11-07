using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button _startButton;
        
        public event Action PlayButtonClicked;
        
        protected override void Awake()
        {
            base.Awake();
            _startButton.onClick.AddListener(() => PlayButtonClicked?.Invoke());
        }
    }
}