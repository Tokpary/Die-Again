using System;
using Code.Scripts.Camera;
using Code.Scripts.UI;
using UnityEngine;

namespace Code.Scripts.Dice
{
    public interface IDiceHoverRules
    {
        bool CanHover(Dice dice, CameraShotId view);
    }
    public class DiceHover : MonoBehaviour, IHoverable
    {
        public static IDiceHoverRules HoverRules { get; set; }
        [SerializeField] private GameObject _highlight;
        
        private Dice _dice;

        private void Awake()
        {
            _dice = GetComponent<Dice>();
            SetHighlight(false);
        }

        public bool CanHover(CameraShotId view) => HoverRules != null && _dice.Data != null && HoverRules.CanHover(_dice, view);

        private void OnDisable() => SetHighlight(false);
        public object TooltipData => _dice.Data;
        public void OnHoverEnter() => SetHighlight(true);
        public void OnHoverExit() => SetHighlight(false);
        
        private void SetHighlight(bool value)
        {
            if (_highlight != null) _highlight.SetActive(value);
        }
    }
}