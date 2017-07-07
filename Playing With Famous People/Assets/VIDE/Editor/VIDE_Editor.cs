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
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MiniJSON_VIDE;
using System.Reflection;
using bf = System.Reflection.BindingFlags;
using System.Text.RegularExpressions;
using System.Linq;

public class VIDE_Editor : EditorWindow
{

    //This script will draw the VIDE Editor window and all of its content
    //It comunicates with VIDE_EditorDB to store the data


    //IMPORTANT! If you move the VIDE folder from the root of your project, make sure you set this variable to the updated path
    //For example, if you have a path like this: 'Assets/myFolder/myPlugins/VIDE'
    //Then set pathToVide to 'myFolder/myPlugins/'
	//Default is "" for root
    public const string pathToVide = "";

    //Blacklist for namespaces. 
    //For Action Nodes, add here the namespaces of the scripts you don't wish to see fetched in the list.
	//Any namespace CONTAINING any of the below strings will be discarded in the search.
    public string[] namespaceBlackList = new string[]{
        "UnityEngine",
        //TMP       
    };

    VIDE_EditorDB db; //This is the connection to VIDE_EditorDB, all variables and classes are temporarily stored there
    GameObject dbObj;
    Color defaultColor;
    Color32[] colors;

    VIDE_EditorDB.Comment draggedCom;
    VIDE_EditorDB.Answer draggedAns;
    VIDE_EditorDB.ActionNode draggedAction;

    Vector2 dragStart;
    Rect fWin = new Rect();
    Rect startDiag;
    string loadTag = string.Empty;
    int startID = 0;
    int curFocusID = 0;
    bool showSettings;
    bool previewPanning = true;

    static int currentDiag = 0;
    static int fileIndex = 0;
    int areYouSureIndex = 0;
    int focusedPlayerText = 0;
    Texture2D lineIcon;
    Texture2D newNodeIcon;
    Texture2D newNodeIcon2;
    Texture2D newNodeIcon3;
	int dragNewNode = 0;
	Rect dragNewNodeRect = new Rect(20, 20, 100, 40);

    bool draggingLine = false;
    bool dragnWindows = false;
    bool repaintLines = false;
    //bool autosaveON = true;
    bool editEnabled = true;
    bool newFile = false;
    bool overwritePopup = false;
    bool deletePopup = false;
    bool needSave = false;
    bool npcReady = false;
    bool playerReady = false;
    bool areYouSure = false;
    bool showError = false;
    bool hasID = false;

    string newFileName = "My Dialogue";
    string errorMsg = "";
    string lastTextFocus;

    List<string> saveNames = new List<string>() { };

    //Add VIDE Editor to Window...
    [MenuItem("Window/VIDE Editor")]
    static void ShowEditor()
    {
        if (!Directory.Exists(Application.dataPath + "/" + pathToVide + "VIDE"))
        {
            Debug.LogError("Cannot find VIDE folder at '" + Application.dataPath + "/" + pathToVide + "VIDE" + "'! If you moved the VIDE folder from the root, make sure you set the 'pathToVide' variable in VIDE_Editor.cs");
            return;
        }

        VIDE_Editor editor = EditorWindow.GetWindow<VIDE_Editor>();
        editor.Init("", false);   
    }

    void OnEnable()
    {
        dbObj = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Editor/db.prefab", typeof(GameObject));
        db = dbObj.GetComponent<VIDE_EditorDB>();

        lineIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Data/lineIcon.png", typeof(Texture2D));
        newNodeIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Data/newNode.png", typeof(Texture2D));
        newNodeIcon2 = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Data/newNode1.png", typeof(Texture2D));
        newNodeIcon3 = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Data/newNode2.png", typeof(Texture2D));

        Load(true);
    }

    //Save progress if autosave is on
    void OnLostFocus()
    {
		dragnWindows = false;
        Repaint();
        repaintLines = true;

        if (npcReady && playerReady && needSave)
        {
            Save();
            saveEditorSettings(currentDiag);
        }
    }

    //For safety reasons, let's re-link and repaint
    void OnFocus()
    {
        dbObj = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Editor/db.prefab", typeof(GameObject));
        db = dbObj.GetComponent<VIDE_EditorDB>();
        Repaint();
    }

