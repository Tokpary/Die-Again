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
        [MenuItem("DiceMaster/Roulette/Validate connected Ruleta")]
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
            foreach (GameObject root in manager.gameObject.scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new InvalidOperationException("Missing script on " + child.name);
            Debug.Log("ROULETTE_SCENE_REFERENCES_PASS");
        }

    }
}
