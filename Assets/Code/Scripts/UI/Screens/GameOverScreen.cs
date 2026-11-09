using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Scripts.UI.Screens
{
    public class GameOverScreen : UIScreen
    {
        [SerializeField] private Button _returnMainMenuButton;

        public event Action ReturnToMainMenuClicked;


        protected override void Awake()
        {
            base.Awake();

            if (_returnMainMenuButton != null)
                _returnMainMenuButton.onClick.AddListener(() => ReturnToMainMenuClicked?.Invoke());
        }
        
    }
}
