using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class SelectChildrenEditor : Editor
{
    [MenuItem("Edit/Select All Children &c")] // Alt + C
    private static void SelectAllChildren()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        List<UnityEngine.Object> selectedObjects = new List<UnityEngine.Object>();

        foreach (var obj in Selection.transforms)
        {
            int childCount = obj.childCount;

            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    selectedObjects.Add(obj.GetChild(i).gameObject);
                }
            }
            else if (obj.parent != null)
            {
                Transform parent = obj.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    selectedObjects.Add(parent.GetChild(i).gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"{obj.name} has no children and no parent.");
            }
        }

        if (selectedObjects.Count > 0)
        {
            Selection.objects = selectedObjects.Cast<UnityEngine.Object>().ToArray();
        }
        else
        {
            Debug.LogWarning("No valid children or siblings found to select.");
        }
    }

    [MenuItem("Edit/Collapse Parents of Selected &b")] // Alt + B
    private static void CollapseParentsOfSelected()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogWarning("No object selected.");
            return;
        }

        // Get the internal SceneHierarchyWindow type and method
        Assembly unityEditorAssembly = typeof(Editor).Assembly;
        Type hierarchyType = unityEditorAssembly.GetType("UnityEditor.SceneHierarchyWindow");
        EditorWindow window = EditorWindow.GetWindow(hierarchyType);
        if (window == null)
        {
            Debug.LogError("Hierarchy window not found.");
            return;
        }

        MethodInfo setExpanded = hierarchyType.GetMethod(
            "SetExpanded",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (setExpanded == null)
        {
            Debug.LogError("Could not find SetExpanded method.");
            return;
        }

        HashSet<GameObject> collapsedParents = new HashSet<GameObject>();

        foreach (var obj in Selection.transforms)
        {
            Transform parent = obj.parent;
            if (parent != null && collapsedParents.Add(parent.gameObject)) // avoid duplicate collapsing
            {
                setExpanded.Invoke(window, new object[] { parent.gameObject.GetInstanceID(), false });
                Debug.Log($"Collapsed parent: {parent.gameObject.name}");
            }
        }
    }
}
