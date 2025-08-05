using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomKeysEditorScript
{
    // Clear all PlayerPrefs with Ctrl+Shift+Q
    [MenuItem("Edit/Clear All PlayerPrefs using ShortCut %&q")]
    public static void ClearAllPlayerPrefs()
    {
        if (EditorUtility.DisplayDialog("Clear All PlayerPrefs",
                "Are you sure you want to clear all PlayerPrefs?", "Yes", "No"))
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("PlayerPrefs have been cleared.");
        }
    }
    
    // Toggle active state on selected GameObjects with Alt+T
    [MenuItem("GameObject/Toggle Active (Group) &x", false, 0)]
    private static void ToggleActive()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("No GameObject selected to toggle active state.");
            return;
        }

        // Determine if all selected objects are active.
        bool allActive = true;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (!go.activeSelf)
            {
                allActive = false;
                break;
            }
        }

        // Set target state: disable if all are active; otherwise, enable.
        bool targetState = !allActive;

        // Toggle each object's active state.
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go, "Toggle Active State");
            go.SetActive(targetState);
            EditorUtility.SetDirty(go);
        }
    }
    
    // Load the first scene (build index 0) with Alt+Q
    [MenuItem("File/Load First Scene &q", false, 1)]
    public static void LoadFirstScene()
    {
        // Optionally prompt to save any unsaved changes.
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(0);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("Scene at build index 0 not found in Build Settings!");
                return;
            }
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log("Loaded first scene: " + scenePath);
        }
    }
    
    // Load the next scene in the Build Settings with Alt+Right Arrow
    [MenuItem("File/Load Next Scene &2", false, 2)]
    public static void LoadNextScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int totalScenes = SceneManager.sceneCountInBuildSettings;
            int nextIndex = currentIndex + 1;
            if (nextIndex >= totalScenes)
            {
                Debug.LogWarning("Already at the last scene in Build Settings.");
                return;
            }
            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("Scene path not found for build index " + nextIndex);
                return;
            }
            EditorSceneManager.OpenScene(scenePath);
            /*Debug.Log("Loaded next scene: " + scenePath);*/
        }
    }
    
    // Load the previous scene in the Build Settings with Alt+Left Arrow
    [MenuItem("File/Load Previous Scene &1", false, 3)]
    public static void LoadPreviousScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int prevIndex = currentIndex - 1;
            if (prevIndex < 0)
            {
                Debug.LogWarning("Already at the first scene in Build Settings.");
                return;
            }
            string scenePath = SceneUtility.GetScenePathByBuildIndex(prevIndex);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("Scene path not found for build index " + prevIndex);
                return;
            }
            EditorSceneManager.OpenScene(scenePath);
            /*Debug.Log("Loaded previous scene: " + scenePath);*/
        }
    }
    
    
    private const string MENU_PARENT_PATH = "Tools/Create Empty Children for Selected";
    private const string MENU_DELETE_CHILDREN_PATH = "Tools/Delete Children of Selected";

    [MenuItem(MENU_PARENT_PATH, priority = 0)]
    private static void CreateChildrens()
    {
        var selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected. Please select one or more GameObjects in the Hierarchy.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        foreach (var parent in selected)
        {
            // Create the new child GameObject
            var emptyChild = new GameObject(parent.name + "_Child");
            Undo.RegisterCreatedObjectUndo(emptyChild, "Create Empty Child");

            // Parent it under the selected object
            Undo.SetTransformParent(emptyChild.transform, parent, "Set Parent of Empty");

            // Reset local transform so it sits at the parent's origin
            emptyChild.transform.localPosition = Vector3.zero;
            emptyChild.transform.localRotation = Quaternion.identity;
            emptyChild.transform.localScale = Vector3.one;
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"Created empty children for {selected.Length} GameObject(s).");
    }

    [MenuItem(MENU_DELETE_CHILDREN_PATH, priority = 1)]
    private static void DeleteChildren()
    {
        var selectedObjects = Selection.transforms;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected. Please select one or more GameObjects in the Hierarchy.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        foreach (var parent in selectedObjects)
        {
            // Iterate children in reverse to safely remove them
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                Undo.DestroyObjectImmediate(child);
            }
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"Deleted all children for {selectedObjects.Length} GameObject(s).");
    }
}
