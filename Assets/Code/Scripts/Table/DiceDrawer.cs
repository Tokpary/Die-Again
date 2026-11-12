using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

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
        [SerializeField] private float _selectLift = 0.4f;
        private Dice _selectedDice;
        private Vector3 _closedPosition;
        [SerializeField] private Code.Scripts.Table.DiceDropZone _dropZone;
        [SerializeField] private float _dragHeight = 1.5f;
        public float DragHeight => _dragHeight;
        
        private enum OpenMode {Slide, Hinge}
        
        [Header("Apertura")]
        [Tooltip("Slide: se desliza (cajón). Hinge: _drawerTransform gira como una tapa (cofre).")]
        [SerializeField] private OpenMode _openMode = OpenMode.Slide;
        [SerializeField] private Vector3 _slideOffset = new Vector3(0f, 0f, -2.5f);
        [Tooltip("Rotación local que se suma a la tapa al abrir. El pivote de _drawerTransform debe estar en la bisagra.")]
        [SerializeField] private Vector3 _hingeOpenEuler = new Vector3(-110f, 0f, 0f);
        [SerializeField] private float _openDuration = 1.5f;

        private Quaternion _closedRotation;
        
        public bool IsOpen { get; private set; }
        public Bounds Area => _area.bounds;
        public Vector3 DropPoint => _area.bounds.center + Vector3.up * _returnDropHeight;
        public Code.Scripts.Table.DiceDropZone DropZone => _dropZone;
        private void Awake()
        {
            _closedPosition = _drawerTransform.position;
            _closedRotation = _drawerTransform.localRotation;
        }

        public void Open(DiceCase diceCase = null)
        {
            if (IsOpen) return;
            IsOpen = true;
            if (_openMode == OpenMode.Hinge && AudioManager.Instance != null) AudioManager.Instance.PlayChestSFX();
            if (_openMode == OpenMode.Slide && AudioManager.Instance != null) AudioManager.Instance.PlayDrawerSFX();
            _drawerTransform.DOKill();

            if (_openMode == OpenMode.Hinge)
                _drawerTransform.DOLocalRotateQuaternion(_closedRotation * Quaternion.Euler(_hingeOpenEuler), _openDuration).SetEase(Ease.OutBack);
            else
                _drawerTransform.DOMove(_closedPosition + _slideOffset, _openDuration).SetEase(Ease.OutQuad);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_openMode == OpenMode.Hinge && AudioManager.Instance != null) AudioManager.Instance.PlayChestSFX();
            if (_openMode == OpenMode.Slide && AudioManager.Instance != null) AudioManager.Instance.PlayDrawerSFX();
            _drawerTransform.DOKill();

            if (_openMode == OpenMode.Hinge)
                _drawerTransform.DOLocalRotateQuaternion(_closedRotation, _openDuration).SetEase(Ease.InOutSine);
            else
                _drawerTransform.DOMove(_closedPosition, _openDuration).SetEase(Ease.InOutSine);
        }
        
        public Dice Find(DiceInstance data)
        {
            foreach (Dice dice in _dice)
                if (dice != null && dice.Data == data) return dice;
            return null;
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
            _selectedDice = null;
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
        
        public void Discard(DiceInstance data, DiceCase diceCase)
        {
            Dice dice = Find(data);
            if (dice != null) Discard(dice, diceCase);
            else diceCase.Remove(data);
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
