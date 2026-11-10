using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Code.Scripts.Dice
{
    public class DiceDrawer : MonoBehaviour
    {
        [SerializeField] private Transform[] _slots;
        private readonly List<Dice> _dice = new List<Dice>();
        [SerializeField] private float _returnDropHeight = 1f;
        [SerializeField] Transform _drawerTransform;
        private Vector3 _closedPosition;
        public void Open(DiceCase diceCase)
        {
            _closedPosition = _drawerTransform.position;
            _drawerTransform.DOMove(_closedPosition + Vector3.forward * -2.5f, 1.5f).SetEase(Ease.OutBack);
        }

        public void Close()
        {
            _drawerTransform.DOMove(_closedPosition, 1.5f).SetEase(Ease.InBack);
        }

        public void Populate(DiceCase diceCase)
        {
            for (int i = 0; i < diceCase.Dice.Count && i < _slots.Length; i++)
            {
                DiceInstance data = diceCase.Dice[i];
                Dice dice = Instantiate(data.DiceData.prefab, _slots[i].position, _slots[i].rotation, _slots[i]);
                dice.Init(data);
                _dice.Add(dice);
            }
        }
        
        public void Release(Dice dice) => _dice.Remove(dice);
        
        public void Clear()
        {
            foreach (Dice dice in _dice)
            {
                if (dice != null)
                {
                    Destroy(dice.gameObject);
                }
            }
            _dice.Clear();
        }

        public bool Contains(Dice dice)
        {
            return _dice.Contains(dice);
        }
        
        public Vector3 GetReturnPoint(int index) => _slots[index % _slots.Length].position + Vector3.up * _returnDropHeight;

        public void Add(Dice dice)
        {
            var rb = dice.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            _dice.Add(dice);
        }

        public void Discard(Dice dice, DiceCase diceCase)
        {
            diceCase.Remove(dice.Data);
            _dice.Remove(dice);
            Destroy(dice.gameObject);
        }
    }
}