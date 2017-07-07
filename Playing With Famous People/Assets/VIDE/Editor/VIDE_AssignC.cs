/*
 * Copyright (c) 2017 Christian Henderson
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using MiniJSON_VIDE;

[CanEditMultipleObjects]
[CustomEditor(typeof(VIDE_Assign))]
public class VIDE_AssignC : Editor
{
    /*
     * Custom Inspector for the VIDE_Assign component
     */
    VIDE_Assign d;

    private void openVIDE_Editor(string idx)
    {
        if (d != null)
            loadFiles();

        if (!Directory.Exists(Application.dataPath + "/" + VIDE_Editor.pathToVide + "VIDE"))
        {
            Debug.LogError("Cannot find VIDE folder! If you moved the VIDE folder from the root, make sure you set the 'pathToVide' variable in VIDE_Editor.cs");
            return;
        }

        VIDE_Editor editor = EditorWindow.GetWindow<VIDE_Editor>();
        editor.Init(idx, true);

    }

    void Awake()
    {
        //loadFiles();
    }

    public class SerializeHelper
    {
        static string fileDataPath = Application.dataPath + "/" + VIDE_Editor.pathToVide + "VIDE/Resources/Dialogues/";
        public static void WriteToFile(object data, string filename)
        {
            string outString = DiagJson.Serialize(data);
            File.WriteAllText(fileDataPath + filename, outString);
        }
        public static object ReadFromFile(string filename)
        {
            string jsonString = File.ReadAllText(fileDataPath + filename);
            return DiagJson.Deserialize(jsonString);
        }
    }

    public override void OnInspectorGUI()
    {

        d = (VIDE_Assign)target;
        Color defColor = GUI.color;
        GUI.color = Color.yellow;

        //Create a button to open up the VIDE Editor and load the currently assigned dialogue
        if (GUILayout.Button("Open VIDE Editor"))
        {
            openVIDE_Editor(d.assignedDialogue);
        }

        GUI.color = defColor;

        //Refresh dialogue list
        if (Event.current.type == EventType.MouseDown)
        {
            if (d != null)
                loadFiles();
        }

        GUILayout.BeginHorizontal();

        GUILayout.Label("Assigned dialogue:");
        if (d.diags.Count > 0)
        {
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(d, "Changed dialogue index");
            d.assignedIndex = EditorGUILayout.Popup(d.assignedIndex, d.diags.ToArray());

            if (EditorGUI.EndChangeCheck())
            {
                int theID = 0;
                if (File.Exists(Application.dataPath + "/" + VIDE_Editor.pathToVide + "VIDE/Resources/Dialogues/" + d.diags[d.assignedIndex] + ".json"))
                {
                    Dictionary<string, object> dict = SerializeHelper.ReadFromFile(d.diags[d.assignedIndex] + ".json") as Dictionary<string, object>;
                    if (dict.ContainsKey("dID"))
                        theID = ((int)((long)dict["dID"]));
                    else Debug.LogError("Could not read dialogue ID!");
                }

                d.assignedID = theID;
                d.assignedDialogue = d.diags[d.assignedIndex];


                foreach (var transform in Selection.transforms)
                {
                    VIDE_Assign scr = transform.GetComponent<VIDE_Assign>();
                    scr.assignedIndex = d.assignedIndex;
                    scr.assignedDialogue = d.assignedDialogue;
                    scr.assignedID = d.assignedID;
                }

            }
        }
        else
        {
            GUILayout.Label("No saved Dialogues!");

        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("Alias: ");

        Undo.RecordObject(d, "Changed custom name");
        EditorGUI.BeginChangeCheck();
        d.alias = EditorGUILayout.TextField(d.alias);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var transform in Selection.transforms)
            {
                VIDE_Assign scr = transform.GetComponent<VIDE_Assign>();
                scr.alias = d.alias;
            }
        }

            GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        GUILayout.Label("Override Start Node: ");
        Undo.RecordObject(d, "Changed override start node");
        EditorGUI.BeginChangeCheck();
        d.overrideStartNode = EditorGUILayout.IntField(d.overrideStartNode);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var transform in Selection.transforms)
            {
                VIDE_Assign scr = transform.GetComponent<VIDE_Assign>();
                scr.overrideStartNode = d.overrideStartNode;
            }
        }
        GUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        d.defaultPlayerSprite = (Sprite)EditorGUILayout.ObjectField("Def. Player Sprite: ", d.defaultPlayerSprite, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var transform in Selection.transforms)
            {
                VIDE_Assign scr = transform.GetComponent<VIDE_Assign>();
                scr.defaultPlayerSprite = d.defaultPlayerSprite;
            }
        }

        EditorGUI.BeginChangeCheck();
        d.defaultNPCSprite = (Sprite)EditorGUILayout.ObjectField("Def. NPC Sprite: ", d.defaultNPCSprite, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var transform in Selection.transforms)
            {
                VIDE_Assign scr = transform.GetComponent<VIDE_Assign>();
                scr.defaultNPCSprite = d.defaultNPCSprite;
            }
        }
        GUILayout.Label("Interaction Count: " + d.interactionCount.ToString());
        /*GUILayout.Label("index: " + d.assignedIndex.ToString());
        GUILayout.Label("dialogue: " + d.assignedDialogue.ToString());
        GUILayout.Label("id: " + d.assignedID.ToString());*/

    }

    //Refresh dialogue list
    public void OnFocus()
    {
        if (d != null)
            loadFiles();
    }

        //Refresh dialogue list
    public void loadFiles()
    {
		AssetDatabase.Refresh();
        d = (VIDE_Assign)target;
		
        TextAsset[] files = Resources.LoadAll<TextAsset>("Dialogues");
        d.diags = new List<string>();

        if (files.Length < 1) return;

        foreach (TextAsset f in files)
        {
            d.diags.Add(f.name);
        }

        d.diags.Sort();

        if (d.assignedIndex >= d.diags.Count)
            d.assignedIndex = 0;

        if (d.assignedIndex != -1)
        d.assignedDialogue = d.diags[d.assignedIndex];

        //Lets make sure we still have the right file
        IDCheck();
        Repaint();

    }

    void IDCheck()
    {
        int theID = 0;
        List<int> theIDs = new List<int>();

        if (d.assignedIndex == -1) return;

        if (File.Exists(Application.dataPath + "/" + VIDE_Editor.pathToVide + "/" + d.diags[d.assignedIndex] + ".json"))
        {
            Dictionary<string, object> dict = SerializeHelper.ReadFromFile(d.diags[d.assignedIndex] + ".json") as Dictionary<string, object>;
            if (dict.ContainsKey("dID"))
            {
                theID = ((int)((long)dict["dID"]));
            }
            else { Debug.LogError("Could not read dialogue ID!"); return; }
        }

        if (theID != d.assignedID)
        {
           // Debug.Log("Not same ID!");
            //Retrieve all IDs
            foreach (string s in d.diags)
            {
                if (File.Exists(Application.dataPath + "/" + VIDE_Editor.pathToVide + "VIDE/Resources/Dialogues/" + s + ".json"))
                {
                    Dictionary<string, object> dict = SerializeHelper.ReadFromFile(s + ".json") as Dictionary<string, object>;
                    if (dict.ContainsKey("dID"))
                        theIDs.Add((int)((long)dict["dID"]));
                }
            }

            var theRealID_Index = theIDs.IndexOf(d.assignedID);
            d.assignedIndex = theRealID_Index;
            if (d.assignedIndex != -1)
                d.assignedDialogue = d.diags[d.assignedIndex];
        }
    }

}
