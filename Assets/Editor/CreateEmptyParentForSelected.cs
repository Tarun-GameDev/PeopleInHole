using UnityEngine;
using UnityEditor;

public static class CreateEmptyParentForSelected
{
    private const string MENU_PARENT_PATH = "Tools/Create Empty Parent for Selected";
    private const string MENU_DELETE_CHILDREN_PATH = "Tools/Delete Children of Selected";

    [MenuItem(MENU_PARENT_PATH, priority = 0)]
    private static void CreateParents()
    {
        var selectedTransforms = Selection.transforms;
        if (selectedTransforms == null || selectedTransforms.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected. Please select one or more GameObjects in the Hierarchy.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        foreach (var child in selectedTransforms)
        {
            // Create the new parent GameObject
            var emptyParent = new GameObject(child.name + "_Parent");
            Undo.RegisterCreatedObjectUndo(emptyParent, "Create Empty Parent");

            // Insert it into the hierarchy
            Undo.SetTransformParent(emptyParent.transform, child.parent, "Set Parent of Empty");
            emptyParent.transform.position = child.position;
            emptyParent.transform.rotation = child.rotation;
            emptyParent.transform.localScale = child.localScale;

            // Reparent the original object under the new parent
            Undo.SetTransformParent(child, emptyParent.transform, "Reparent Object");
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"Created empty parents for {selectedTransforms.Length} GameObject(s).");
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
