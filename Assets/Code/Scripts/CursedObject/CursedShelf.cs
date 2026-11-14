using System;
using System.Collections.Generic;
using Code.Scripts.Camera;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Code.Scripts.CursedObject
{
    public class CursedShelf : MonoBehaviour
    {
        [SerializeField] private Transform[] _slots;

        [Header("Animación")]
        [SerializeField] private float _appearDuration = 0.4f;
        [SerializeField] private float _flyDuration = 1f;
        [SerializeField] private float _flyHeight = 1.5f;
        [SerializeField] private float _vanishDuration = 0.3f;

        [Header("Float Animation")]
        [SerializeField] private float _floatAmplitude = 0.1f;
        [SerializeField] private float _floatPeriod = 2f;
        [SerializeField] private float _spinPeriod = 12f;
        [SerializeField] private float _focusDuration = 0.25f;
        
        [Header("Hover")]
        [SerializeField] private CameraShotId _hoverView = CameraShotId.CursedShelf;
        

        private class Slot
        {
            public Transform Anchor;
            public CursedObject Item;
            public GameObject View;
            public Vector3 RestPosition;     
            public Quaternion RestRotation;
            public bool Idle;               
            public float Motion;             
            public float FloatTime;
            public float SpinAngle;
        }

        private Slot[] _state;
        private int _focused = -1;

        public int Capacity => _slots.Length;
        public bool HasRoom => FreeSlot() >= 0;

        private void Awake()
        {
            _state = new Slot[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _state[i] = new Slot { Anchor = _slots[i] };

                CursedShelfSlot hoverable = _slots[i].GetComponent<CursedShelfSlot>();
                if (hoverable == null) hoverable = _slots[i].gameObject.AddComponent<CursedShelfSlot>();
                hoverable.Init(this, i);
            }
        }

        public void Populate(IReadOnlyList<CursedObject> objects)
        {
            Clear();
            for (int i = 0; i < objects.Count && i < _state.Length; i++)
                if (Fill(_state[i], objects[i]) != null)
                    StartIdle(_state[i], true);
        }

        public void Clear()
        {
            _focused = -1;
            foreach (Slot slot in _state)
            {
                if (slot.View != null) Destroy(slot.View);
                slot.Item = null;
                slot.View = null;
                slot.Idle = false;
            }
        }

        public void Deliver(CursedObject data, Vector3 from, Action onAppeared, Action onLanded)
        {
            int index = FreeSlot();
            if (index < 0)
            {
                Debug.LogWarning("[CursedShelf] No hay hueco libre en la estantería.", this);
                onAppeared?.Invoke();
                onLanded?.Invoke();
                return;
            }

            Slot slot = _state[index];
            GameObject view = Fill(slot, data);
            if (view == null)
            {
                onAppeared?.Invoke();
                onLanded?.Invoke();
                return;
            }

            Transform t = view.transform;
            Vector3 restScale = t.localScale;
            t.position = from;
            t.localScale = Vector3.zero;

            DOTween.Sequence()
                .Append(t.DOScale(restScale, _appearDuration).SetEase(Ease.OutBack))
                .AppendCallback(() => onAppeared?.Invoke())
                .Append(t.DOLocalJump(slot.RestPosition, _flyHeight, 1, _flyDuration))
                .Join(t.DOLocalRotateQuaternion(slot.RestRotation, _flyDuration).SetEase(Ease.InOutSine))
                .SetLink(view)
                .OnComplete(() =>
                {
                    StartIdle(slot, false);
                    onLanded?.Invoke();
                });
        }

        public void Remove(CursedObject data)
        {
            Slot slot = Array.Find(_state, s => s.Item == data);
            if (slot == null) return;

            GameObject view = slot.View;
            slot.Item = null;
            slot.View = null;
            slot.Idle = false;
            if (view == null) return;

            view.transform.DOKill();
            view.transform.DOScale(Vector3.zero, _vanishDuration).SetEase(Ease.InBack)
                .SetLink(view)
                .OnComplete(() => Destroy(view));
        }


        public CursedObject GetItem(int index) => index >= 0 && index < _state.Length ? _state[index].Item : null;

        public bool IsHoverView(CameraShotId view) => view == _hoverView;

        public void SetFocused(int index, bool focused)
        {
            if (focused) _focused = index;
            else if (_focused == index) _focused = -1;
        }

        private void Update()
        {
            float blendStep = Time.deltaTime / Mathf.Max(0.01f, _focusDuration);

            for (int i = 0; i < _state.Length; i++)
            {
                Slot slot = _state[i];
                if (!slot.Idle || slot.View == null) continue;

                slot.Motion = Mathf.MoveTowards(slot.Motion, i == _focused ? 0f : 1f, blendStep);
                if (slot.Motion <= 0f)
                {
                    slot.FloatTime = 0f;
                    slot.SpinAngle = 0f;
                }

                slot.FloatTime += Time.deltaTime * slot.Motion;
                if (_spinPeriod > 0f)
                    slot.SpinAngle = (slot.SpinAngle + 360f / _spinPeriod * Time.deltaTime * slot.Motion) % 360f;

                float weight = Mathf.SmoothStep(0f, 1f, slot.Motion);
                float bob = (1f - Mathf.Cos(slot.FloatTime * 2f * Mathf.PI / _floatPeriod)) * 0.5f;  
                Quaternion spun = Quaternion.AngleAxis(slot.SpinAngle, Vector3.up) * slot.RestRotation; 

                Transform t = slot.View.transform;
                t.localPosition = slot.RestPosition + Vector3.up * (_floatAmplitude * bob * weight);
                t.localRotation = Quaternion.Slerp(slot.RestRotation, spun, weight);
            }
        }

        private void StartIdle(Slot slot, bool randomPhase)
        {
            slot.Idle = true;
            slot.Motion = randomPhase ? 1f : 0f;
            slot.FloatTime = randomPhase ? UnityEngine.Random.Range(0f, _floatPeriod) : 0f;
            slot.SpinAngle = randomPhase ? UnityEngine.Random.Range(0f, 360f) : 0f;
        }

        private int FreeSlot() => Array.FindIndex(_state, s => s.Item == null);

        private GameObject Fill(Slot slot, CursedObject data)
        {
            slot.Item = data;
            slot.Idle = false;
            slot.View = null;
            if (data.prefab == null)
            {
                Debug.LogWarning($"[CursedShelf] {data.DisplayName} no tiene prefab.", data);
                return null;
            }

            slot.View = Instantiate(data.prefab, slot.Anchor);
            slot.RestPosition = slot.View.transform.localPosition;
            slot.RestRotation = slot.View.transform.localRotation;
            return slot.View;
        }
    }
}