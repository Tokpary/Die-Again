using System;
using Code.Scripts.Core;
using Code.Scripts.Roulette;
using Code.Scripts.UI.Screens;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Code.Scripts.Editor
{
    public static class RouletteSceneSetup
    {
        [MenuItem("DiceMaster/Roulette/Inspect geometry %&r")]
        public static void InspectGeometry()
        {
            var view = Object.FindFirstObjectByType<RouletteView>();
            if (view == null) throw new InvalidOperationException("Open Ruleta first.");
            var data = new SerializedObject(view);
            Debug.Log($"ROULETTE_VALID {view.TryValidate(out string error)} {error}");
            var screen = Object.FindFirstObjectByType<RouletteScreen>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font, out string fontGuid, out long fontId);
            Debug.Log($"ROULETTE_FONT {fontGuid} {fontId}");
            var canvas = screen.GetComponentInParent<Canvas>();
            Debug.Log($"ROULETTE_CANVAS position={canvas.transform.position}, rect={((RectTransform)canvas.transform).rect}, scale={canvas.transform.lossyScale}, enabled={canvas.enabled}");
            foreach (var label in screen.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                Debug.Log($"ROULETTE_UI {label.name}: font={label.font}, text={label.text}, scale={label.transform.lossyScale}, rect={label.rectTransform.rect}, active={label.gameObject.activeInHierarchy}, alpha={screen.GetComponent<CanvasGroup>().alpha}");
            var root = (GameObject)data.FindProperty("_visualRoot").objectReferenceValue;
            foreach (Transform child in root.transform.parent.GetComponentsInChildren<Transform>(true))
            {
                Debug.Log($"ROULETTE_GEOMETRY {child.name}: position={child.localPosition:F7}, rotation={child.localEulerAngles:F3}, scale={child.localScale:F7}, world={child.position:F5}");
                var filter = child.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                Debug.Log($"ROULETTE_MESH {child.name}: {mesh.name} bounds={mesh.bounds.ToString("F7")}");
                for (int i = 0; i < mesh.subMeshCount; i++)
                    Debug.Log($"ROULETTE_SUBMESH {child.name} {i}: {mesh.GetSubMesh(i).bounds.ToString("F7")}");
            }
        }

        [MenuItem("DiceMaster/Roulette/Validate connected Ruleta %&v")]
        public static void ValidateScene()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Ruleta.unity")
                throw new InvalidOperationException("Open Ruleta outside Play Mode before validating.");
            var manager = Object.FindFirstObjectByType<GameManager>();
            var screen = Object.FindFirstObjectByType<RouletteScreen>();
            if (manager == null || screen == null) throw new InvalidOperationException("Open Ruleta first.");
            var managerData = new SerializedObject(manager);
            var view = managerData.FindProperty("_rouletteView").objectReferenceValue as RouletteView;
            var settings = managerData.FindProperty("_rouletteSettings").objectReferenceValue as RouletteSO;
            if (view == null || settings == null) throw new InvalidOperationException("Missing roulette references.");
            if (!view.TryValidate(out string error) || !settings.TryValidate(out error)) throw new InvalidOperationException(error);
            var screenData = new SerializedObject(screen);
            foreach (string field in new[] { "_nextButton", "_spinButton", "_previewText", "_resultText" })
                if (screenData.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException("Missing RouletteScreen reference: " + field);
            var canvas = screen.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.transform.lossyScale.sqrMagnitude == 0)
                throw new InvalidOperationException("Roulette UI requires a visible Canvas with nonzero scale.");
            foreach (var label in screen.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (label.font == null) throw new InvalidOperationException("Missing font on " + label.name);
            foreach (GameObject root in manager.gameObject.scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new InvalidOperationException("Missing script on " + child.name);
            Debug.Log("ROULETTE_SCENE_REFERENCES_PASS");
        }

    }
}
