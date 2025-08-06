#if UNITY_EDITOR
using DT.GridSystem.Ruletile;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelRuleTileManager))]
public class LevelRuleTileManagerEditor : RuleTileMangerEditor
{
    private const string KEY = "LevelRuleTileManagerEditor_SelectedHoleColor";

    // Cache the target
    private LevelRuleTileManager manager => (LevelRuleTileManager)target;

    // SessionState to remember the last-picked color
    public ColorEnum selectedHoleColor
    {
        get => (ColorEnum)SessionState.GetInt(KEY, (int)ColorEnum.Red);
        set => SessionState.SetInt(KEY, (int)value);
    }

    public override void OnInspectorGUI()
    {
        // 1) Draw whatever the base class draws
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Design Tools", EditorStyles.boldLabel);

        // 2) Begin your own controls
        EditorGUILayout.BeginHorizontal();

        // color popup
        selectedHoleColor = (ColorEnum)EditorGUILayout.EnumPopup("Hole Color", selectedHoleColor);

        // Add Hole
        if (GUILayout.Button("Add Hole", GUILayout.Width(80)))
        {
            // push the choice into your component, then spawn
            manager.selectedHoleColor = selectedHoleColor;
            manager.SpawnHole();
        }

        // Remove Hole
        if (GUILayout.Button("Remove Hole", GUILayout.Width(80)))
        {
            manager.RemoveHoles();
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif