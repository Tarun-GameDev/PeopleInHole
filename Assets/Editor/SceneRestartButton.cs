using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public static class SceneRestartButton
{
    static bool shouldRestart = false;
    static string sceneToReload = "";
    static GUIContent restartIcon;
    static float timeScale = 1f;
    static int selectedSceneIndex = 0;
    static List<string> sceneNames = new List<string>();
    static List<string> scenePaths = new List<string>();

    static SceneRestartButton()
    {
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        restartIcon = EditorGUIUtility.IconContent("d_Refresh");
        restartIcon.tooltip = "Restart current scene";

        LoadScenesFromBuildSettings();
    }

    static void LoadScenesFromBuildSettings()
    {
        sceneNames.Clear();
        scenePaths.Clear();
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            sceneNames.Add(name);
            scenePaths.Add(path);
        }
    }

  static void OnToolbarGUI()
{
    GUILayout.BeginHorizontal();
    GUILayout.Space(-6); // Closer to frame skip

    // Restart Button
    if (GUILayout.Button(restartIcon, GUILayout.Width(30), GUILayout.Height(21)))
    {
        SaveAndReloadScene(SceneManager.GetActiveScene().path);
    }

    // Time Scale Slider (only when playing)
    EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
    GUILayout.Label("Speed:", GUILayout.Width(40));
    float newTimeScale = GUILayout.HorizontalSlider(timeScale, 0.1f, 2f, GUILayout.Width(100));
    if (EditorApplication.isPlaying && Mathf.Abs(newTimeScale - timeScale) > 0.01f)
    {
        timeScale = newTimeScale;
        Time.timeScale = timeScale;
    }
    GUILayout.Label(timeScale.ToString("0.0") + "x", GUILayout.Width(30));
    EditorGUI.EndDisabledGroup();

    // Dropdown for Scene Selection with index numbers
    GUILayout.Space(5);
    string[] sceneWithIndexes = new string[sceneNames.Count];
    for (int i = 0; i < sceneNames.Count; i++)
    {
        sceneWithIndexes[i] = $"{i}: {sceneNames[i]}";
    }

    int newSelectedSceneIndex = EditorGUILayout.Popup(selectedSceneIndex, sceneWithIndexes, GUILayout.Width(150));
    if (newSelectedSceneIndex != selectedSceneIndex)
    {
        // Check for unsaved changes before switching scenes
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            selectedSceneIndex = newSelectedSceneIndex;
            string selectedScenePath = scenePaths[selectedSceneIndex];

            if (EditorApplication.isPlaying)
            {
                SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath(selectedScenePath));
            }
            else
            {
                EditorSceneManager.OpenScene(selectedScenePath);
            }
        }
    }

    GUILayout.EndHorizontal();
}

static void SaveAndReloadScene(string scenePath)
{
    if (EditorApplication.isPlaying)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    else
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(scenePath);
    }
}

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && shouldRestart)
        {
            EditorApplication.delayCall += () =>
            {
                if (!string.IsNullOrEmpty(sceneToReload))
                {
                    EditorSceneManager.OpenScene(sceneToReload);
                }
                shouldRestart = false;
            };
        }

        // Reset timescale when play mode starts
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            timeScale = 1f;
            Time.timeScale = 1f;
        }
    }
}
