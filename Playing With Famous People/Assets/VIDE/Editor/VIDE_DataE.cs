using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(VIDE_Data))]
public class VIDE_DataE : Editor
{
    Vector2 scrollPos = new Vector2();

    public override void OnInspectorGUI()
    {

        GUIStyle b = new GUIStyle(GUI.skin.GetStyle("Label"));
        b.fontStyle = FontStyle.Bold;

        if (EditorApplication.isPlaying)
        {

            if (VIDE_Data.isLoaded)
            {
                GUILayout.Box("Active: " + VIDE_Data.diags[VIDE_Data.currentDiag].name, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.Box("No dialogue Active", GUILayout.ExpandWidth(true));
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUI.skin.GetStyle("Box"), GUILayout.ExpandWidth(true), GUILayout.Height(400));
            for (int i = 0; i < VIDE_Data.diags.Count; i++)
            {
                if (!VIDE_Data.diags[i].loaded)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(i.ToString() + ". " + VIDE_Data.diags[i].name + ": NOT LOADED");
                    if (VIDE_Data.isLoaded) GUI.enabled = false;
                    if (GUILayout.Button("Load!")) VIDE_Data.LoadDialogues(VIDE_Data.diags[i].name, "");
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();

                }
                else
                {
                    EditorGUILayout.LabelField(i.ToString() + ". " + VIDE_Data.diags[i].name + ": LOADED", b);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();

            if (VIDE_Data.isLoaded) GUI.enabled = false;

            if (GUILayout.Button("Load All"))
            {
                VIDE_Data.LoadDialogues();
            }
            if (GUILayout.Button("Unload All"))
            {
                VIDE_Data.UnloadDialogues();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

        } else
        {
            GUILayout.Box("Enter PlayMode to display loaded/unloaded information", GUILayout.MaxWidth(300));
        }


    }

}
