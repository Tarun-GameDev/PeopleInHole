// LevelRuleTileManagerEditor.cs
using DT.GridSystem.Ruletile;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelRuleTileManager))]
public class LevelRuleTileManagerEditor : RuleTileMangerEditor
{
    private const string HOLE_KEY   = "LevelRuleTileManagerEditor_SelectedHoleColor";
    private const string PLAYER_KEY = "LevelRuleTileManagerEditor_SelectedPlayerColor";

    private LevelRuleTileManager Manager => (LevelRuleTileManager)target;

    public ColorEnum selectedHoleColor
    {
        get => (ColorEnum)SessionState.GetInt(HOLE_KEY, (int)ColorEnum.Red);
        set => SessionState.SetInt(HOLE_KEY, (int)value);
    }

    public ColorEnum selectedPlayerColor
    {
        get => (ColorEnum)SessionState.GetInt(PLAYER_KEY, (int)ColorEnum.Blue);
        set => SessionState.SetInt(PLAYER_KEY, (int)value);
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Design Tools", EditorStyles.boldLabel);

        // Holes
        EditorGUILayout.BeginHorizontal();
        selectedHoleColor = (ColorEnum)EditorGUILayout.EnumPopup("Hole Color", selectedHoleColor);
        if (GUILayout.Button("Add Hole", GUILayout.Width(80)))
        {
            Manager.selectedHoleColor = selectedHoleColor;
            Manager.SpawnHole();
        }
        if (GUILayout.Button("Remove Hole", GUILayout.Width(100)))
            Manager.RemoveHoles();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Player Groups
        EditorGUILayout.BeginHorizontal();
        selectedPlayerColor = (ColorEnum)EditorGUILayout.EnumPopup("Player Color", selectedPlayerColor);
        if (GUILayout.Button("Add Player", GUILayout.Width(80)))
        {
            Manager.selectedPlayerColor = selectedPlayerColor;
            Manager.SpawnPlayerGroup();
        }
        if (GUILayout.Button("Remove Player", GUILayout.Width(100)))
            Manager.RemovePlayerGroups();
        EditorGUILayout.EndHorizontal();
    }
}
