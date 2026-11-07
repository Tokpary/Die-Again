using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.UI
{
    public class UIManager : MonoBehaviour
    {
        private readonly Dictionary<Type, UIScreen> _screens = new Dictionary<Type, UIScreen>();
        private UIScreen _currentScreen;
        
        private void Awake()
        {
            foreach (var screen in GetComponentsInChildren<UIScreen>(true))
            {
                _screens[screen.GetType()] = screen;
                screen.Hide();
            }
        }
        
        public T ShowScreen<T>() where T : UIScreen
        {
            if(_currentScreen != null)
                _currentScreen.Hide();
            _currentScreen = Get<T>();
            _currentScreen.Show();
            return (T)_currentScreen;
        }
        
        public T Get<T>() where T : UIScreen => (T) _screens[typeof(T)];
    }
}