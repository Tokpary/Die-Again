using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Dice
{
    public class DiceAnimator : MonoBehaviour
    {
        [Header("Arc Animation")]
        [SerializeField] private float _arcDuration = 0.5f;
        [SerializeField] private float _arcHeight = 2f;
        [SerializeField] private AnimationCurve _arcCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Vanish Animation")]
        [SerializeField] private float _stagger = 0.08f;
        [SerializeField] private float _vanishDuration = 0.5f;

        public void ArcTo(IReadOnlyList<Dice> dice, Func<int, Vector3> getTarget, Action<Dice> onArrived,
            Action onComplete)
        {
            RunBatch(dice, (d,i) => Arc(d, getTarget(i), i * _stagger), onArrived, onComplete);
        }
        
        public void Vanish(IReadOnlyList<Dice> dice, Action<Dice> onVanish, Action onComplete)
        {
            RunBatch(dice, (d,i) => Shrink(d, i * _stagger), onVanish, onComplete);
        }
        
        private void RunBatch(IReadOnlyList<Dice> dice, Func<Dice, int, IEnumerator> animation, Action<Dice> onEach, Action onComplete)
        {
            var list = new List<Dice>(dice);
            int pending = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Dice d = list[i];
                if (d == null) continue;
                pending++;
                StartCoroutine(Then(animation(d, i), () =>
                {
                    onEach?.Invoke(d);
                    if (--pending == 0) onComplete?.Invoke();
                }));
            }
            if(pending == 0) onComplete?.Invoke();
        }
        
        private static IEnumerator Then(IEnumerator coroutine, Action done)
        {
            yield return coroutine;
            done();
        }

        private IEnumerator Arc(Dice dice, Vector3 target, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Rigidbody rb = dice.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            Vector3 start = dice.transform.position;
            Quaternion startRot = dice.transform.rotation;
            Quaternion targetRot = UnityEngine.Random.rotation;
            
            for(float t = 0f; t < 1f; t += Time.deltaTime / _arcDuration)
            {
                float e = t*t * (3f - 2f*t); 
                Vector3 position = Vector3.Lerp(start, target, e) + Vector3.up * (_arcHeight * 4f * e * (1f - e));
                dice.transform.SetPositionAndRotation(position, Quaternion.Slerp(startRot, targetRot, e));
                yield return null;
            }

            dice.transform.position = target;
        }

        private IEnumerator Shrink(Dice dice, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            dice.GetComponent<Rigidbody>().isKinematic = true;
            Vector3 startScale = dice.transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / _vanishDuration)
            {
                dice.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }
        }
    }
}