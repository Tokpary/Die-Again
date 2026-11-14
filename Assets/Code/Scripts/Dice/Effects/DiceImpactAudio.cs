using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Code.Scripts.Dice
{
    // Golpes de los dados: contra otro dado o contra cualquier otra cosa (mesa, paredes, cajón...).
    // El volumen va con la fuerza del golpe y el tono varía un poco para que no suene repetido.
    // Para no saturar: un sonido por pareja de dados, una pausa mínima por dado y un tope de sonidos a la vez.
    // Basta con tener uno en la escena: escucha los choques de todos los dados.
    public class DiceImpactAudio : MonoBehaviour
    {
        [Header("Sonidos (se elige uno al azar)")]
        [Tooltip("Dado contra dado.")]
        [SerializeField] private AudioClip[] _diceHits;
        [Tooltip("Dado contra la mesa, una pared, el cajón...")]
        [SerializeField] private AudioClip[] _surfaceHits;
        [Tooltip("Tu grupo de SFX del Audio Mixer, para que respete el volumen de efectos.")]
        [SerializeField] private AudioMixerGroup _output;

        [Header("Fuerza del golpe")]
        [Tooltip("Velocidad del choque por debajo de la cual no suena (roces, dados asentándose).")]
        [SerializeField, Min(0f)] private float _minImpact = 0.5f;
        [Tooltip("Velocidad del choque a partir de la cual suena al volumen máximo.")]
        [SerializeField, Min(0.01f)] private float _fullImpact = 6f;
        [SerializeField, Range(0f, 1f)] private float _maxVolume = 0.8f;
        [SerializeField] private Vector2 _pitchRange = new Vector2(0.9f, 1.15f);

        [Header("Límites")]
        [Tooltip("Segundos mínimos entre dos golpes del mismo dado.")]
        [SerializeField, Min(0f)] private float _cooldownPerDie = 0.06f;
        [Tooltip("Sonidos de golpe a la vez como máximo.")]
        [SerializeField, Range(1, 16)] private int _voices = 6;
        [Tooltip("0 = 2D (igual en toda la pantalla). 1 = 3D (suena desde donde golpea).")]
        [SerializeField, Range(0f, 1f)] private float _spatialBlend;

        private AudioSource[] _sources;
        private int _nextSource;
        private readonly Dictionary<Dice, float> _lastHit = new Dictionary<Dice, float>();

        private void Awake()
        {
            _sources = new AudioSource[_voices];
            for (int i = 0; i < _voices; i++)
            {
                var voice = new GameObject($"DiceImpactVoice {i}");
                voice.transform.SetParent(transform, false);
                AudioSource source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.outputAudioMixerGroup = _output;
                source.spatialBlend = _spatialBlend;
                _sources[i] = source;
            }
        }

        private void OnEnable() => Dice.Impacted += OnImpact;
        private void OnDisable() => Dice.Impacted -= OnImpact;

        private void OnImpact(Dice dice, UnityEngine.Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (speed < _minImpact) return;

            // Dado contra dado: los dos reciben el choque, suena solo uno.
            Dice other = collision.rigidbody != null ? collision.rigidbody.GetComponent<Dice>() : null;
            if (other != null && dice.GetInstanceID() > other.GetInstanceID()) return;

            float now = Time.time;
            if (_lastHit.TryGetValue(dice, out float last) && now - last < _cooldownPerDie) return;
            _lastHit[dice] = now;
            if (_lastHit.Count > 64) ForgetDestroyedDice();

            // Si falta la lista de ese tipo de golpe, usa la otra.
            AudioClip clip = PickClip(other != null ? _diceHits : _surfaceHits);
            if (clip == null) clip = PickClip(other != null ? _surfaceHits : _diceHits);
            if (clip == null) return;

            AudioSource source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Length;

            source.transform.position = collision.contactCount > 0 ? collision.GetContact(0).point : dice.transform.position;
            source.clip = clip;
            source.volume = _maxVolume * Mathf.InverseLerp(_minImpact, _fullImpact, speed);
            source.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
            source.Play();
        }

        private static AudioClip PickClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        private void ForgetDestroyedDice()
        {
            var destroyed = new List<Dice>();
            foreach (Dice dice in _lastHit.Keys)
                if (dice == null) destroyed.Add(dice);
            foreach (Dice dice in destroyed) _lastHit.Remove(dice);
        }
    }
}
