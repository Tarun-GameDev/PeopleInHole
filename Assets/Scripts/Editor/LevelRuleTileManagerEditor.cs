#if UNITY_EDITOR
using DT.GridSystem.Ruletile;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelRuleTileManager))]
public class LevelRuleTileManagerEditor : RuleTileMangerEditor
{
    private const string SELECTED_HOLE_COLOR_KEY = "LevelRuleTileManagerEditor_SelectedHoleColor";
    
    private HoleColor selectedHoleColor
    {
        get { return (HoleColor)SessionState.GetInt(SELECTED_HOLE_COLOR_KEY, (int)HoleColor.None); }
        set { SessionState.SetInt(SELECTED_HOLE_COLOR_KEY, (int)value); }
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        base.OnInspectorGUI();
        
        EditorGUILayout.LabelField("Level Design Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Blocks"))
        {
            var generator = (LevelRuleTileManager)target;
            generator.GenerateBlocks();
        }

        if (GUILayout.Button("Remove Blocks"))
        {
            var generator = (LevelRuleTileManager)target;
            generator.RemoveBlocks();
        }

        // Create horizontal layout for the hole placement controls
        EditorGUILayout.BeginHorizontal();
        
        // Dropdown for hole color selection
        selectedHoleColor = (HoleColor)EditorGUILayout.EnumPopup("Hole Color", selectedHoleColor);
        
        // Place Holes button
        if (GUILayout.Button("Place Holes", GUILayout.Width(100)))
        {
            var generator = (LevelRuleTileManager)target;
            generator.PlaceHoles(selectedHoleColor);
        }

        // Remove Holes button
        if (GUILayout.Button("Remove Holes", GUILayout.Width(100)))
        {
            var generator = (LevelRuleTileManager)target;
            generator.RemoveHoles();
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif