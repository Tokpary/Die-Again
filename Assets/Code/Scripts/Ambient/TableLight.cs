using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Scripts.Ambient
{
    public enum TableMood
    {
        Normal,
        Boss,       // día de jefe (desde que sale en la ruleta hasta que acaba el combate)
        CanWin,     // la suma de la mesa está dentro del rango
        Shop,
        GameOver,
        Victory
    }

    // Luz de la mesa (Spot Light + haces del shader LightBeam) que cambia de tono en los momentos clave.
    // Los estados eligen un "mood" con SetMood; los destellos (FlashClaim / FlickerFail) son momentáneos
    // y luego vuelven al mood que hubiera.
    public class TableLight : MonoBehaviour
    {
        [Serializable]
        private class MoodSettings
        {
            public TableMood mood;
            public Color color = Color.white;
            [Tooltip("Multiplica la intensidad original de la luz y del haz.")]
            [Min(0f)] public float intensity = 1f;
        }

        private static readonly int BeamColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Light _light;
        [Tooltip("Renderers con el material del haz (LightBeam). Cogen el color de la luz.")]
        [SerializeField] private Renderer[] _beams;
        [SerializeField, Min(0f)] private float _transitionDuration = 0.6f;
        [Tooltip("Normal = el color y la intensidad con los que empieza la luz en la escena (ignora la entrada Normal de la lista).")]
        [SerializeField] private bool _normalFromScene = true;

        [SerializeField] private MoodSettings[] _moods =
        {
            new MoodSettings { mood = TableMood.Normal, color = new Color(1f, 0.85f, 0.6f), intensity = 1f },
            new MoodSettings { mood = TableMood.Boss, color = new Color(1f, 0.15f, 0.1f), intensity = 1.2f },
            new MoodSettings { mood = TableMood.CanWin, color = new Color(0.3f, 1f, 0.4f), intensity = 1.2f },
            new MoodSettings { mood = TableMood.Shop, color = new Color(0.7f, 0.45f, 1f), intensity = 1f },
            new MoodSettings { mood = TableMood.GameOver, color = new Color(0.5f, 0.05f, 0.05f), intensity = 0.4f },
            new MoodSettings { mood = TableMood.Victory, color = new Color(1f, 0.8f, 0.3f), intensity = 1.6f },
        };

        [Header("Canje en la tienda")]
        [SerializeField] private Color _claimFlashColor = new Color(1f, 0.8f, 0.3f);
        [SerializeField, Min(0f)] private float _claimFlashIntensity = 2.5f;
        [SerializeField, Min(0.05f)] private float _flashDuration = 0.6f;
        [Tooltip("Veces que parpadea al fallar la mezcla.")]
        [SerializeField, Range(1, 10)] private int _flickerCount = 4;

        private MaterialPropertyBlock _block;
        private Color _sceneColor;
        private float _sceneIntensity;
        private float[] _beamStrength;   // brillo (HDR) original de cada haz

        private TableMood _mood = TableMood.Normal;
        private Color _color;
        private float _intensity = 1f;
        private Tween _tween;

        public TableMood Mood => _mood;

        private void Awake()
        {
            if (_light == null) _light = GetComponent<Light>();
            _block = new MaterialPropertyBlock();

            _sceneColor = _light != null ? _light.color : Color.white;
            _sceneIntensity = _light != null ? _light.intensity : 1f;

            _beamStrength = new float[_beams != null ? _beams.Length : 0];
            for (int i = 0; i < _beamStrength.Length; i++)
            {
                Material material = _beams[i] != null ? _beams[i].sharedMaterial : null;
                Color beam = material != null && material.HasProperty(BeamColorId) ? material.GetColor(BeamColorId) : Color.white;
                _beamStrength[i] = Mathf.Max(beam.r, Mathf.Max(beam.g, beam.b));
            }

            Target(TableMood.Normal, out _color, out _intensity);
            Apply(_color, _intensity);
        }

        private void OnDestroy() => _tween?.Kill();

        // ---------------------------------------------------------------- API

        public void SetMood(TableMood mood)
        {
            if (mood == _mood) return;   // si hay un destello en marcha, ya vuelve a este mood
            _mood = mood;
            Target(mood, out Color color, out float intensity);
            TweenTo(color, intensity, _transitionDuration);
        }

        // Canje con premio: destello dorado y vuelta al mood.
        public void FlashClaim() => Flash(_claimFlashColor, _claimFlashIntensity);

        public void Flash(Color color, float intensity)
        {
            Target(_mood, out Color backColor, out float backIntensity);
            Color fromColor = _color;
            float fromIntensity = _intensity;

            _tween?.Kill();
            _tween = DOTween.Sequence()
                .Append(DOVirtual.Float(0f, 1f, 0.08f, t => Apply(Color.Lerp(fromColor, color, t), Mathf.Lerp(fromIntensity, intensity, t))))
                .Append(DOVirtual.Float(0f, 1f, _flashDuration, t => Apply(Color.Lerp(color, backColor, t), Mathf.Lerp(intensity, backIntensity, t)))
                    .SetEase(Ease.OutQuad))
                .SetLink(gameObject);
        }

        // Mezcla fallida: parpadea como una bombilla que falla y vuelve al mood.
        public void FlickerFail()
        {
            Target(_mood, out Color color, out float intensity);

            _tween?.Kill();
            Sequence flicker = DOTween.Sequence();
            for (int i = 0; i < _flickerCount; i++)
            {
                float dim = Random.Range(0.05f, 0.3f);
                flicker.AppendCallback(() => Apply(color, intensity * dim));
                flicker.AppendInterval(Random.Range(0.04f, 0.1f));
                flicker.AppendCallback(() => Apply(color, intensity));
                flicker.AppendInterval(Random.Range(0.05f, 0.15f));
            }
            _tween = flicker.SetLink(gameObject);
        }

        // ---------------------------------------------------------------- Interno

        private void TweenTo(Color color, float intensity, float duration)
        {
            Color fromColor = _color;
            float fromIntensity = _intensity;

            _tween?.Kill();
            _tween = DOVirtual.Float(0f, 1f, duration,
                    t => Apply(Color.Lerp(fromColor, color, t), Mathf.Lerp(fromIntensity, intensity, t)))
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        private void Target(TableMood mood, out Color color, out float intensity)
        {
            if (mood == TableMood.Normal && _normalFromScene)
            {
                color = _sceneColor;
                intensity = 1f;
                return;
            }

            foreach (MoodSettings settings in _moods)
            {
                if (settings.mood != mood) continue;
                color = settings.color;
                intensity = settings.intensity;
                return;
            }

            color = _sceneColor;   // mood sin configurar: como Normal
            intensity = 1f;
        }

        private void Apply(Color color, float intensity)
        {
            _color = color;
            _intensity = intensity;

            if (_light != null)
            {
                _light.color = color;
                _light.intensity = _sceneIntensity * intensity;
            }

            if (_beams == null) return;
            for (int i = 0; i < _beams.Length; i++)
            {
                if (_beams[i] == null) continue;
                _beams[i].GetPropertyBlock(_block);
                _block.SetColor(BeamColorId, color * (_beamStrength[i] * intensity));
                _beams[i].SetPropertyBlock(_block);
            }
        }
    }

    // Atajo para los estados: piden el mood sin preocuparse de si hay luz de mesa en la escena.
    public static class TableLightExtensions
    {
        public static void SetTableMood(this GameContext context, TableMood mood)
        {
            if (context.TableLight != null) context.TableLight.SetMood(mood);
        }
    }
}