    //Set all start variables
    public void Init(string dName, bool loadFromIndex)
    {
#if UNITY_5_0 
        EditorWindow.GetWindow<VIDE_Editor>().title = "VIDE Editor";
#else
        Texture2D icon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Data/assignIcon.png", typeof(Texture2D));
        GUIContent titleContent = new GUIContent(" VIDE Editor", icon);
        EditorWindow.GetWindow<VIDE_Editor>().titleContent = titleContent;
#endif

        dbObj = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/" + pathToVide + "VIDE/Editor/db.prefab", typeof(GameObject));
        db = dbObj.GetComponent<VIDE_EditorDB>();
        startDiag = new Rect(20f, 50f, 300f, 50f);

        VIDE_Editor editor = EditorWindow.GetWindow<VIDE_Editor>();
        editor.position = new Rect(50f, 50f, 1027f, 768);


        //Update diag list
        string[] files = Directory.GetFiles(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/", "*json");
        saveNames = new List<string>();
        foreach (string f in files)
        {
            saveNames.Add(Path.GetFileNameWithoutExtension(f));
        }
        saveNames.Sort();

        //Get correct index of sent diag
        int theIndex = 0;
        for (int i = 0; i < saveNames.Count; i++)
        {
            if (saveNames[i] == dName)
                theIndex = i;
        }

        if (loadFromIndex)
        {
            fileIndex = theIndex;
            loadFiles(theIndex);
            saveEditorSettings(currentDiag);
            Load(true);
        } else
        {
            loadEditorSettings();
            loadFiles(currentDiag);
            Load(true);
        }

    }

    public class SerializeHelper
    {
        static string fileDataPath = Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/";
        static string SettingsDataPath = Application.dataPath + "/" + pathToVide + "VIDE/Resources/";

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
        public static void WriteSettings(object data, string filename)
        {
            string outString = DiagJson.Serialize(data);
            File.WriteAllText(SettingsDataPath + filename, outString);
        }
        public static object ReadSettings(string filename)
        {
            string jsonString = File.ReadAllText(SettingsDataPath + filename);
            return DiagJson.Deserialize(jsonString);
        }
    }

    #region Main Methods

    //Methods that manage node creation and deletion 
    public void addComment(VIDE_EditorDB.CommentSet id)
    {
        id.comment.Add(new VIDE_EditorDB.Comment(id));
    }

    public void addAnswer(Vector2 rPos, bool endC, string t, int id, string exD, string tagt)
    {
        db.npcDiags.Add(new VIDE_EditorDB.Answer(rPos, endC, t, id, exD, tagt));
    }

    public void addSet(Vector2 rPos, int cSize, int id, string pTag, bool endC)
    {
        db.playerDiags.Add(new VIDE_EditorDB.CommentSet(rPos, cSize, id, pTag, endC));
    }

    //Remove methods also disconnect nodes correspondingly

    public void removeSet(VIDE_EditorDB.CommentSet id)
    {
        db.playerDiags.Remove(id);
        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].outputSet == id)
            {
                db.npcDiags[i].outputSet = null;
            }
        }

        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].outPlayer == id)
            {
                db.actionNodes[i].outPlayer = null;
            }
        }
    }

    public void removeComment(VIDE_EditorDB.Comment idx)
    {
        Undo.RecordObject(db, "Removed Comment");
        idx.inputSet.comment.Remove(idx);
    }

    public void removeAnswer(VIDE_EditorDB.Answer id)
    {
        db.npcDiags.Remove(id);

        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            for (int ii = 0; ii < db.playerDiags[i].comment.Count; ii++)
            {
                if (db.playerDiags[i].comment[ii].outputAnswer == id)
                {
                    db.playerDiags[i].comment[ii].outputAnswer = null;
                }
            }
        }

        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].outputNPC == id)
            {
                db.npcDiags[i].outputNPC = null;
            }
        }

        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].outNPC == id)
            {
                db.actionNodes[i].outNPC = null;
            }
        }
    }

    public void removeAction(VIDE_EditorDB.ActionNode id)
    {
        db.actionNodes.Remove(id);

        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            for (int ii = 0; ii < db.playerDiags[i].comment.Count; ii++)
            {
                if (db.playerDiags[i].comment[ii].outAction == id)
                {
                    db.playerDiags[i].comment[ii].outAction = null;
                }
            }
        }

        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].outAction == id)
            {
                db.npcDiags[i].outAction = null;
            }
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].outAction == id)
            {
                db.actionNodes[i].outAction = null;
            }
        }
    }

    //This will break the node connections
    public void breakConnection(int type, VIDE_EditorDB.Comment commID, VIDE_EditorDB.Answer ansID, VIDE_EditorDB.ActionNode aID)
    {
        //Type 0 = VIDE_EditorDB.Comment -> VIDE_EditorDB.Answer
        //Type 1 = VIDE_EditorDB.Answer -> Set	
        //Type 2 = VIDE_EditorDB.ActionNode -> All	

        if (type == 0)
        {
            Undo.RecordObject(db, "Broke connection");
            commID.outputAnswer = null;
            commID.outAction = null;
        }
        if (type == 1)
        {

            Undo.RecordObject(db, "Broke connection");
            ansID.outputSet = null;
            Undo.RecordObject(db, "Broke connection");
            ansID.outputNPC = null;
            Undo.RecordObject(db, "Broke connection");
            ansID.outAction = null;
        }
        if (type == 2)
        {
            Undo.RecordObject(db, "Broke connection");
            aID.outPlayer = null;
            Undo.RecordObject(db, "Broke connection");
            aID.outNPC = null;
            Undo.RecordObject(db, "Broke connection");
            aID.outAction = null;
        }

    }

    //Connect player node to NPC node
    //Create node if released on empty space
    public void TryConnectToAnswer(Vector2 mPos, VIDE_EditorDB.Comment commID)
    {
        if (commID == null) return;

        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].rect.Contains(mPos))
            {
                Undo.RecordObject(db, "Connected Node");
                commID.outputAnswer = db.npcDiags[i];
                Repaint();
                return;
            }
        }
        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            if (db.playerDiags[i].rect.Contains(mPos))
            {
                return;
            }
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].rect.Contains(mPos))
            {
                Undo.RecordObject(db, "Connected Node");
                commID.outAction = db.actionNodes[i];
                Repaint();
                return;
            }
        }

        int id = setUniqueID();
        Undo.RecordObject(db, "Added Node");
        db.npcDiags.Add(new VIDE_EditorDB.Answer(new Rect(mPos.x - 150, mPos.y - 200, 0, 0), id));
        commID.outputAnswer = db.npcDiags[db.npcDiags.Count - 1];
        repaintLines = true;
        Repaint();
        GUIUtility.hotControl = 0;
    }

    //Connect NPC node to Player/NPC node
    //Create node if released on empty space
    public void TryConnectToSet(Vector2 mPos, VIDE_EditorDB.Answer ansID)
    {
        if (ansID == null) return;

        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            if (db.playerDiags[i].rect.Contains(mPos))
            {
                Undo.RecordObject(db, "Connected Node");
                ansID.outputSet = db.playerDiags[i];
                Repaint();
                return;
            }
        }
        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].rect.Contains(mPos))
            {
                if (db.npcDiags[i] == ansID) { return; }

                Undo.RecordObject(db, "Connected Node");
                ansID.outputNPC = db.npcDiags[i];
                Repaint();
                return;
            }
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].rect.Contains(mPos))
            {
                Undo.RecordObject(db, "Connected Node");
                ansID.outAction = db.actionNodes[i];
                Repaint();
                return;
            }
        }
        int id = setUniqueID();
        Undo.RecordObject(db, "Added Node");
        db.playerDiags.Add(new VIDE_EditorDB.CommentSet(new Rect(mPos.x - 150, mPos.y - 200, 0, 0), id));
        ansID.outputSet = db.playerDiags[db.playerDiags.Count - 1];
        repaintLines = true;
        Repaint();
        GUIUtility.hotControl = 0;
    }

    //Connect Action node to Player/NPC/Action node
    //Create Action node if released on empty space
    public void TryConnectAction(Vector2 mPos, VIDE_EditorDB.ActionNode aID)
    {
        if (aID == null) return;

        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            if (db.playerDiags[i].rect.Contains(mPos))
            {
                Undo.RecordObject(db, "Connected Node");
                aID.outPlayer = db.playerDiags[i];
                Repaint();
                return;
            }
        }
        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].rect.Contains(mPos))
            {
                Undo.RecordObject(db, "Connected Node");
                aID.outNPC = db.npcDiags[i];
                Repaint();
                return;
            }
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].rect.Contains(mPos))
            {
                if (db.actionNodes[i] == aID) { return; }

                Undo.RecordObject(db, "Connected Node");
                aID.outAction = db.actionNodes[i];
                Repaint();
                return;
            }
        }
        int id = setUniqueID();
        Undo.RecordObject(db, "Added Node");
        db.actionNodes.Add(new VIDE_EditorDB.ActionNode(new Rect(mPos.x - 150, mPos.y - 200, 0, 0), id));
        aID.outAction = db.actionNodes[db.actionNodes.Count - 1];
        repaintLines = true;
        Repaint();
        GUIUtility.hotControl = 0;
    }

    //Sets a unique ID for the node
    public int setUniqueID()
    {
        int tempID = 0;
        while (!searchIDs(tempID))
        {
            tempID++;
        }
        return tempID;
    }

    //Searches for a unique ID
    public bool searchIDs(int id)
    {
        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            if (db.playerDiags[i].ID == id) return false;
        }
        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            if (db.npcDiags[i].ID == id) return false;
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (db.actionNodes[i].ID == id) return false;
        }
        return true;
    }

    int AssignDialogueID()
    {
        List<int> ids = new List<int>();
        int newID = Random.Range(0, 99999);

        //Retrieve all IDs
        foreach (string s in saveNames)
        {
            if (File.Exists(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/" + s + ".json"))
            {
                Dictionary<string, object> dict = SerializeHelper.ReadFromFile(s + ".json") as Dictionary<string, object>;
                if (dict.ContainsKey("dID"))
                    ids.Add((int)((long)dict["dID"]));
            }
        }

        //Make sure ID is unique
        while (ids.Contains(newID))
        {
            newID = Random.Range(0, 99999);
        }

        return newID;
    }

    //Try create a new dialogue file
    public bool tryCreate(string fName)
    {
        if (saveNames.Contains(fName))
        {
            return false;
        }
        else
        {
            saveNames.Add(fName);
            saveNames.Sort();
            currentDiag = saveNames.IndexOf(fName);
            startID = 0;
            return true;
        }
    }

    //Deletes dialogue
    public void DeleteDiag()
    {
        File.Delete(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/" + saveNames[currentDiag] + ".json");
        AssetDatabase.Refresh();
        loadFiles(0);
        Load(true);
    }

    public Rect IDExists()
    {
        int higherID = 0;
        foreach (VIDE_EditorDB.Answer a in db.npcDiags)
        {
            if (a.ID > higherID) { higherID = a.ID; }
        }
        foreach (VIDE_EditorDB.CommentSet c in db.playerDiags)
        {
            if (c.ID > higherID) { higherID = c.ID; }
        }
		foreach (VIDE_EditorDB.ActionNode c in db.actionNodes)
        {
            if (c.ID > higherID) { higherID = c.ID; }
        }
		
        for (int i = 0; i < 99999; i++)
        {
            if (curFocusID > higherID) curFocusID = 0;
            foreach (VIDE_EditorDB.Answer a in db.npcDiags)
            {
                if (a.ID == curFocusID) { return a.rect; }
            }
            foreach (VIDE_EditorDB.CommentSet c in db.playerDiags)
            {
                if (c.ID == curFocusID) { return c.rect; }
            }
			foreach (VIDE_EditorDB.ActionNode c in db.actionNodes)
            {
                if (c.ID == curFocusID) { return c.rect; }
            }
            curFocusID++;
        }
        return new Rect(0, 0, 0, 0);
    }

    //Centers nodes
    public void CenterAll(bool cen)
    {
        Vector2 nodesCenter;
        Rect f = new Rect(0, 0, 0, 0);
        if (!cen)
        {
            curFocusID++;
            f = IDExists();
        }
		
        nodesCenter = new Vector2(f.x + 150, f.y + 50);
        Vector2 center = new Vector2(position.width / 2, position.height / 2);
        Vector2 offset = new Vector2();
        if (cen)
        {
            foreach (VIDE_EditorDB.Answer a in db.npcDiags)
            {
                nodesCenter.x += a.rect.x + 150;
                nodesCenter.y += a.rect.y + 50;
            }
            foreach (VIDE_EditorDB.CommentSet c in db.playerDiags)
            {
                nodesCenter.x += c.rect.x + 150;
                nodesCenter.y += c.rect.y + 50;
            }
			foreach (VIDE_EditorDB.ActionNode a in db.actionNodes)
            {
                nodesCenter.x += a.rect.x + 100;
                nodesCenter.y += a.rect.y + 80;
            }
            nodesCenter.x /= db.npcDiags.Count + db.playerDiags.Count + db.actionNodes.Count;
            nodesCenter.y /= db.npcDiags.Count + db.playerDiags.Count + db.actionNodes.Count;
        }
        offset = nodesCenter - center;
        foreach (VIDE_EditorDB.Answer a in db.npcDiags)
        {
            a.rect = new Rect(a.rect.x - Mathf.Round(offset.x), a.rect.y - Mathf.Round(offset.y), a.rect.width, a.rect.height);
        }
        foreach (VIDE_EditorDB.CommentSet c in db.playerDiags)
        {
            c.rect = new Rect(c.rect.x - Mathf.Round(offset.x), c.rect.y - Mathf.Round(offset.y), c.rect.width, c.rect.height);
        }
		foreach (VIDE_EditorDB.ActionNode a in db.actionNodes)
        {
            a.rect = new Rect(a.rect.x - Mathf.Round(offset.x), a.rect.y - Mathf.Round(offset.y), a.rect.width, a.rect.height);
        }
    }

    #endregion

    #region File Handling

    //This will save the current data base status
    public void Save()
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        int theID = -1;

        if (saveNames.Count < 1)
            return;

        if (currentDiag >= saveNames.Count)
        {
            Debug.LogError("Dialogue file not found! Loading default.");
            currentDiag = 0;
        }

        //AssetDatabase.ImportAsset(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/" + saveNames[currentDiag] + ".json", ImportAssetOptions.Default);

        if (File.Exists(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/" + saveNames[currentDiag] + ".json"))
        {

            Dictionary<string, object> dictl = SerializeHelper.ReadFromFile(saveNames[currentDiag] + ".json") as Dictionary<string, object>;
            if (dictl.ContainsKey("dID"))
                theID = ((int)((long)dictl["dID"]));
        }

        if (theID == -1)
        {
            dict.Add("dID", AssignDialogueID());
        }
        else
        {
            dict.Add("dID", theID);
        }

        dict.Add("npcDiags", db.npcDiags.Count);
        dict.Add("playerDiags", db.playerDiags.Count);
        dict.Add("actionNodes", db.actionNodes.Count);
        dict.Add("startPoint", startID);
        dict.Add("loadTag", loadTag);
        dict.Add("previewPanning", previewPanning);

        /*if (defNPCSprite != null)
            dict.Add("defNPCSpritePath", AssetDatabase.GetAssetPath(defNPCSprite));
        if (defPlayerSprite != null)
            dict.Add("defPlayerSpritePath", AssetDatabase.GetAssetPath(defPlayerSprite));*/

        dict.Add("showSettings", showSettings);

        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            dict.Add("pd_rect_" + i.ToString(), new int[] { (int)db.playerDiags[i].rect.x, (int)db.playerDiags[i].rect.y });
            dict.Add("pd_comSize_" + i.ToString(), db.playerDiags[i].comment.Count);
            dict.Add("pd_ID_" + i.ToString(), db.playerDiags[i].ID);
            dict.Add("pd_pTag_" + i.ToString(), db.playerDiags[i].playerTag);
            //dict.Add("pd_endC_" + i.ToString(), db.playerDiags[i].endConversation);

            if (db.playerDiags[i].sprite != null)
                dict.Add("pd_sprite_" + i.ToString(), AssetDatabase.GetAssetPath(db.playerDiags[i].sprite));

            dict.Add("pd_expand_" + i.ToString(), db.playerDiags[i].expand);
            dict.Add("pd_vars" + i.ToString(), db.playerDiags[i].vars.Count);

            for (int v = 0; v < db.playerDiags[i].vars.Count; v++)
            {
                dict.Add("pd_var_" + i.ToString() + "_" + v.ToString(), db.playerDiags[i].vars[v]);
                dict.Add("pd_varKey_" + i.ToString() + "_" + v.ToString(), db.playerDiags[i].varKeys[v]);
            }

            for (int ii = 0; ii < db.playerDiags[i].comment.Count; ii++)
            {
                dict.Add("pd_" + i.ToString() + "_com_" + ii.ToString() + "iSet", db.playerDiags.FindIndex(idx => idx == db.playerDiags[i].comment[ii].inputSet));
                dict.Add("pd_" + i.ToString() + "_com_" + ii.ToString() + "oAns", db.npcDiags.FindIndex(idx => idx == db.playerDiags[i].comment[ii].outputAnswer));
                dict.Add("pd_" + i.ToString() + "_com_" + ii.ToString() + "oAct", db.actionNodes.FindIndex(idx => idx == db.playerDiags[i].comment[ii].outAction));
                dict.Add("pd_" + i.ToString() + "_com_" + ii.ToString() + "text", db.playerDiags[i].comment[ii].text);
                dict.Add("pd_" + i.ToString() + "_com_" + ii.ToString() + "extraD", db.playerDiags[i].comment[ii].extraData);
            }
        }
        for (int i = 0; i < db.npcDiags.Count; i++)
        {

            if (db.npcDiags[i].sprite != null) 
                dict.Add("nd_sprite_" + i.ToString(), AssetDatabase.GetAssetPath(db.npcDiags[i].sprite));

            dict.Add("nd_expand_" + i.ToString(), db.npcDiags[i].expand);
            dict.Add("nd_vars" + i.ToString(), db.npcDiags[i].vars.Count);

            for (int v = 0; v < db.npcDiags[i].vars.Count; v++)
            {
                dict.Add("nd_var_" + i.ToString() + "_" + v.ToString(), db.npcDiags[i].vars[v]);
                dict.Add("nd_varKey_" + i.ToString() + "_" + v.ToString(), db.npcDiags[i].varKeys[v]);
            }

            dict.Add("nd_rect_" + i.ToString(), new int[] { (int)db.npcDiags[i].rect.x, (int)db.npcDiags[i].rect.y });
            //dict.Add("nd_endc_" + i.ToString(), db.npcDiags[i].endConversation);
            dict.Add("nd_extraData_" + i.ToString(), db.npcDiags[i].extraData);
            dict.Add("nd_tag_" + i.ToString(), db.npcDiags[i].tag);
            dict.Add("nd_text_" + i.ToString(), db.npcDiags[i].text);
            dict.Add("nd_ID_" + i.ToString(), db.npcDiags[i].ID);
            dict.Add("nd_oSet_" + i.ToString(), db.playerDiags.FindIndex(idx => idx == db.npcDiags[i].outputSet));
            dict.Add("nd_oNPC_" + i.ToString(), db.npcDiags.FindIndex(idx => idx == db.npcDiags[i].outputNPC));
            dict.Add("nd_oAct_" + i.ToString(), db.actionNodes.FindIndex(idx => idx == db.npcDiags[i].outAction));
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            dict.Add("ac_rect_" + i.ToString(), new int[] { (int)db.actionNodes[i].rect.x, (int)db.actionNodes[i].rect.y });
            dict.Add("ac_ID_" + i.ToString(), db.actionNodes[i].ID);
            dict.Add("ac_pause_" + i.ToString(), db.actionNodes[i].pauseHere);

            dict.Add("ac_goName_" + i.ToString(), db.actionNodes[i].gameObjectName);
            dict.Add("ac_nIndex_" + i.ToString(), db.actionNodes[i].nameIndex);
			
            dict.Add("ac_optsCount_" + i.ToString(), db.actionNodes[i].opts.Length);
            for (int ii = 0; ii < db.actionNodes[i].opts.Length; ii++)           
                dict.Add("ac_opts_" + ii.ToString()+ "_" + i.ToString(), db.actionNodes[i].opts[ii]);

            dict.Add("ac_namesCount_" + i.ToString(), db.actionNodes[i].nameOpts.Count);
            for (int ii = 0; ii < db.actionNodes[i].nameOpts.Count; ii++)
                dict.Add("ac_names_" + ii.ToString() + "_" + i.ToString(), db.actionNodes[i].nameOpts[ii]);		
            
			List<string> keyList = new List<string>(db.actionNodes[i].methods.Keys);
            dict.Add("ac_methCount_" + i.ToString(), keyList.Count);			
			
			for (int ii = 0; ii < db.actionNodes[i].methods.Count; ii++){
                dict.Add("ac_meth_key_" + i.ToString() + "_" + ii.ToString(), keyList[ii]);			
                dict.Add("ac_meth_val_" + i.ToString() + "_" + ii.ToString(), db.actionNodes[i].methods[keyList[ii]]);			
			}					

            dict.Add("ac_meth_" + i.ToString(), db.actionNodes[i].methodName);
            dict.Add("ac_paramT_" + i.ToString(), db.actionNodes[i].paramType);
            dict.Add("ac_methIndex_" + i.ToString(), db.actionNodes[i].methodIndex);

            dict.Add("ac_pString_" + i.ToString(), db.actionNodes[i].param_string);
            dict.Add("ac_pBool_" + i.ToString(), db.actionNodes[i].param_bool);
            dict.Add("ac_pInt_" + i.ToString(), db.actionNodes[i].param_int);
            dict.Add("ac_pFloat_" + i.ToString(), db.actionNodes[i].param_float);

            dict.Add("ac_oSet_" + i.ToString(), db.playerDiags.FindIndex(idx => idx == db.actionNodes[i].outPlayer));
            dict.Add("ac_oNPC_" + i.ToString(), db.npcDiags.FindIndex(idx => idx == db.actionNodes[i].outNPC));
            dict.Add("ac_oAct_" + i.ToString(), db.actionNodes.FindIndex(idx => idx == db.actionNodes[i].outAction));

            dict.Add("ac_ovrStartNode_" + i.ToString(), db.actionNodes[i].ovrStartNode);
            dict.Add("ac_renameDialogue_" + i.ToString(), db.actionNodes[i].renameDialogue);
            dict.Add("ac_more_" + i.ToString(), db.actionNodes[i].more);

        }

        needSave = false;
        SerializeHelper.WriteToFile(dict as Dictionary<string, object>, saveNames[currentDiag] + ".json");
        //AssetDatabase.Refresh();
    }

    public static void saveEditorSettings(int cd)
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("currentDiagEdited", cd);
        SerializeHelper.WriteSettings(dict as Dictionary<string, object>, "EditorSettings" + ".json");
        AssetDatabase.Refresh();
    }

    //1.1 -- Added key to remember the last dialogue being edited
    public static void loadEditorSettings()
    {
        if (!File.Exists(Application.dataPath + "/" + pathToVide + "VIDE/Resources/" + "EditorSettings" + ".json"))
            return;

        Dictionary<string, object> dict = SerializeHelper.ReadSettings("EditorSettings" + ".json") as Dictionary<string, object>;
        if (dict.ContainsKey("currentDiagEdited"))
        {
            currentDiag = (int)((long)dict["currentDiagEdited"]);
            fileIndex = currentDiag;
        }
        else
        {
            currentDiag = 0;
            fileIndex = 0;
        }

        //Debug.Log("Loaded current diag: " + (int)((long)dict["currentDiagEdited"]));
        
    }

    //Loads from dialogues
    public void Load(bool clear)
    {
        if (clear)
        {
            db.playerDiags = new List<VIDE_EditorDB.CommentSet>();
            db.npcDiags = new List<VIDE_EditorDB.Answer>();
            db.actionNodes = new List<VIDE_EditorDB.ActionNode>();
        }


        if (saveNames.Count < 1)
            return;

        if (currentDiag >= saveNames.Count)
        {
            Debug.LogError("Dialogue file not found! Loading default.");
            currentDiag = 0;
        }

        if (currentDiag < 0) currentDiag = 0;

        if (!File.Exists(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/" + saveNames[currentDiag] + ".json"))
        {
            return;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>("");
        List<string> spriteNames = new List<string>();
        foreach (Sprite t in sprites)
            spriteNames.Add(t.name);

        Dictionary<string, object> dict = SerializeHelper.ReadFromFile(saveNames[currentDiag] + ".json") as Dictionary<string, object>;

        int pDiags = (int)((long)dict["playerDiags"]);
        int nDiags = (int)((long)dict["npcDiags"]);

        int aDiags = 0;
        if (dict.ContainsKey("actionNodes")) aDiags = (int)((long)dict["actionNodes"]);

        startID = (int)((long)dict["startPoint"]);
        if (dict.ContainsKey("loadTag"))
        loadTag = (string)dict["loadTag"];
        if (dict.ContainsKey("previewPanning"))
                previewPanning = (bool)dict["previewPanning"];

        if (dict.ContainsKey("showSettings"))
        {
            showSettings = (bool)dict["showSettings"];
            startDiag.height = 10;
        }

        //Create first...
        for (int i = 0; i < pDiags; i++)
        {
            string tagt = "";
            //bool endC = false;

            if (dict.ContainsKey("pd_pTag_" + i.ToString()))
                tagt = (string)dict["pd_pTag_" + i.ToString()];

            string k = "pd_rect_" + i.ToString();
            List<object> rect = (List<object>)(dict[k]);
            addSet(new Vector2((float)((long)rect[0]), (float)((long)rect[1])),
                (int)((long)dict["pd_comSize_" + i.ToString()]),
                (int)((long)dict["pd_ID_" + i.ToString()]),
                tagt,
                false
                );

            if (dict.ContainsKey("pd_sprite_" + i.ToString()))
            {
                string name = Path.GetFileNameWithoutExtension((string)dict["pd_sprite_" + i.ToString()]);
                if (spriteNames.Contains(name))
                    db.playerDiags[db.playerDiags.Count - 1].sprite = sprites[spriteNames.IndexOf(name)];
                else
                    Debug.LogError("'" + name + "' not found in any Resources folder!");
            }

            if (dict.ContainsKey("pd_expand_" + i.ToString()))
                db.playerDiags[db.playerDiags.Count - 1].expand = (bool)dict["pd_expand_" + i.ToString()];

            if (dict.ContainsKey("pd_vars" + i.ToString()))
            {
                for (int v = 0; v < (int)(long)dict["pd_vars" + i.ToString()]; v++)
                {
                    db.playerDiags[db.playerDiags.Count - 1].vars.Add((string)dict["pd_var_" + i.ToString() + "_" + v.ToString()]);
                    db.playerDiags[db.playerDiags.Count - 1].varKeys.Add((string)dict["pd_varKey_" + i.ToString() + "_" + v.ToString()]);
                }
            }

        }

        for (int i = 0; i < nDiags; i++)
        {
            string k = "nd_rect_" + i.ToString();
            List<object> rect = (List<object>)(dict[k]);

            string tagt = "";

            if (dict.ContainsKey("nd_tag_" + i.ToString()))
                tagt = (string)dict["nd_tag_" + i.ToString()];

            addAnswer(new Vector2((float)((long)rect[0]), (float)((long)rect[1])),
                false,
                (string)dict["nd_text_" + i.ToString()],
                (int)((long)dict["nd_ID_" + i.ToString()]),
                (string)dict["nd_extraData_" + i.ToString()],
                tagt
                );

            if (dict.ContainsKey("nd_sprite_" + i.ToString()))
            {
                string name = Path.GetFileNameWithoutExtension((string)dict["nd_sprite_" + i.ToString()]);

                if (spriteNames.Contains(name))
                    db.npcDiags[db.npcDiags.Count - 1].sprite = sprites[spriteNames.IndexOf(name)];
                else
                    Debug.LogError("'" + name + "' not found in any Resources folder!");
            }

            if (dict.ContainsKey("nd_expand_" + i.ToString()))
                db.npcDiags[db.npcDiags.Count - 1].expand = (bool)dict["nd_expand_" + i.ToString()];

            if (dict.ContainsKey("nd_vars" + i.ToString()))
            {
                for (int v = 0; v < (int)(long)dict["nd_vars" + i.ToString()]; v++)
                {
                    db.npcDiags[db.npcDiags.Count - 1].vars.Add((string)dict["nd_var_" + i.ToString() + "_" + v.ToString()]);
                    db.npcDiags[db.npcDiags.Count - 1].varKeys.Add((string)dict["nd_varKey_" + i.ToString() + "_" + v.ToString()]);
                }
            }
        }

        for (int i = 0; i < aDiags; i++)
        {
            string k = "ac_rect_" + i.ToString();
            List<object> rect = (List<object>)(dict[k]);
            float pFloat;
            var pfl = dict["ac_pFloat_" + i.ToString()];
            if (pfl.GetType() == typeof(System.Double))
                pFloat = System.Convert.ToSingle(pfl);
            else
                pFloat = (float)(long)pfl;


            db.actionNodes.Add(new VIDE_EditorDB.ActionNode(
                new Vector2((float)((long)rect[0]), (float)((long)rect[1])),
                (int)((long)dict["ac_ID_" + i.ToString()]),
                (string)dict["ac_meth_" + i.ToString()],
                (string)dict["ac_goName_" + i.ToString()],
                (bool)dict["ac_pause_" + i.ToString()],
                (bool)dict["ac_pBool_" + i.ToString()],
                (string)dict["ac_pString_" + i.ToString()],
                (int)((long)dict["ac_pInt_" + i.ToString()]),
                pFloat
                ));

            db.actionNodes[db.actionNodes.Count - 1].nameIndex = (int)((long)dict["ac_nIndex_" + i.ToString()]);

            if (dict.ContainsKey("ac_ovrStartNode_" + i.ToString()))
                db.actionNodes[db.actionNodes.Count - 1].ovrStartNode = (int)((long)dict["ac_ovrStartNode_" + i.ToString()]);

            if (dict.ContainsKey("ac_renameDialogue_" + i.ToString()))
                db.actionNodes[db.actionNodes.Count - 1].renameDialogue = (string)dict["ac_renameDialogue_" + i.ToString()];

            if (dict.ContainsKey("ac_more_" + i.ToString()))
                db.actionNodes[db.actionNodes.Count - 1].more = (bool)dict["ac_more_" + i.ToString()];



            List<string> opts = new List<string>();
            List<string> nameOpts = new List<string>();

            for (int ii = 0; ii < (int)((long)dict["ac_optsCount_" + i.ToString()]); ii++)
                opts.Add((string)dict["ac_opts_" + ii.ToString() + "_" + i.ToString()]);

            for (int ii = 0; ii < (int)((long)dict["ac_namesCount_" + i.ToString()]); ii++)
                nameOpts.Add((string)dict["ac_names_" + ii.ToString() + "_" + i.ToString()]);

            db.actionNodes[db.actionNodes.Count - 1].opts = opts.ToArray();
            db.actionNodes[db.actionNodes.Count - 1].nameOpts = nameOpts;

            int dc = (int)((long)dict["ac_methCount_" + i.ToString()]);

            for (int ii = 0; ii < dc; ii++)
            {
                db.actionNodes[db.actionNodes.Count - 1].methods.Add(
                    (string)dict["ac_meth_key_" + i.ToString() + "_" + ii.ToString()],
                    (string)dict["ac_meth_val_" + i.ToString() + "_" + ii.ToString()]
                    );
            }


        }

        //Connect now...
        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            for (int ii = 0; ii < db.playerDiags[i].comment.Count; ii++)
            {
                db.playerDiags[i].comment[ii].text = (string)dict["pd_" + i.ToString() + "_com_" + ii.ToString() + "text"];

                if (dict.ContainsKey("pd_" + i.ToString() + "_com_" + ii.ToString() + "extraD"))
                    db.playerDiags[i].comment[ii].extraData = (string)dict["pd_" + i.ToString() + "_com_" + ii.ToString() + "extraD"];

                int index = (int)((long)dict["pd_" + i.ToString() + "_com_" + ii.ToString() + "iSet"]);

                if (index != -1)
                    db.playerDiags[i].comment[ii].inputSet = db.playerDiags[index];

                index = (int)((long)dict["pd_" + i.ToString() + "_com_" + ii.ToString() + "oAns"]);

                if (index != -1)
                    db.playerDiags[i].comment[ii].outputAnswer = db.npcDiags[index];

                index = -1;
                if (dict.ContainsKey("pd_" + i.ToString() + "_com_" + ii.ToString() + "oAct"))
                    index = (int)((long)dict["pd_" + i.ToString() + "_com_" + ii.ToString() + "oAct"]);

                if (index != -1)
                    db.playerDiags[i].comment[ii].outAction = db.actionNodes[index];
            }
        }
        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            int index = -1;
            index = (int)((long)dict["nd_oSet_" + i.ToString()]);

            if (index != -1)
                db.npcDiags[i].outputSet = db.playerDiags[index];

            if (dict.ContainsKey("nd_oNPC_" + i.ToString()))
            {
                index = -1;
                index = (int)((long)dict["nd_oNPC_" + i.ToString()]);
                if (index != -1)
                    db.npcDiags[i].outputNPC = db.npcDiags[index];
            }

            if (dict.ContainsKey("nd_oAct_" + i.ToString()))
            {
                index = -1;
                index = (int)((long)dict["nd_oAct_" + i.ToString()]);
                if (index != -1)
                    db.npcDiags[i].outAction = db.actionNodes[index];
            }
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            db.actionNodes[i].paramType = (int)((long)dict["ac_paramT_" + i.ToString()]);
            db.actionNodes[i].methodIndex = (int)((long)dict["ac_methIndex_" + i.ToString()]);

            int index = -1;
            index = (int)((long)dict["ac_oSet_" + i.ToString()]);

            if (index != -1)
                db.actionNodes[i].outPlayer = db.playerDiags[index];

            if (dict.ContainsKey("ac_oNPC_" + i.ToString()))
            {
                index = -1;
                index = (int)((long)dict["ac_oNPC_" + i.ToString()]);
                if (index != -1)
                    db.actionNodes[i].outNPC = db.npcDiags[index];
            }

            if (dict.ContainsKey("ac_oAct_" + i.ToString()))
            {
                index = -1;
                index = (int)((long)dict["ac_oAct_" + i.ToString()]);
                if (index != -1)
                    db.actionNodes[i].outAction = db.actionNodes[index];
            }
        }
        repaintLines = true;
        Repaint();
    }

    public static GameObject FindGameObject(string name, int id)
    {
        GameObject g;
        g = EditorUtility.InstanceIDToObject(id) as GameObject;
        return g;
    }

    //Refreshes file list
    public void loadFiles(int focused)
    {
        string[] files = Directory.GetFiles(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/", "*json");
        saveNames = new List<string>();
        currentDiag = focused;
        foreach (string f in files)
        {
            saveNames.Add(Path.GetFileNameWithoutExtension(f));
        }
        saveNames.Sort();
    }

    #endregion
	
	//TOOLBAR
	void DrawToolbar() {
		
		//Current Dialogue
		
        GUI.enabled = editEnabled;
        GUIStyle titleSt = new GUIStyle(GUI.skin.GetStyle("Label"));
        titleSt.fontStyle = FontStyle.Bold;
		GUILayout.BeginHorizontal();
        GUILayout.Label("Editing: ", EditorStyles.label, GUILayout.Width(55));
        int t_file = fileIndex;

        if (saveNames.Count > 0)
        {
            EditorGUI.BeginChangeCheck();
            fileIndex = EditorGUILayout.Popup(fileIndex, saveNames.ToArray(), EditorStyles.toolbarPopup ,GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                if (t_file != fileIndex)
                {
                    if (/*autosaveON &&*/ npcReady && playerReady)
                    {
                        Save();
                    }
                    currentDiag = fileIndex;
                    saveEditorSettings(currentDiag);
                    Load(true);
                }
            }
        }
		
		//Add new
        //GUI.color = Color.green;
        if (GUILayout.Button("Add new dialogue", EditorStyles.toolbarButton))
        {
            editEnabled = false;
            newFile = true;
            GUI.FocusWindow(99998);
        }
        GUI.color = defaultColor;
		
		//Delete
        if (saveNames.Count > 0){
            if (GUILayout.Button("Delete current", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                editEnabled = false;
                deletePopup = true;
            }
            GUIStyle bb = new GUIStyle(GUI.skin.label);
            bb.fontStyle = FontStyle.Bold;
            bb.normal.textColor = Color.red;
            
            if (showError)
			GUI.enabled = false;
				
            
                if (needSave) GUI.color = Color.yellow;
                if (GUILayout.Button("SAVE", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    editEnabled = false;
                    overwritePopup = true;
                }
                GUI.color = defaultColor;
                if (needSave) GUI.color = defaultColor;
                //autosaveON = GUILayout.Toggle(autosaveON, "Autosave");
            
 
			GUI.enabled = true;
			
			    if (!hasID) { GUI.color = Color.red; }
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Start Node ID: ", EditorStyles.miniLabel);
			
                Undo.RecordObject(db, "changed start node");
                startID = EditorGUILayout.IntField(startID, EditorStyles.toolbarTextField, GUILayout.Width(50));
                GUI.color = defaultColor;
                GUILayout.Label("Load Tag: ", EditorStyles.miniLabel);
                Undo.RecordObject(db, "changed load tag");
                loadTag = EditorGUILayout.TextField(loadTag, EditorStyles.toolbarTextField, GUILayout.Width(50));			
                if (EditorGUI.EndChangeCheck())
                    needSave = true;
                GUI.color = defaultColor;
		}
		
		
		
		
        GUILayout.EndHorizontal();
		
		GUILayout.FlexibleSpace();
		
		
     if (GUILayout.Button("Visit blog", EditorStyles.toolbarButton)) {
		 Application.OpenURL("https://videdialogues.wordpress.com/blog/");
         EditorGUIUtility.ExitGUI();
     }		
 	}
	
	void DrawToolbar2() {
		
		//Current Dialogue
		
        GUI.enabled = editEnabled;
        GUIStyle titleSt = new GUIStyle(GUI.skin.GetStyle("Label"));
        titleSt.fontStyle = FontStyle.Bold;
        //int t_file = fileIndex;

		if (saveNames.Count > 0)
        {

            GUI.enabled = true;
            GUILayout.Label("Add nodes: ", EditorStyles.label);
            GUILayout.BeginHorizontal();
            GUI.color = Color.cyan;

            // ADD NEW BUTTONS
            Rect lr;

            if (dragNewNode == 1)
                GUILayout.Box("", EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(40));
            else
                GUILayout.Box(newNodeIcon, EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(40));
            lr = GUILayoutUtility.GetLastRect();
            if (editEnabled && lr.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
            {
                dragNewNode = 1;
            }

            if (dragNewNode == 2)
                GUILayout.Box("", EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(40));
            else
                GUILayout.Box(newNodeIcon2, EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(40));
            lr = GUILayoutUtility.GetLastRect();
            if (editEnabled && lr.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
            {
                dragNewNode = 2;
            }

            if (dragNewNode == 3)
                GUILayout.Box("", EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(40));
            else
                GUILayout.Box(newNodeIcon3, EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(40));
            lr = GUILayoutUtility.GetLastRect();
            if (editEnabled && lr.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
            {
                dragNewNode = 3;
            }

            GUILayout.EndHorizontal();

            GUI.color = defaultColor;
			
                GUILayout.BeginHorizontal();

                GUILayout.Label("Center View: ",  EditorStyles.label, GUILayout.Width(80));
				GUI.color = Color.cyan;
                if (GUILayout.Button("On All", EditorStyles.toolbarButton))
                {
                    CenterAll(true);
                    Repaint();
                }
                if (GUILayout.Button("On Node", EditorStyles.toolbarButton))
                {
                    CenterAll(false);
                    Repaint();
                }
            	GUI.color = defaultColor;
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(db, "Changed preview");
                previewPanning = GUILayout.Toggle(previewPanning, "Perf. panning");
                if (EditorGUI.EndChangeCheck())
                    needSave = true;
	
 	}
			GUILayout.FlexibleSpace();
            GUILayout.Label("VIDE Dialogues 1.2",  EditorStyles.miniLabel);
		
		
	}
	

    //Here's where we actually draw everything
    void OnGUI()
    {
		
        Event e = Event.current;
        //Set colors we'll be using later
        colors = new Color32[]{new Color32(255,255,255,255),
            new Color32(118,180,154, 255),
            new Color32(142,172,180,255),
            new Color32(84,110,137,255),
            new Color32(198,143,137,255)
        };
		
        defaultColor = GUI.color;
		
		GUIStyle sty = EditorStyles.toolbar;
		sty.fixedHeight = 18;
		GUILayout.BeginHorizontal(sty);
     	DrawToolbar();
     	GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(sty);
     	DrawToolbar2();
     	GUILayout.EndHorizontal();
	
		GUILayout.BeginArea(new Rect(0, 36, position.width, position.height));
		
        defaultColor = GUI.color;
		
        //handle input events
        if (editEnabled)
        {
            if (!dragnWindows)
            {
                if (e.type == EventType.MouseUp && GUIUtility.hotControl == 0 && e.button == 1)
                {
                    startDiag.x = e.mousePosition.x - 150;
                    startDiag.y = e.mousePosition.y - 25;
                    GUIUtility.keyboardControl = 0;
                    Repaint();
                }
            }
            if (position.Contains(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)))
            {
                if (e.type == EventType.MouseDrag && e.button == 0 && dragNewNode == 0) //Drag all around
                {
                    if (GUIUtility.hotControl == 0)
                    {
                        dragnWindows = true;
                        //GUI.FocusWindow(99999);
                        if (e.delta.x < 200 && e.delta.y < 200)
                        {
                            for (int offset = 0; offset < db.playerDiags.Count; offset++)
                            {
                                Rect offsetAdded = db.playerDiags[offset].rect;
                                offsetAdded.x += e.delta.x;
                                offsetAdded.y += e.delta.y;
                                db.playerDiags[offset].rect = offsetAdded;
                            }
                            for (int offset = 0; offset < db.npcDiags.Count; offset++)
                            {
                                Rect offsetAdded = db.npcDiags[offset].rect;
                                offsetAdded.x += e.delta.x;
                                offsetAdded.y += e.delta.y;
                                db.npcDiags[offset].rect = offsetAdded;
                            }
                            for (int offset = 0; offset < db.actionNodes.Count; offset++)
                            {
                                Rect offsetAdded = db.actionNodes[offset].rect;
                                offsetAdded.x += e.delta.x;
                                offsetAdded.y += e.delta.y;
                                db.actionNodes[offset].rect = offsetAdded;
                            }
                            Repaint();
                            repaintLines = true;
                        }

                    }
                }
            } else
            {
                if (dragnWindows) // Stop dragging windows
                {
                    dragnWindows = false;
                    Repaint();
                    repaintLines = true;
                }
            }
             
            if (e.type == EventType.MouseUp)
            {
                if (draggingLine) //Connect node detection
                {
                    TryConnectToAnswer(e.mousePosition, draggedCom);
                    TryConnectToSet(e.mousePosition, draggedAns);
                    TryConnectAction(e.mousePosition, draggedAction);
                    needSave = true;
					Repaint();
                    GUIUtility.hotControl = 0;
                    repaintLines = true;
                }
                if (dragnWindows) // Stop dragging windows
                {
                    dragnWindows = false;
                    Repaint();
                    repaintLines = true;
                }
				/*if (dragNewNode > 0) // Stop dragging windows
                {
					addNewNode(e.mousePosition, dragNewNode);						
					dragNewNode = 0;
                    Repaint();
                }*/
                draggingLine = false;
            }
        }
        //Draw connection line
        if (editEnabled)
        {
            if (draggingLine)
            {
                DrawNodeLine3(dragStart, Event.current.mousePosition);
                Repaint();
            }
        }

        //Draw all connected lines
        if (e.type == EventType.Repaint && !dragnWindows)
        {
            DrawLines();
        } else if (!previewPanning)
        {
            DrawLines();
        }

        //Here we'll draw all of the windows
        BeginWindows();

        int setID = 0;
        int ansID = 0;
        int acID = 0;
        GUI.enabled = editEnabled;
        GUIStyle st = new GUIStyle(GUI.skin.window);
	
        st.fontStyle = FontStyle.Bold;
        st.fontSize = 12;
        st.richText = true;
        st.wordWrap = true;
	

        if (db.playerDiags.Count > 0)
        {
            for (; setID < db.playerDiags.Count; setID++)
            {
                GUI.color = colors[1];
                if (!dragnWindows)
                {
                    if (CheckInsideWindow(db.playerDiags[setID].rect))
                    db.playerDiags[setID].rect = GUILayout.Window(setID, db.playerDiags[setID].rect, DrawPlayerWindow, "Player Dialogue - <color=white>ID: " + db.playerDiags[setID].ID.ToString() + "</color>", st, GUILayout.Height(40));
                }
                else
                {
                    if (previewPanning)
                    {
                        if (CheckInsideWindow(db.playerDiags[setID].rect))
                            db.playerDiags[setID].rect = GUILayout.Window(setID, db.playerDiags[setID].rect, DrawEmptyWindow, "Player Dialogue - <color=white>ID: " + db.playerDiags[setID].ID.ToString() + "</color>", st, GUILayout.Height(40));
                    }
                    else
                    {
                        if (CheckInsideWindow(db.playerDiags[setID].rect))
                            db.playerDiags[setID].rect = GUILayout.Window(setID, db.playerDiags[setID].rect, DrawPlayerWindow, "Player Dialogue - <color=white>ID: " + db.playerDiags[setID].ID.ToString() + "</color>", st, GUILayout.Height(40));
                    }

                }

                if (e.keyCode == KeyCode.Tab) focusedPlayerText++;
            }
        }
        if (db.npcDiags.Count > 0)
        {
            for (; ansID < db.npcDiags.Count; ansID++)
            {
                GUI.color = colors[2];
                if (!dragnWindows)
                {
                    if (CheckInsideWindow(db.npcDiags[ansID].rect))
                        db.npcDiags[ansID].rect = GUILayout.Window(ansID + setID, db.npcDiags[ansID].rect, DrawNPCWindow, "NPC Dialogue - <color=white>ID: " + db.npcDiags[ansID].ID.ToString() + "</color>", st, GUILayout.Height(40));
                }
                else
                {
                    if (previewPanning)
                    {
                        if (CheckInsideWindow(db.npcDiags[ansID].rect))
                            db.npcDiags[ansID].rect = GUILayout.Window(ansID + setID, db.npcDiags[ansID].rect, DrawEmptyWindow, "NPC Dialogue - <color=white>ID: " + db.npcDiags[ansID].ID.ToString() + "</color>", st, GUILayout.Height(40));
                    }
                    else
                    {
                        if (CheckInsideWindow(db.npcDiags[ansID].rect))
                            db.npcDiags[ansID].rect = GUILayout.Window(ansID + setID, db.npcDiags[ansID].rect, DrawNPCWindow, "NPC Dialogue - <color=white>ID: " + db.npcDiags[ansID].ID.ToString() + "</color>", st, GUILayout.Height(40));

                    }
                }
            }
        }
        if (db.actionNodes.Count > 0)
        {
            for (; acID < db.actionNodes.Count; acID++)
            {
                GUI.color = new Color32(148, 
					148, 
					221, 
					255);
                if (!dragnWindows)
                {
                    if (CheckInsideWindow(db.actionNodes[acID].rect))
                    db.actionNodes[acID].rect = GUILayout.Window(acID + setID + ansID, db.actionNodes[acID].rect, DrawActionWindow, "Action Node - <color=white>ID: " + db.actionNodes[acID].ID.ToString() + "</color>", st, GUILayout.Height(40), GUILayout.Width(200));
                }
                else
                {
                    if (previewPanning)
                    {
                        if (CheckInsideWindow(db.actionNodes[acID].rect))
                            db.actionNodes[acID].rect = GUILayout.Window(acID + setID + ansID, db.actionNodes[acID].rect, DrawEmptyWindow, "Action Node - <color=white>ID: " + db.actionNodes[acID].ID.ToString() + "</color>", st, GUILayout.Height(40), GUILayout.Width(200));
                    }
                    else
                    {
                        if (CheckInsideWindow(db.actionNodes[acID].rect))
                            db.actionNodes[acID].rect = GUILayout.Window(acID + setID + ansID, db.actionNodes[acID].rect, DrawActionWindow, "Action Node - <color=white>ID: " + db.actionNodes[acID].ID.ToString() + "</color>", st, GUILayout.Height(40), GUILayout.Width(200));
                    }

                }
            }

        }
		


        //Here we check for errors in the node structure

        npcReady = true; playerReady = true;
        hasID = false;
        for (int i = 0; i < db.npcDiags.Count; i++)
        {
            /*if (!db.npcDiags[i].endConversation )
            {
                if (db.npcDiags[i].outputNPC == null && db.npcDiags[i].outputSet == null && db.npcDiags[i].outAction == null)
                { npcReady = false; break; }
            }*/

            if (startID == db.npcDiags[i].ID)
            {
                hasID = true;
            }
        }
        for (int i = 0; i < db.playerDiags.Count; i++)
        {
            /*if (!db.playerDiags[i].endConversation)
            for (int ii = 0; ii < db.playerDiags[i].comment.Count; ii++)
            {
                if (db.playerDiags[i].comment[ii].outputAnswer == null)
                {
                    playerReady = false; break;
                }
            }*/
            if (startID == db.playerDiags[i].ID)
            {
                hasID = true;
            }
        }
        for (int i = 0; i < db.actionNodes.Count; i++)
        {
            if (startID == db.actionNodes[i].ID)
            {
                hasID = true;
            }
        }
        if (!hasID) npcReady = false;

        if (Event.current.type == EventType.Layout)
        {
            showError = false;
            if (!npcReady || !playerReady)
                showError = true;
        }
        GUI.color = colors[0];
        GUI.SetNextControlName("startD");
				
        //startDiag = GUILayout.Window(99999, startDiag, DrawStartWindow, "Editor tools:", st);
		
        GUI.enabled = true;
        if (newFile)
        {
            fWin = new Rect(Screen.width / 4, Screen.height / 4, Screen.width / 2, 0);
            fWin = GUILayout.Window(99998, fWin, DrawNewFileWindow, "New Dialogue:");
            GUI.FocusWindow(99998);
        }
        if (overwritePopup)
        {
            fWin = new Rect(Screen.width / 4, Screen.height / 4, Screen.width / 2, 0);
            fWin = GUILayout.Window(99997, fWin, DrawOverwriteWindow, "File Already Exists!");
            GUI.FocusWindow(99997);
        }
        if (deletePopup)
        {
            fWin = new Rect(Screen.width / 4, Screen.height / 4, Screen.width / 2, 0);
            fWin = GUILayout.Window(99996, fWin, DrawDeleteWindow, "Are you sure?");
            GUI.FocusWindow(99996);
        }
        EndWindows();
		
			
        if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
        {
            areYouSure = false; 
            GUIUtility.keyboardControl = 0;
            Repaint();
        }
		GUILayout.EndArea();

        if (editEnabled)
        if (e.type == EventType.MouseUp)
        {
            if (dragNewNode > 0) // Stop dragging windows
            {
                addNewNode(e.mousePosition, dragNewNode);
                dragNewNode = 0;
                Repaint();
            }
        }

        if (dragNewNode > 0){
			dragNewNodeRect.x = e.mousePosition.x-50;
			dragNewNodeRect.y = e.mousePosition.y-20;
			dragNewNodeRect.width = 100; 
			dragNewNodeRect.height = 40; 
			if (dragNewNode == 1)
			GUI.DrawTexture(dragNewNodeRect, newNodeIcon, ScaleMode.StretchToFill);
			if (dragNewNode == 2)
			GUI.DrawTexture(dragNewNodeRect, newNodeIcon2, ScaleMode.StretchToFill);
			if (dragNewNode == 3)
			GUI.DrawTexture(dragNewNodeRect, newNodeIcon3, ScaleMode.StretchToFill);
			Repaint ();
		}

        //Here's where we'll autosave if everything's in order
		/*
        if (autosaveON && npcReady && playerReady && Event.current.type == EventType.Repaint)
        {
            if (lastTextFocus != "startD" && lastTextFocus != "")
                if (lastTextFocus != GUI.GetNameOfFocusedControl()) { needSave = true; }
            lastTextFocus = GUI.GetNameOfFocusedControl();

            if (saveNames.Count > 0)
            {
                if (needSave)
                {
                    Save();
                    needSave = false;
                }
            }
        }*/
		
    }

    bool CheckInsideWindow(Rect rr)
    {
        Rect r = rr;
        r.x += position.x;
        r.y += position.y;
        if (position.Contains(new Vector2(r.x, r.y))){
            return true;
        }
        if (position.Contains(new Vector2(r.x+r.width, r.y+r.height))){
            return true;
        }
        if (position.Contains(new Vector2(r.x + r.width, r.y)))
        {
            return true;
        }
        if (position.Contains(new Vector2(r.x, r.y + r.height)))
        {
            return true;
        }
        return false;
    }

    //Draw empty window while scrolling everything for better performance
    void DrawEmptyWindow(int id)
    {
        GUI.color = Color.clear;
        GUILayout.Box("", GUILayout.Width(200), GUILayout.Height(50));
        GUI.color = Color.white;

    }

    //The player node
    void DrawPlayerWindow(int id)
    {
        GUI.enabled = editEnabled;
        bool dontDrag = false;
        if (Event.current.type == EventType.MouseUp)
        {
            draggingLine = false;
            dontDrag = true;
        }
        GUILayout.BeginHorizontal();
        GUI.color = colors[1];
        string delText = "Delete Node";
        if (areYouSureIndex == id)
            if (areYouSure) { delText = "Sure?"; GUI.color = new Color32(176, 128, 54, 255); }
        if (GUILayout.Button(delText, GUILayout.Width(80)))
        {
            if (areYouSureIndex != id) areYouSure = false;
            if (!areYouSure)
            {
                areYouSure = true;
                areYouSureIndex = id;
            }
            else
            {
                areYouSure = false;
                areYouSureIndex = 0;
                Undo.RecordObject(db, "Removed Player Node");
                removeSet(db.playerDiags[id]);
                needSave = true;
                return;
            }
        }
        if (Event.current.type == EventType.MouseDown)
        {
            areYouSure = false;
            Repaint();
        }
        GUI.color = defaultColor;
        if (GUILayout.Button("Add comment", GUILayout.Width(162)))
        {
            areYouSure = false;
            addComment(db.playerDiags[id]);
            needSave = true;
        }
        GUILayout.Label("Ex.Data");
		
        GUILayout.EndHorizontal();
        for (int i = 0; i < db.playerDiags[id].comment.Count; i++)
        {
            if (db.playerDiags[id].comment.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label((i + 1).ToString() + ". ", GUILayout.Width(20));
                if (i == 0) GUILayout.Space(24);
                if (i != 0)
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        areYouSure = false;
                        removeComment(db.playerDiags[id].comment[i]);
                        needSave = true;
                        return;
                    }
                GUIStyle stf = new GUIStyle(GUI.skin.textField);
                GUIStyle exD = new GUIStyle(GUI.skin.textField);
                stf.wordWrap = true;
                exD.wordWrap = false;
                GUI.SetNextControlName("pText_" + id.ToString() + i.ToString());
                Undo.RecordObject(db, "Edited Player comment");
				EditorGUI.BeginChangeCheck();
                db.playerDiags[id].comment[i].text = EditorGUILayout.TextArea(db.playerDiags[id].comment[i].text, stf, GUILayout.Width(200));
                if (EditorGUI.EndChangeCheck()){
            	needSave = true;				
				}

				GUI.color = Color.cyan;
                Undo.RecordObject(db, "Edited Player extraData");
				EditorGUI.BeginChangeCheck();
                db.playerDiags[id].comment[i].extraData = EditorGUILayout.TextArea(db.playerDiags[id].comment[i].extraData, exD, GUILayout.Width(70));
                GUI.color = Color.white;
                if (EditorGUI.EndChangeCheck())
                {
                    needSave = true;
                }

                if (db.playerDiags[id].comment[i].outputAnswer == null && db.playerDiags[id].comment[i].outAction == null)
                {
                    Rect lr;
            		GUI.color = Color.green;
                    if (GUILayout.RepeatButton("O", GUILayout.Width(30)))
                    {
                        areYouSure = false;
                        lr = GUILayoutUtility.GetLastRect();
                        lr = new Rect(lr.x + db.playerDiags[id].rect.x + 30, lr.y + db.playerDiags[id].rect.y + 7, 0, 0);
                        if (!draggingLine && !dontDrag)
                        {
                            draggedCom = db.playerDiags[id].comment[i];
                            draggedAns = null;
                            draggedAction = null;
                            dragStart = new Vector2(lr.x, lr.y);
                            draggingLine = true;
                        	needSave = true;							
                        }
                    }
                    GUI.color = defaultColor;
                }
                else
                {
                    GUI.color = defaultColor;
                    if (GUILayout.Button("x", GUILayout.Width(30)))
                    {
                        areYouSure = false;
                        breakConnection(0, db.playerDiags[id].comment[i], null, null);
                        needSave = true;
                    }
                    if (Event.current.type == EventType.Repaint)
                    {
                        db.playerDiags[id].comment[i].outRect = GUILayoutUtility.GetLastRect();
                    }
                }
                //}

                GUILayout.EndHorizontal();
            }
        }

        GUIStyle stf2 = new GUIStyle(GUI.skin.textField);
        stf2.wordWrap = true;
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tag: ", GUILayout.Width(30));
        GUI.color = Color.cyan;
        Undo.RecordObject(db, "changed player Tag");
		
		EditorGUI.BeginChangeCheck();
        db.playerDiags[id].playerTag = EditorGUILayout.TextField(db.playerDiags[id].playerTag, stf2, GUILayout.Width(80));
		if (EditorGUI.EndChangeCheck()){
		needSave = true;	
		}

        GUI.color = new Color(0.7f, 0.8f, 0.4f, 1);
        if (db.playerDiags[id].sprite != null || db.playerDiags[id].vars.Count > 0)
        {
            Color c = new Color(0.1f, 0.4f, 0.8f, 1);
            GUI.color = c;
        }

        GUILayout.FlexibleSpace();
        string exText = (db.playerDiags[id].expand) ? "-" : "+";
        if (GUILayout.Button(exText, GUILayout.Width(60)))
        {
            db.playerDiags[id].expand = !db.playerDiags[id].expand;
            needSave = true;
        }

        GUILayout.EndHorizontal();
        GUI.color = defaultColor;

        /* Expand stuff */

        if (db.playerDiags[id].expand)
        {
            GUIStyle st = new GUIStyle(GUI.skin.label);
            Vector2 coff = st.contentOffset;
            coff.y -= 5;
            st.contentOffset = coff;
            GUILayout.Label("__________________________________________________", st);
            coff.y += 7;
            st.contentOffset = coff;
            st.fontStyle = FontStyle.Bold;

		    EditorGUI.BeginChangeCheck();
            db.playerDiags[id].sprite = (Sprite)EditorGUILayout.ObjectField("Node Sprite: ", db.playerDiags[id].sprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
                needSave = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Extra Variables: ", st);
            if (GUILayout.Button("Add"))
            {
                db.playerDiags[id].vars.Add(string.Empty);
                db.playerDiags[id].varKeys.Add("Key"+ db.playerDiags[id].vars.Count.ToString());
                needSave = true;
            }

            GUILayout.EndHorizontal();

            for (int i = 0; i < db.playerDiags[id].vars.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i.ToString() + ". ", GUILayout.Width(20));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    db.playerDiags[id].vars.RemoveAt(i);
                    db.playerDiags[id].varKeys.RemoveAt(i);
                    needSave = true;
                    break;
                }

                EditorGUI.BeginChangeCheck();
                db.playerDiags[id].varKeys[i] = EditorGUILayout.TextField(db.playerDiags[id].varKeys[i], GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                    needSave = true;

                EditorGUI.BeginChangeCheck();
                db.playerDiags[id].vars[i] = EditorGUILayout.TextField(db.playerDiags[id].vars[i]);
                if (EditorGUI.EndChangeCheck())
                    needSave = true;

                GUILayout.EndHorizontal();

            }

        }



        if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
        {
            areYouSure = false;
            Repaint();
        }
        if (Event.current.commandName == "UndoRedoPerformed")
            Repaint();
        
        GUI.DragWindow();
    }

    //The NPC node
    void DrawNPCWindow(int id)
    {
        GUI.enabled = editEnabled;
        GUIStyle stf = new GUIStyle(GUI.skin.textField);
        stf.wordWrap = true;
        bool dontDrag = false;
        if (Event.current.type == EventType.MouseUp)
        {
            draggingLine = false;
            dontDrag = true;
        }
        int ansID = id - (db.playerDiags.Count);
        if (ansID < 0)
            ansID = 0;
        GUILayout.BeginHorizontal();
        GUI.color = colors[2];
        string delText = "Delete Node";
        if (areYouSureIndex == id)
            if (areYouSure) { delText = "Sure?"; GUI.color = new Color32(176, 128, 54, 255); }
        if (GUILayout.Button(delText, GUILayout.Width(80)))
        {
            if (areYouSureIndex != id) areYouSure = false;
            if (!areYouSure)
            {
                areYouSure = true;
                areYouSureIndex = id;
            }
            else
            {
                areYouSure = false;
                areYouSureIndex = 0;
                Undo.RecordObject(db, "removed node");
                removeAnswer(db.npcDiags[ansID]);
                needSave = true;
                return;
            }
        }
        if (Event.current.type == EventType.MouseDown)
        {
            areYouSure = false;
            Repaint();
        }
        GUI.color = defaultColor;

        if (db.npcDiags[ansID].endConversation)
        {
            GUI.color = Color.green;
        }
        else if (db.npcDiags[ansID].outputSet == null && db.npcDiags[ansID].outputNPC == null)
        {
            GUI.color = Color.red;
        }
        /* Removing as we are gonna do a smart check to know if it is the end
        if (GUILayout.Button("End here: " + db.npcDiags[ansID].endConversation.ToString()))
        {
            areYouSure = false;
            Undo.RecordObject(db, "Changed End Point for id");
            db.npcDiags[ansID].endConversation = !db.npcDiags[ansID].endConversation;
            db.npcDiags[ansID].outputSet = null;
            db.npcDiags[ansID].outputNPC = null;
            needSave = true;
        }
        */
		
        GUI.color = defaultColor;
		
        GUILayout.Label("Ex.Data: ", GUILayout.Width(50));
        GUI.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        Undo.RecordObject(db, "changed extraData");
        db.npcDiags[ansID].extraData = EditorGUILayout.TextField(db.npcDiags[ansID].extraData, stf);
        if (EditorGUI.EndChangeCheck())
        {
            needSave = true;
        }

        GUI.color = defaultColor;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        GUI.SetNextControlName("nText_" + id.ToString());
		EditorGUI.BeginChangeCheck();
        Undo.RecordObject(db, "textCange");
        db.npcDiags[ansID].text = EditorGUILayout.TextArea(db.npcDiags[ansID].text, stf, GUILayout.Width(260));
		if (EditorGUI.EndChangeCheck()){
		needSave = true;	
		}
        //if (!db.npcDiags[ansID].endConversation)
        //{
        if (db.npcDiags[ansID].outputSet == null && db.npcDiags[ansID].outputNPC == null && db.npcDiags[ansID].outAction == null)
        {
            Rect lr;
            GUI.color = Color.green;
			
            if (GUILayout.RepeatButton("O", GUILayout.Width(30)))
            {
                areYouSure = false;
                lr = GUILayoutUtility.GetLastRect();
                lr = new Rect(lr.x + db.npcDiags[ansID].rect.x + 30, lr.y + db.npcDiags[ansID].rect.y + 7, 0, 0);
                if (!draggingLine && !dontDrag)
                {
                    draggedAns = db.npcDiags[ansID];
                    draggedCom = null;
                    draggedAction = null;
                    dragStart = new Vector2(lr.x, lr.y);
                    draggingLine = true;
					needSave = true;
                }
            }
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.white;
            if (GUILayout.Button("x", GUILayout.Width(30)))
            {
                areYouSure = false;
                breakConnection(1, null, db.npcDiags[ansID], null);
                needSave = true;
            }
        }
        //}
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();

        GUILayout.Label("Tag: ", GUILayout.Width(30));
        GUI.color = Color.cyan;
		EditorGUI.BeginChangeCheck();
        Undo.RecordObject(db, "changed Tag");
        db.npcDiags[ansID].tag = EditorGUILayout.TextField(db.npcDiags[ansID].tag, stf, GUILayout.Width(80));
		if (EditorGUI.EndChangeCheck()){
		needSave = true;	
		}

        GUI.color = new Color(0.3f, 0.8f, 0.3f, 1);
        if (db.npcDiags[ansID].sprite != null || db.npcDiags[ansID].vars.Count > 0)
        {
            Color c = new Color(0.1f, 0.4f, 0.8f, 1);
            GUI.color = c;
        }

        GUILayout.FlexibleSpace();
        string exText = (db.npcDiags[ansID].expand) ? "-" : "+";
        if (GUILayout.Button(exText, GUILayout.Width(60)))
        {
            db.npcDiags[ansID].expand = !db.npcDiags[ansID].expand;
            needSave = true;
        }

        GUI.color = defaultColor;

        GUILayout.EndHorizontal();

        /* Expand stuff */

        if (db.npcDiags[ansID].expand)
        {
            GUIStyle st = new GUIStyle(GUI.skin.label);
            Vector2 coff = st.contentOffset;
            coff.y -= 5;
            st.contentOffset = coff;
            GUILayout.Label("_________________________________________", st);
            coff.y += 7;
            st.contentOffset = coff;
            st.fontStyle = FontStyle.Bold;

            EditorGUI.BeginChangeCheck();
            db.npcDiags[ansID].sprite = (Sprite) EditorGUILayout.ObjectField("Node Sprite: ", db.npcDiags[ansID].sprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                needSave = true;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Extra Variables: ", st);
            if (GUILayout.Button("Add"))
            {
                db.npcDiags[ansID].vars.Add(string.Empty);
                db.npcDiags[ansID].varKeys.Add("Key" + db.npcDiags[ansID].vars.Count.ToString());
                needSave = true;
            }

            GUILayout.EndHorizontal();
            for (int i = 0; i < db.npcDiags[ansID].vars.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i.ToString() + ". ", GUILayout.Width(20));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    db.npcDiags[ansID].vars.RemoveAt(i);
                    db.npcDiags[ansID].varKeys.RemoveAt(i);
                needSave = true;
                    break;
                }

                EditorGUI.BeginChangeCheck();
                db.npcDiags[ansID].varKeys[i] = EditorGUILayout.TextField(db.npcDiags[ansID].varKeys[i], GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                    needSave = true;

                EditorGUI.BeginChangeCheck();
                db.npcDiags[ansID].vars[i] = EditorGUILayout.TextField(db.npcDiags[ansID].vars[i]);
                if (EditorGUI.EndChangeCheck())
                    needSave = true;


                GUILayout.EndHorizontal();

            }

        }



        if (Event.current.commandName == "UndoRedoPerformed")
            Repaint();
        GUI.DragWindow();
    }

    //The Action node
    void DrawActionWindow(int id)
    {
        GUI.enabled = editEnabled;
        bool dontDrag = false;
        if (Event.current.type == EventType.MouseUp)
        {
            draggingLine = false;
            dontDrag = true;
        }
        int aID = id - (db.playerDiags.Count) - (db.npcDiags.Count);
        if (aID < 0)
            aID = 0;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("+", GUILayout.Width(30)))
        {
            db.actionNodes[aID].more = !db.actionNodes[aID].more;
        }

        GUI.color = colors[2];
        string delText = "Delete Node";
        if (areYouSureIndex == id)
            if (areYouSure) { delText = "Sure?"; GUI.color = new Color32(176, 128, 54, 255); }
        if (GUILayout.Button(delText))
        {
            if (areYouSureIndex != id) areYouSure = false;
            if (!areYouSure)
            {
                areYouSure = true;
                areYouSureIndex = id;
            }
            else
            {
                areYouSure = false;
                areYouSureIndex = 0;
                Undo.RecordObject(db, "removed node");
                removeAction(db.actionNodes[aID]);
                needSave = true;
                return;
            }
        }
        if (Event.current.type == EventType.MouseDown)
        {
            areYouSure = false;
            Repaint();
        }

        GUI.color = defaultColor;

        GUIStyle stf = new GUIStyle(GUI.skin.textField);
        stf.wordWrap = true;
        GUI.SetNextControlName("nText_" + id.ToString());


        if (db.actionNodes[aID].outPlayer == null && db.actionNodes[aID].outAction == null && db.actionNodes[aID].outNPC == null)
        {
            Rect lr;
            GUI.color = Color.green;
            if (GUILayout.RepeatButton("O", GUILayout.Width(30)))
            {
                areYouSure = false;
                lr = GUILayoutUtility.GetLastRect();
                lr = new Rect(lr.x + db.actionNodes[aID].rect.x + 30, lr.y + db.actionNodes[aID].rect.y + 7, 0, 0);
                if (!draggingLine && !dontDrag)
                {
                    draggedAns = null;
                    draggedCom = null;
                    draggedAction = db.actionNodes[aID];
                    dragStart = new Vector2(lr.x, lr.y);
                    draggingLine = true;
                	needSave = true;
                }
            }
            GUI.color = Color.white;

        }
        else
        {
            if (GUILayout.Button("x", GUILayout.Width(30)))
            {
                areYouSure = false;
                breakConnection(2, null, null, db.actionNodes[aID]);
                needSave = true;
            }
        }

        GUILayout.EndHorizontal();
        GUI.color = Color.cyan;
        GUILayout.Label("___________________________");

        if (db.actionNodes[aID].more)
        {
            //OVERRIDE START NODE
            GUILayout.BeginHorizontal();
            GUILayout.Label("OvrStartNode");
            Undo.RecordObject(db, "Changed Override Start Node");
            EditorGUI.BeginChangeCheck();
            db.actionNodes[aID].ovrStartNode = EditorGUILayout.IntField(db.actionNodes[aID].ovrStartNode);
            if (EditorGUI.EndChangeCheck())
                needSave = true;
            GUILayout.EndHorizontal();
            //CHANGE DIALOGUE NAME
            GUILayout.BeginHorizontal();
            GUILayout.Label("RenameDialogue");
            Undo.RecordObject(db, "Set Rename dialogue");
            EditorGUI.BeginChangeCheck();
            db.actionNodes[aID].renameDialogue = EditorGUILayout.TextField(db.actionNodes[aID].renameDialogue);
            if (EditorGUI.EndChangeCheck())
                needSave = true;
            GUILayout.EndHorizontal();

        }
        else
        {
            if (GUILayout.Button("Reset and fetch"))
            {
                //GameObject obj = GameObject.Find(db.actionNodes[aID].gameObjectName);		
                //List<MethodInfo> methods = GetMethods(obj);

                var objects = Resources.FindObjectsOfTypeAll<GameObject>();
                db.actionNodes[aID].nameOpts.Clear();

                int c = 0;
                db.actionNodes[aID].nameOpts.Add("[No object]");

                foreach (GameObject g in objects)
                {
                    if (g.activeInHierarchy && checkUseful(g))
                        db.actionNodes[aID].nameOpts.Add(g.name);

                    c++;
                }

                db.actionNodes[aID].Clean();

                //Fill up methods dictionary
                var gos = Resources.FindObjectsOfTypeAll<GameObject>();
                db.actionNodes[aID].methods = new Dictionary<string, string>();
                for (int i = 0; i < gos.Length; i++)
                {
                    if (gos[i].activeInHierarchy && checkUseful(gos[i]))
                    {
                        List<MethodInfo> methodz = GetMethods(gos[i]);

                        for (int ii = 0; ii < methodz.Count; ii++)
                        {
                            if (!db.actionNodes[aID].methods.ContainsKey(gos[i].name + ii.ToString()))
                                db.actionNodes[aID].methods.Add(gos[i].name + ii.ToString(), methodz[ii].Name);
                        }
                    }
                }

                db.actionNodes[aID].opts = new string[] { "[No method]" };

                needSave = true;
            }

            GUI.color = Color.white;

            if (!db.actionNodes[aID].editorRefreshed && Event.current.type == EventType.repaint)
            {
                db.actionNodes[aID].editorRefreshed = true;

                if (db.actionNodes[aID].nameIndex != 0)
                {
                    db.actionNodes[aID].gameObjectName = db.actionNodes[aID].nameOpts[db.actionNodes[aID].nameIndex];
                }
                else
                {
                    db.actionNodes[aID].gameObjectName = "[No object]";
                    db.actionNodes[aID].methodName = "[No method]";
                    db.actionNodes[aID].methodIndex = 0;
                    db.actionNodes[aID].paramType = -1;
                }

                if (db.actionNodes[aID].methodIndex != 0)
                {
                    db.actionNodes[aID].methodName = db.actionNodes[aID].opts[db.actionNodes[aID].methodIndex];
                }
                else
                {
                    db.actionNodes[aID].methodName = "[No method]";
                    db.actionNodes[aID].methodIndex = 0;
                    db.actionNodes[aID].paramType = -1;
                }

                Repaint();
                return;
            }

            if (db.actionNodes[aID].nameOpts.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(db, "Changed name Index");
                db.actionNodes[aID].nameIndex = EditorGUILayout.Popup(db.actionNodes[aID].nameIndex, db.actionNodes[aID].nameOpts.ToArray());
                if (EditorGUI.EndChangeCheck()) //Pick name
                {

                    db.actionNodes[aID].gameObjectName = db.actionNodes[aID].nameOpts[db.actionNodes[aID].nameIndex];
                    db.actionNodes[aID].methodName = "[No method]";
                    db.actionNodes[aID].methodIndex = 0;
                    db.actionNodes[aID].paramType = -1;
                    //db.actionNodes[aID].editorRefreshed = false;

                    List<string> opti = new List<string>();
                    opti.Add("[No method]");

                    for (int x = 0; x < 10000; x++)
                    {
                        if (db.actionNodes[aID].methods.ContainsKey(db.actionNodes[aID].gameObjectName + x.ToString()))
                        {
                            opti.Add(db.actionNodes[aID].methods[db.actionNodes[aID].gameObjectName + x.ToString()]);
                        }
                        else
                        {
                            break;
                        }
                    }
                    db.actionNodes[aID].opts = opti.ToArray();

                    /*GameObject obj = GameObject.Find(db.actionNodes[aID].gameObjectName);
                    List<MethodInfo> methods = GetMethods(obj);
                    db.actionNodes[aID].opts = GetOptions(methods);*/
                    needSave = true;
                }
            }


            //if (db.actionNodes[aID].opts.Length > 1)
            //{

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(db, "Changed method index");
            db.actionNodes[aID].methodIndex = EditorGUILayout.Popup(db.actionNodes[aID].methodIndex, db.actionNodes[aID].opts);

            if (EditorGUI.EndChangeCheck()) //Pick method
            {
                db.actionNodes[aID].methodName = db.actionNodes[aID].opts[db.actionNodes[aID].methodIndex];

                GameObject ob = GameObject.Find(db.actionNodes[aID].gameObjectName);
                var objects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == db.actionNodes[aID].gameObjectName);

                foreach (GameObject g in objects)
                {
                    if (g.activeInHierarchy && checkUseful(g))
                    {
                        ob = g;
                        break;
                    }

                }

                List<MethodInfo> methods = GetMethods(ob);


                if (db.actionNodes[aID].methodIndex > 0)
                    db.actionNodes[aID].paramType = checkParam(methods[db.actionNodes[aID].methodIndex - 1]);
                else
                    db.actionNodes[aID].paramType = -1;

                needSave = true;

            }

            GUI.color = Color.white;
            GUILayout.BeginHorizontal();


            if (db.actionNodes[aID].paramType > 0)
                GUILayout.Label("Param: ", GUILayout.Width(60));


            if (db.actionNodes[aID].paramType == 1)
            {
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(db, "Changed param");
                db.actionNodes[aID].param_bool = EditorGUILayout.Toggle(db.actionNodes[aID].param_bool, GUILayout.Width(50));
                if (EditorGUI.EndChangeCheck()) //Pick method
                {
                    needSave = true;
                }
            }

            if (db.actionNodes[aID].paramType == 2)
            {
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(db, "Changed param");
                db.actionNodes[aID].param_string = EditorGUILayout.TextField(db.actionNodes[aID].param_string, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck()) //Pick method
                {
                    needSave = true;
                }
            }
            if (db.actionNodes[aID].paramType == 3)
            {
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(db, "Changed param");
                db.actionNodes[aID].param_int = EditorGUILayout.IntField(db.actionNodes[aID].param_int, new GUIStyle(GUI.skin.textField), GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck()) //Pick method
                {
                    needSave = true;
                }
            }
            if (db.actionNodes[aID].paramType == 4)
            {
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(db, "Changed param");
                db.actionNodes[aID].param_float = EditorGUILayout.FloatField(db.actionNodes[aID].param_float, new GUIStyle(GUI.skin.textField), GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck()) //Pick method
                {
                    needSave = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        
            
        //}

        GUILayout.Label("___________________________");
            if (db.actionNodes[aID].pauseHere) GUI.color = Color.green; else GUI.color = Color.white;
            if (GUILayout.Button("Pause Here: " + db.actionNodes[aID].pauseHere.ToString()))
            {
                Undo.RecordObject(db, "Changed pause");
                db.actionNodes[aID].pauseHere = !db.actionNodes[aID].pauseHere;
				needSave = true;
            }
            GUI.color = Color.white;
        


        //}


        if (Event.current.commandName == "UndoRedoPerformed")
            Repaint();


        GUI.DragWindow();



    }

    bool notInBlackList(MonoBehaviour mb)
    {
        for (int i = 0; i < namespaceBlackList.Length; i++){
            if (mb.GetType().Namespace != null && mb.GetType().Namespace.Contains(namespaceBlackList[i]))
                return false;
        }
        return true;   
    }

    bool checkUseful(GameObject g)
    {
        bool useful = false;
        var methods = new List<MethodInfo>();
        var mbs = g.GetComponents<MonoBehaviour>();

        var publicFlags = bf.Instance | bf.Public | bf.DeclaredOnly | bf.IgnoreReturn;

        foreach (MonoBehaviour mb in mbs)
        {
            if (notInBlackList(mb))
            {
                methods.AddRange(mb.GetType().GetMethods(publicFlags));
            }
        }

        string[] ops = GetOptions(methods);

        if (ops.Length > 1)
            useful = true;
        else
            useful = false;
		
        if (mbs.Length < 1)
		useful = false;

        return useful;
    }

    int checkParam(MethodInfo m)
    {
        ParameterInfo[] ps = m.GetParameters();

        if (ps.Length == 1)
        {
            if (ps[0].ParameterType == typeof(System.Boolean))
            {
                return 1;
            }
            if (ps[0].ParameterType == typeof(System.String))
            {
                return 2;
            }
            if (ps[0].ParameterType == typeof(System.Int32))
            {
                return 3;
            }
            if (ps[0].ParameterType == typeof(System.Single))
            {
                return 4;
            }
            return -1; 
        }

        if (ps.Length > 1)
        {
            return -1;
        }
        else
        {
            return 0; 
        }
    }

    string[] GetOptions(List<MethodInfo> ms)
    {
        List<string> str = new List<string>();

        str.Add("[No object]");

        foreach (MethodInfo m in ms)
        {
            ParameterInfo[] ps = m.GetParameters();

            if (ps.Length < 2 && m.ReturnType == typeof(void))
            {
                if (checkParam(m) > -1)
                    str.Add(m.Name);
            }
        }
        return str.ToArray();
    }

    List<MethodInfo> GetMethods(GameObject obj)
    {
        var methods = new List<MethodInfo>();
        var methodsFiltered = new List<MethodInfo>();

        if (obj == null) {return methods; }

        var mbs = obj.GetComponents<MonoBehaviour>();

        var publicFlags = bf.Instance | bf.Public | bf.DeclaredOnly | bf.IgnoreReturn;

        foreach (MonoBehaviour mb in mbs)
        {
            if (notInBlackList(mb))
            {
                methods.AddRange(mb.GetType().GetMethods(publicFlags));
            }
        }

        foreach (MethodInfo m in methods)
        {
            if (checkParam(m) > -1)
            {
                methodsFiltered.Add(m);
            }
        }

        return methodsFiltered;
    }



    //The Editor Tools 
	
//    void DrawStartWindow(int id)
//    {
//        GUI.enabled = editEnabled;
//        GUIStyle titleSt = new GUIStyle(GUI.skin.GetStyle("Label"));
//        titleSt.fontStyle = FontStyle.Bold;
//        GUILayout.BeginHorizontal();
//        GUILayout.Label("Currently editing: ", titleSt, GUILayout.Width(120));
//        int t_file = fileIndex;
//
//        if (saveNames.Count > 0)
//        {
//            EditorGUI.BeginChangeCheck();
//            fileIndex = EditorGUILayout.Popup(fileIndex, saveNames.ToArray());
//            if (EditorGUI.EndChangeCheck())
//            {
//                if (t_file != fileIndex)
//                {
//                    if (/*autosaveON &&*/ npcReady && playerReady)
//                    {
//                        Save();
//                    }
//                    currentDiag = fileIndex;
//                    saveEditorSettings(currentDiag);
//                    Load(true);
//                }
//            }
//        }
//
//        GUILayout.EndHorizontal();
//
//        if (Event.current.type == EventType.MouseUp && dragNewNode > 0) // Stop dragging windows
//        {
//            addNewNode(Event.current.mousePosition, dragNewNode);
//            dragNewNode = 0;
//            Repaint();
//        }
//
//        GUILayout.BeginHorizontal();
//        GUI.color = Color.green;
//        if (GUILayout.Button("Add new dialogue"))
//        {
//            editEnabled = false;
//            newFile = true;
//            GUI.FocusWindow(99998);
//        }
//        GUI.color = defaultColor;
//
//        if (saveNames.Count > 0)
//            if (GUILayout.Button("Delete current", GUILayout.Width(100)))
//            {
//                editEnabled = false;
//                deletePopup = true;
//            }
//        GUILayout.EndHorizontal();
//
//        if (saveNames.Count > 0)
//        {
//            GUILayout.BeginHorizontal();
//            GUIStyle bb = new GUIStyle(GUI.skin.label);
//            bb.fontStyle = FontStyle.Bold;
//            bb.normal.textColor = Color.red;
//            if (!showError)
//            {
//                if (needSave) GUI.color = Color.yellow;
//                if (GUILayout.Button("Save"))
//                {
//                    editEnabled = false;
//                    overwritePopup = true;
//                }
//                GUI.color = defaultColor;
//                if (needSave) GUI.color = defaultColor;
//                //autosaveON = GUILayout.Toggle(autosaveON, "Autosave");
//            }
//            else
//            {
//                GUILayout.Label("Check your Start and End Nodes!", bb);
//            }
//            GUILayout.EndHorizontal();
//
//            GUI.enabled = true;
//            GUILayout.Label("Add nodes: ", titleSt, GUILayout.Width(100));
//            GUILayout.BeginHorizontal();
//            GUI.color = Color.cyan;
//
//            // ADD NEW BUTTONS
//            Rect lr;
//
//            if (dragNewNode == 1)
//                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(40));
//            else
//                GUILayout.Box(newNodeIcon, GUILayout.Width(100), GUILayout.Height(40));
//            lr = GUILayoutUtility.GetLastRect();
//            if (editEnabled && lr.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
//            {
//                dragNewNode = 1;
//            }
//
//            if (dragNewNode == 2)
//                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(40));
//            else
//                GUILayout.Box(newNodeIcon2, GUILayout.Width(100), GUILayout.Height(40));
//            lr = GUILayoutUtility.GetLastRect();
//            if (editEnabled && lr.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
//            {
//                dragNewNode = 2;
//            }
//
//            if (dragNewNode == 3)
//                GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(40));
//            else
//                GUILayout.Box(newNodeIcon3, GUILayout.Width(100), GUILayout.Height(40));
//            lr = GUILayoutUtility.GetLastRect();
//            if (editEnabled && lr.Contains(Event.current.mousePosition) && Event.current.type == EventType.mouseDown)
//            {
//                dragNewNode = 3;
//            }
//
//            GUILayout.EndHorizontal();
//
//            GUI.color = defaultColor;
//
//            GUILayout.BeginHorizontal();
//            GUILayout.Label("General settings: ", titleSt, GUILayout.Width(130));
//            string showText = (showSettings == true) ? "Hide" : "Show";
//            if (GUILayout.Button(showText, GUILayout.Width(50)))
//            {
//                showSettings = !showSettings;
//                needSave = true;
//                if (!showSettings)
//                    startDiag.height = 10;
//            }
//            GUILayout.EndHorizontal();
//
//            if (showSettings)
//            {
//                GUILayout.Space(10);
//                if (!hasID) { GUI.color = Color.red; }
//                EditorGUI.BeginChangeCheck();
//                GUILayout.BeginHorizontal();
//                GUILayout.Label("Start Node ID: ", titleSt);
//                Undo.RecordObject(db, "changed start node");
//                startID = EditorGUILayout.IntField(startID);
//                GUILayout.Label("Load Tag: ");
//                Undo.RecordObject(db, "changed load tag");
//                loadTag = EditorGUILayout.TextField(loadTag);
//                GUILayout.EndHorizontal();
//                if (EditorGUI.EndChangeCheck())
//                    needSave = true;
//
//                GUI.color = defaultColor;
//                GUILayout.BeginHorizontal();
//
//                GUILayout.Label("Center View: ", GUILayout.Width(100));
//                if (GUILayout.Button("On All"))
//                {
//                    CenterAll(true);
//                    Repaint();
//                }
//                if (GUILayout.Button("On Node"))
//                {
//                    CenterAll(false);
//                    Repaint();
//                }
//                GUILayout.EndHorizontal();
//
//                EditorGUI.BeginChangeCheck();
//                Undo.RecordObject(db, "Changed preview");
//                previewPanning = GUILayout.Toggle(previewPanning, "Performance panning");
//                if (EditorGUI.EndChangeCheck())
//                    needSave = true;
//
//            }
//
//
//            //GUILayout.Label("Start Node ID: ");
//
//        }
//
//        if (dragNewNode == 0)
//            GUI.DragWindow();
//    }
	
    void DrawNewFileWindow(int id)
    {
        GUI.FocusControl("createFile");
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.alignment = TextAnchor.UpperCenter;
        st.fontSize = 16;
        st.fontStyle = FontStyle.Bold;
        GUILayout.Label("Please name your new dialogue:", st);
        GUIStyle stf = new GUIStyle(GUI.skin.textField);
        stf.fontSize = 14;
        stf.alignment = TextAnchor.MiddleCenter;
        GUI.SetNextControlName("createFile");
        newFileName = GUILayout.TextField(newFileName, stf, GUILayout.Height(40));
		newFileName = Regex.Replace(newFileName, @"[^a-zA-Z0-9_$&#]", "");
        GUI.color = Color.green;
        if (GUILayout.Button("Create", GUILayout.Height(30)))
        {
            if (tryCreate(newFileName))
            {
                fileIndex = currentDiag;
                newFileName = "My Dialogue";
                editEnabled = true;
                newFile = false;
                errorMsg = "";
                needSave = true;
                Load(true);
                Repaint();
                saveEditorSettings(currentDiag);
            }
            else
            {
                errorMsg = "File already exists!";
            }
        }
        if (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.keyUp)
        {
            if (tryCreate(newFileName))
            {
                fileIndex = currentDiag;
                newFileName = "My Dialogue";
                editEnabled = true;
                newFile = false;
                errorMsg = "";
                needSave = true;
                Load(true);
                Repaint();
                saveEditorSettings(currentDiag);
            }
            else
            {
                errorMsg = "File already exists!";
            }
        }
        GUI.color = defaultColor;
        if (GUILayout.Button("Cancel", GUILayout.Height(20)) || Event.current.keyCode == KeyCode.Escape)
        {
            newFileName = "My Dialogue";
            editEnabled = true;
            newFile = false;
            errorMsg = "";
            Repaint();
        }
        st.normal.textColor = Color.red;
        GUILayout.Label(errorMsg, st);
    }

    void DrawOverwriteWindow(int id)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.alignment = TextAnchor.UpperCenter;
        st.fontSize = 16;
        st.fontStyle = FontStyle.Bold;

        if (saveNames.Count > 0)
        {
            if (File.Exists(Application.dataPath + "/" + pathToVide + "VIDE/Resources/Dialogues/" + saveNames[currentDiag] + ".json"))
            {
                GUILayout.Label("File Already Exists! Overwrite?", st);
                if (GUILayout.Button("Yes!", GUILayout.Height(30)))
                {
                    Save();
                    needSave = false;
                    newFileName = "My Dialogue";
                    editEnabled = true;
                    overwritePopup = false;
                    newFile = false;
                    errorMsg = "";
                    saveEditorSettings(currentDiag);
                }
                if (GUILayout.Button("No", GUILayout.Height(20)))
                {
                    newFileName = "My Dialogue";
                    editEnabled = true;
                    overwritePopup = false;
                    newFile = false;
                    errorMsg = "";
                }
                GUILayout.Space(10);
            }
        }
        GUILayout.Label("Save as new...", st);
        GUIStyle stf = new GUIStyle(GUI.skin.textField);
        stf.fontSize = 14;
        stf.alignment = TextAnchor.MiddleCenter;
        newFileName = GUILayout.TextField(newFileName, stf, GUILayout.Height(40));
		newFileName = Regex.Replace(newFileName, @"[^a-zA-Z0-9_$&#]", "");
        if (GUILayout.Button("Save", GUILayout.Height(20)))
        {
            if (tryCreate(newFileName))
            {
                fileIndex = currentDiag;
                Load(false);
                newFileName = "My Dialogue";
                editEnabled = true;
                newFile = false;
                overwritePopup = false;
                errorMsg = "";
                needSave = true;
            }
            else
            {
                errorMsg = "File already exists!";
            }
        }
        if (GUILayout.Button("Cancel", GUILayout.Height(20)))
        {
            newFileName = "My Dialogue";
            editEnabled = true;
            overwritePopup = false;
            newFile = false;
            errorMsg = "";
        }
        st.normal.textColor = Color.red;
        GUILayout.Label(errorMsg, st);
    }

    void DrawDeleteWindow(int id)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label);
        st.alignment = TextAnchor.UpperCenter;
        st.fontSize = 16;
        st.fontStyle = FontStyle.Bold;
        GUILayout.Label("Are you sure you want to delete " + "'" + saveNames[fileIndex] + "'?", st);
        GUILayout.Label("Some VIDE_Assign might still have this dialogue assigned to it", st);
		
        if (GUILayout.Button("Yes", GUILayout.Height(30)) || Event.current.keyCode == KeyCode.Return)
        {
            DeleteDiag();
            fileIndex = 0;
            editEnabled = true;
            deletePopup = false;
            newFile = false;
            saveEditorSettings(currentDiag);
        }
        if (GUILayout.Button("No", GUILayout.Height(20)) || Event.current.keyCode == KeyCode.Escape)
        {
            editEnabled = true;
            deletePopup = false;
            newFile = false;
        }
    }

    void DrawLines()
    {
        Handles.color = colors[3];
        if (editEnabled)
        {
            if (draggingLine)
            {
                DrawNodeLine3(dragStart, Event.current.mousePosition);
                Repaint();
            }
            for (int i = 0; i < db.playerDiags.Count; i++)
            {
                for (int ii = 0; ii < db.playerDiags[i].comment.Count; ii++)
                {
                    if (db.playerDiags[i].comment[ii].outputAnswer != null)
                    {
                        DrawNodeLine(db.playerDiags[i].comment[ii].outRect,
                        db.playerDiags[i].comment[ii].outputAnswer.rect, db.playerDiags[i].rect);
                    }

                    if (db.playerDiags[i].comment[ii].outAction != null)
                    {
                        DrawNodeLine(db.playerDiags[i].comment[ii].outRect,
                        db.playerDiags[i].comment[ii].outAction.rect, db.playerDiags[i].rect);
                    }
                }


            }
            for (int i = 0; i < db.npcDiags.Count; i++)
            {
                if (db.npcDiags[i].outputSet != null)
                {
                    DrawNodeLine2(db.npcDiags[i].rect,
                    db.npcDiags[i].outputSet.rect);
                }

                if (db.npcDiags[i].outputNPC != null)
                {
                    DrawNodeLine2(db.npcDiags[i].rect,
                    db.npcDiags[i].outputNPC.rect);
                }

                if (db.npcDiags[i].outAction != null)
                {
                    DrawNodeLine2(db.npcDiags[i].rect,
                    db.npcDiags[i].outAction.rect);
                }
            }
            for (int i = 0; i < db.actionNodes.Count; i++)
            {
                if (db.actionNodes[i].outPlayer != null)
                {
                    DrawActionNodeLine(db.actionNodes[i].rect,
                    db.actionNodes[i].outPlayer.rect);
                }

                if (db.actionNodes[i].outNPC != null)
                {
                    DrawActionNodeLine(db.actionNodes[i].rect,
                    db.actionNodes[i].outNPC.rect);
                }
                if (db.actionNodes[i].outAction != null)
                {
                    DrawActionNodeLine(db.actionNodes[i].rect,
                    db.actionNodes[i].outAction.rect);
                }
            }
        }
            repaintLines = false;
		
    }

    //Player Node to NPC Node
    void DrawNodeLine(Rect start, Rect end, Rect sPos)
    {
        Color nc = Color.black;
        Color nc2 = colors[1];
        nc.a = 0.25f;
        nc2.r -= 0.15f;
        nc2.g -= 0.15f;
        nc2.b -= 0.15f;
        nc2.a = 1;

        //Draw shadow line
        Handles.DrawBezier(
           new Vector3(start.x + sPos.x + 35, start.y + sPos.y + 12, 0),
           new Vector3(end.x+ (end.width / 2), end.y + (end.height / 2) + 2, 0),
           new Vector3(end.x + (end.width / 2), end.y + (end.height / 2) + 2, 0),
           new Vector3(start.x + sPos.x + 35, start.y + sPos.y + 12, 0),
           nc,
           null,
           7
           );
        //Draw main line
        Handles.DrawBezier(
            new Vector3(start.x + sPos.x + 30, start.y + sPos.y + 10, 0),
            new Vector3(end.x + (end.width / 2), end.y + (end.height / 2), 0),
            new Vector3(end.x + (end.width / 2), end.y + (end.height / 2), 0),
            new Vector3(start.x + sPos.x + 30, start.y + sPos.y + 10, 0),
            colors[1],
            null,
            5
            );

        //Draw arrow
        Handles.BeginGUI();
        Vector2 vs = new Vector2(start.x + sPos.x + 30, start.y + sPos.y + 12);
        Vector2 ve = new Vector2(end.x +(end.width / 2), end.y + (end.height / 2));

        float ab = Vector2.Distance(vs, ve);
        float dist = 0.05f;
        Vector2 cen = Vector2.Lerp(vs, ve, dist);
        int tries = 20;
        while (sPos.Contains(cen) && ab > 100 && tries > 0)
        {
            dist += 0.05f;
            cen = Vector2.Lerp(vs, ve, dist);
            tries--;
        }
        dist += 0.1f;
        cen = Vector2.Lerp(vs, ve, dist);

        float rot = AngleBetweenVector2(vs, ve);
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(rot + 90, new Vector2(cen.x, cen.y));
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cen.x - 20, cen.y - 20, 40, 40), lineIcon, ScaleMode.StretchToFill);
        GUI.color = Color.white;
        GUI.matrix = matrixBackup;
        Handles.EndGUI();
		
		if (repaintLines)
        {
            Repaint();
        }
    }

    //NPC Node to Player Node
    void DrawNodeLine2(Rect start, Rect end)
    {
        Color nc = Color.black;
        Color nc2 = colors[2];
        nc.a = 0.25f;
        nc2.r -= 0.15f;
        nc2.g -= 0.15f;
        nc2.b -= 0.15f;
        nc2.a = 1;
        Handles.DrawBezier(
       new Vector3(start.x + 295, start.y + 52, 0),
       new Vector3(end.x + (end.width / 2), end.y + (end.height / 2) + 2, 0),
       new Vector3(end.x + (end.width / 2), end.y + (end.height / 2) + 2, 0),
       new Vector3(start.x + 295, start.y + 52, 0),
       nc,
       null,
       7
       );
        Handles.DrawBezier(
            new Vector3(start.x + 295, start.y + 50, 0),
            new Vector3(end.x + (end.width / 2), end.y + (end.height / 2), 0),
            new Vector3(end.x + (end.width / 2), end.y + (end.height / 2), 0),
            new Vector3(start.x + 295, start.y + 50, 0),
            nc2,
            null,
            5
            );

        Handles.BeginGUI();
        Vector2 vs = new Vector2(start.x + 295, start.y + 52);
        Vector2 ve = new Vector2(end.x + (end.width / 2), end.y + (end.height / 2));
        float ab = Vector2.Distance(vs, ve);
        float dist = 0.05f;
        Vector2 cen = Vector2.Lerp(vs, ve, dist);
        int tries = 20;
        while (start.Contains(cen) && ab > 100 && tries > 0)
        {
            dist += 0.05f;
            cen = Vector2.Lerp(vs, ve, dist);
            tries--;
        }
        dist += 0.1f;
        cen = Vector2.Lerp(vs, ve, dist);
        float rot = AngleBetweenVector2(vs, ve);
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(rot + 90, new Vector2(cen.x, cen.y));
        GUI.color = Color.white; ;
        GUI.DrawTexture(new Rect(cen.x - 20, cen.y - 20, 40, 40), lineIcon, ScaleMode.StretchToFill);
        GUI.color = Color.white;
        GUI.matrix = matrixBackup;
        Handles.EndGUI();

        if (repaintLines)
        {
            Repaint();
        }
    }

    //Action Node line
    void DrawActionNodeLine(Rect start, Rect end)
    {
        Color nc = Color.black;
        Color nc2 = colors[2];
        nc.a = 0.25f;
        nc2.r -= 0.15f;
        nc2.g -= 0.15f;
        nc2.b -= 0.15f;
        nc2.a = 1;
        Handles.DrawBezier(
       new Vector3(start.x + 190, start.y + 30, 0),
       new Vector3(end.x + (end.width / 2), end.y + (end.height / 2) + 2, 0),
       new Vector3(end.x + (end.width / 2), end.y + (end.height / 2) + 2, 0),
       new Vector3(start.x + 190, start.y + 30, 0),
       nc,
       null,
       7
       );
        Handles.DrawBezier(
            new Vector3(start.x + 190, start.y + 28, 0),
            new Vector3(end.x + (end.width / 2), end.y + (end.height / 2), 0),
            new Vector3(end.x + (end.width / 2), end.y + (end.height / 2), 0),
            new Vector3(start.x + 190, start.y + 28, 0),
            nc2,
            null,
            5
            );

        Handles.BeginGUI();
        Vector2 vs = new Vector2(start.x + 190, start.y + 30);
        Vector2 ve = new Vector2(end.x + (end.width / 2), end.y + (end.height / 2));
        float ab = Vector2.Distance(vs, ve);
        float dist = 0.05f;
        Vector2 cen = Vector2.Lerp(vs, ve, dist);
        int tries = 20;
        while (start.Contains(cen) && ab > 100 && tries > 0)
        {
            dist += 0.05f;
            cen = Vector2.Lerp(vs, ve, dist);
            tries--;
        }
        dist += 0.1f;
        cen = Vector2.Lerp(vs, ve, dist);

        float rot = AngleBetweenVector2(vs, ve);
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(rot + 90, new Vector2(cen.x, cen.y));
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cen.x - 20, cen.y - 20, 40, 40), lineIcon, ScaleMode.StretchToFill);
        GUI.color = Color.white;
        GUI.matrix = matrixBackup;
        Handles.EndGUI();

        if (repaintLines)
        {
            Repaint();
        }
    }

    //Connection line
    void DrawNodeLine3(Vector2 start, Vector2 end)
    {
        Handles.DrawBezier(
            new Vector3(start.x, start.y, 0),
            new Vector3(end.x, end.y, 0),
            new Vector3(end.x, end.y, 0),
            new Vector3(start.x, start.y, 0),
            colors[0],
            null,
            5
            );

        Handles.BeginGUI();
        Vector2 vs = new Vector2(start.x, start.y);
        Vector2 ve = new Vector2(end.x, end.y);
        Vector2 cen = Vector2.Lerp(vs, ve, 0.5f);
        float rot = AngleBetweenVector2(vs, ve);
        Matrix4x4 matrixBackup = GUI.matrix;
        GUIUtility.RotateAroundPivot(rot + 90, new Vector2(cen.x, cen.y));
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cen.x - 15, cen.y - 15, 30, 30), lineIcon, ScaleMode.StretchToFill);
        GUI.color = Color.white;
        GUI.matrix = matrixBackup;
        Handles.EndGUI();
    }

    private float AngleBetweenVector2(Vector2 vec1, Vector2 vec2)
    {
        Vector2 diference = vec2 - vec1;
        float sign = (vec2.y < vec1.y) ? -1.0f : 1.0f;
        return Vector2.Angle(Vector2.right, diference) * sign;
    }

    //Clean the database
    void ClearAll()
    {
        db.npcDiags = new List<VIDE_EditorDB.Answer>();
        db.playerDiags = new List<VIDE_EditorDB.CommentSet>();
        db.actionNodes = new List<VIDE_EditorDB.ActionNode>();
    }
	
	void addNewNode(Vector2 pos, int type){
		
		if (!startDiag.Contains(pos)){
			switch(type){
				case 1:
					db.playerDiags.Add(new VIDE_EditorDB.CommentSet(pos, setUniqueID()));
					break;
					case 2:
					db.npcDiags.Add(new VIDE_EditorDB.Answer(pos, setUniqueID()));
					break;
					case 3:
					db.actionNodes.Add(new VIDE_EditorDB.ActionNode(pos , setUniqueID()));
					break;
				}
				needSave = true;
		} else {
						switch(type){
				case 1:
					db.playerDiags.Add(new VIDE_EditorDB.CommentSet(startDiag, setUniqueID()));
					break;
					case 2:
					db.npcDiags.Add(new VIDE_EditorDB.Answer(startDiag, setUniqueID()));
					break;
					case 3:
					db.actionNodes.Add(new VIDE_EditorDB.ActionNode(startDiag , setUniqueID()));
					break;
				}
				needSave = true;
		}
		dragNewNode = 0;
		Repaint();
	}

}