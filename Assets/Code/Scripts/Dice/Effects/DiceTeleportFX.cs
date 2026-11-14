using UnityEngine;

namespace Code.Scripts.Dice
{
    // Efectos del "teletransporte" de un dado: cuando desaparece de su sitio (al soltarlo en la zona del
    // cajón o del cofre, o al elegirlo para reroll) y cuando aparece en la mano del lanzador, en la mesa.
    // RollSession llama a PlayVanish y PlayAppear; aquí solo se crean el VFX y se suena el SFX.
    public class DiceTeleportFX : MonoBehaviour
    {
        [Header("Al desaparecer (colocar / elegir para reroll)")]
        [Tooltip("Prefab que se crea donde estaba el dado. Debe destruirse solo.")]
        [SerializeField] private GameObject _vanishVfx;
        [SerializeField] private AudioClip _vanishSfx;

        [Header("Al aparecer en la mesa")]
        [Tooltip("Prefab que se crea en la mano del lanzador. Debe destruirse solo.")]
        [SerializeField] private GameObject _appearVfx;
        [SerializeField] private AudioClip _appearSfx;

        public void PlayVanish(Vector3 position) => Play(_vanishVfx, _vanishSfx, position);
        public void PlayAppear(Vector3 position) => Play(_appearVfx, _appearSfx, position);

        private static void Play(GameObject vfx, AudioClip sfx, Vector3 position)
        {
            if (vfx != null) Instantiate(vfx, position, Quaternion.identity);
            if (sfx != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfx);
        }
    }
}
