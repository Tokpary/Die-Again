using System;
using System.Collections.Generic;
using System.Numerics;
using DG.Tweening;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Code.Scripts.Dice
{
    public class DiceDrawer : MonoBehaviour
    {
        [SerializeField] private Transform[] _slots;
        [SerializeField, Tooltip("Optional material for spawned dice without an existing special material.")]
        private DiceMaterials.DiceMaterial _defaultDiceMaterial;
        private readonly List<Dice> _dice = new List<Dice>();
        [SerializeField] private float _returnDropHeight = 1f;
        [SerializeField] Transform _drawerTransform;
        [SerializeField] private Rigidbody _drawerRigidbody;
        [SerializeField] private BoxCollider _area;
        private Vector3 _closedPosition;

        public Bounds Area => _area.bounds;
        public Vector3 DropPoint => _area.bounds.center + Vector3.up * _returnDropHeight;

        private void Awake()
        {
            _closedPosition = _drawerTransform.position;
        }

        public void Open(DiceCase diceCase)
        {
            _drawerTransform.DOMove(_closedPosition + Vector3.forward * -2.5f, 1.5f).SetEase(Ease.OutQuad);
        }

        public void Close()
        {
            _drawerTransform.DOMove(_closedPosition, 1.5f).SetEase(Ease.InOutSine);
        }

        public void Populate(DiceCase diceCase)
        {
            for (int i = 0; i < diceCase.Dice.Count && i < _slots.Length; i++)
            {
                DiceInstance data = diceCase.Dice[i];
                if (data.DiceMaterial == null && _defaultDiceMaterial != null)
                    data.DiceMaterial = _defaultDiceMaterial;
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

        private void FixedUpdate()
        {
            float floorY = _area.bounds.min.y - 0.5f;
            foreach (Dice dice in _dice)
            {
                if (dice == null || dice.transform.position.y >= floorY) continue;
                var rb = dice.GetComponent<Rigidbody>();
                if(rb.isKinematic) continue;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = DropPoint;
            }
        }

        public Dice Spawn(DiceInstance data, Vector3 position)
        {
            Dice dice = Instantiate(data.DiceData.prefab, position, UnityEngine.Random.rotation);
            dice.Init(data);
            dice.GetComponent<Rigidbody>().isKinematic = true;
            dice.transform.localScale = Vector3.zero;
            return dice;
        }
    }
}
