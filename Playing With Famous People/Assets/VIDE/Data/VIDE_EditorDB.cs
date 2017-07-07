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
using System;
using System.Collections;
using System.Collections.Generic;

public class VIDE_EditorDB : MonoBehaviour, ISerializationCallbackReceiver
{

    /*
     * Here is were we store all of the temporal data generated on the VIDE Editor. 
     * When the VIDE Editor saves, it will save all of the data we store here into a json file.
     * Since the VIDE Editor allows the creation of endless structures, this script also handles
     * serialization and deserialization of data in order to avoid object composition cycles and
     * to be able to implement Undo/Redo. 
     */

    public class ActionNode
    {
        public bool editorRefreshed = false;
        public bool pauseHere = false;
        public string gameObjectName = "[No object]";
        public string methodName = "[No method]";
        public int methodIndex; 
        public int nameIndex;
        public int paramType;
        public List<string> nameOpts = new List<string>(){"[No object]"};
        public string[] opts = new string[] {"[No method]"};
		public Dictionary<string, string> methods = new Dictionary<string, string>();

        public bool param_bool;
        public string param_string;
        public int param_int;
        public float param_float;

        public Rect rect;
        public int ID;
        public CommentSet outPlayer;
        public Answer outNPC;
        public ActionNode outAction;

        public bool more = false;
        public int ovrStartNode = -1;
        public string renameDialogue = String.Empty;

        public void Clean()
        {
            pauseHere = false;
            gameObjectName = "[NONE]";
            methodName = "[NONE]";
            methodIndex = 0;
            nameIndex = 0;
            ovrStartNode = -1;
            renameDialogue = String.Empty;
            more = false;
            paramType = -1;
        }

        public ActionNode(Rect pos, int id)
        {
            pauseHere = false;
            gameObjectName = "";
            methodName = "";
            ID = id;
            outAction = null;
            outPlayer = null;
            outNPC = null;
            rect = new Rect(pos.x, pos.y + 200, 300, 50);
        }
		public ActionNode(Vector2 pos, int id)
        {
            pauseHere = false;
            gameObjectName = "";
            methodName = "";
            ID = id;
            outAction = null;
            outPlayer = null;
            outNPC = null;
            rect = new Rect(pos.x-100, pos.y-75, 300, 50);
        }
        public ActionNode()
        {
            pauseHere = false;
            gameObjectName = "";
            methodName = "";
            outAction = null;
            outPlayer = null;
            outNPC = null;
        }
        public ActionNode(Vector2 rPos, int id, string meth, string goMeth, bool pau, bool pb, string ps, int pi, float pf)
        {
            pauseHere = pau;
            methodName = meth;
            gameObjectName = goMeth;

            param_bool = pb;
            param_string = ps;
            param_int = pi;
            param_float = pf;

            outPlayer = null;
            outNPC = null;
            outAction = null;
            rect = new Rect(rPos.x, rPos.y, 300, 50);
            ID = id;
        }

    }

    public class CommentSet
    {
        [NonSerialized]
        public List<Comment> comment;
        public Rect rect;
        public int ID;
        public string playerTag = "";
        public bool endConversation = false;

        public Sprite sprite;
        public bool expand;
        public List<string> vars = new List<string>();
        public List<string> varKeys = new List<string>();

        public CommentSet()
        {
            comment = new List<Comment>();
            rect = new Rect(20, 200, 300, 100);
        }

        public CommentSet(Rect pos, int id)
        {
            comment = new List<Comment>();
            comment.Add(new Comment());
            rect = new Rect(pos.x, pos.y + 200, 300, 100);
            ID = id;
        }
		
		public CommentSet(Vector2 pos, int id)
        {
            comment = new List<Comment>();
            comment.Add(new Comment());
            rect = new Rect(pos.x-150, pos.y-50, 300, 100);
            ID = id;
        }

        public CommentSet(Vector2 rectPos, int comSize, int id, string tag, bool endC)
        {
            rect = new Rect(rectPos.x, rectPos.y, 300, 100);
            endConversation = endC;
            comment = new List<Comment>();
            ID = id;
            playerTag = tag;
            for (int i = 0; i < comSize; i++)
                comment.Add(new Comment());
        }
    }

    public class Comment
    {
        public string text;
        public string extraData = "";
        public CommentSet inputSet;
        public Answer outputAnswer;
        public ActionNode outAction;
        public Rect outRect;

        public Comment()
        {
            outputAnswer = null;
            inputSet = null;
            outAction = null;
            text = "Comment...";
            extraData = "";

        }
        public Comment(CommentSet id)
        {
            outputAnswer = null;
            outAction = null;
            inputSet = id;
            text = "Comment...";
            extraData = "";
        }
    }

    public class Answer
    {
        public string text;
        [NonSerialized]
        public CommentSet outputSet;
        public Answer outputNPC;
        public ActionNode outAction;

        public Rect rect;

        public bool endConversation;
        public string extraData;
        public string tag;

        public int ID;

        public Sprite sprite;
        public bool expand;
        public List<string> vars = new List<string>();
        public List<string> varKeys = new List<string>();

        public Answer()
        {
            text = "NPC's comment...";
            outputSet = null;
            outputNPC = null;
            outAction = null;
            endConversation = true;
            rect = new Rect(20, 160, 300, 50);
            extraData = "";
            tag = "";
        }

        public Answer(Vector2 rPos, bool endC, string t, int id, string exData, string tagt)
        {
            text = t;
            outputSet = null;
            outputNPC = null;
            outAction = null;
            endConversation = endC;
            rect = new Rect(rPos.x, rPos.y, 300, 50);
            extraData = exData;
            tag = tagt;
            ID = id;
        }
        public Answer(Rect pos, int id)
        {
            extraData = "";
            tag = "";
            text = "NPC's comment...";
            outputSet = null;
            outputNPC = null;
            outAction = null;
            ID = id;
            endConversation = true;
            rect = new Rect(pos.x, pos.y + 200, 300, 50);
        }
		public Answer(Vector2 pos, int id)
        {
            extraData = "";
            tag = "";
            text = "NPC's comment...";
            outputSet = null;
            outputNPC = null;
            outAction = null;
            ID = id;
            endConversation = true;
            rect = new Rect(pos.x-150, pos.y-25, 300, 50);
        }
    }

    public List<CommentSet> playerDiags = new List<CommentSet>();
    public List<Answer> npcDiags = new List<Answer>();
    public List<ActionNode> actionNodes = new List<ActionNode>();

    //SERIALIZATION...

    public List<Serialized_playerDiags> S_playerDiags;
    public List<Serialized_npcDiags> S_npcDiags;
    public List<Serialized_actionNodes> S_actionNodes;


    public void OnBeforeSerialize()
    {
        npcSerialize();
        playerSerialize();
        actionSerialize();
    }

    public void OnAfterDeserialize()
    {
        if (S_npcDiags.Count > 0)
            npcDiags = npcDeserialize();
        else
            npcDiags = new List<Answer>();

        if (S_playerDiags.Count > 0)
            playerDiags = playerDeserialize();
        else
            playerDiags = new List<CommentSet>();

        if (S_actionNodes.Count > 0)
            actionNodes = actionDeserialize();
        else
            actionNodes = new List<ActionNode>();

        ConnectNodes();
    }

    [Serializable]
    public struct Serialized_npcDiags
    {
        public string extraData;
        public string tag;
        public string text;
        public int ID;
        public bool endConversation;
        public Rect rect;
        public int outNPCIndex;
        public int outActionIndex;
        public int outSetIndex;
        public bool expand;
        public Sprite sprite;
        public List<string> vars;
        public List<string> varKeys;
    }
    [Serializable]
    public struct Serialized_actionNodes
    {
        public bool pauseHere;
        public string gameObjectName;
        public string methodName;
        public int nameIndex;
        public int methodIndex;
        public int paramIndex;
        public string[] opts;
        public List<string> nameOpts;

        public bool param_bool;
        public string param_string;
        public int param_int;
        public float param_float;

        public Rect rect;
        public int ID;

        public int outNPCIndex;
        public int outPlayerIndex;
        public int outActionIndex;

        public bool more;
        public int ovrStartNode;
        public string renameDialogue;
    }
    [Serializable]
    public struct Serialized_playerDiags
    {
        public int commentCount;
        public bool endConversation;
        public List<Serialized_comment> s_comment;
        public int ID;
        public Rect rect;
        public string pTag;
        public bool expand;
        public Sprite sprite;
        public List<string> vars;
        public List<string> varKeys;
    }
    [Serializable]
    public struct Serialized_comment
    {
        public string text;
        public string extraData;
        public int inputSetIndex;
        public int outputAnswerIndex;
        public int outActionIndex;
        public Rect outRect;
    }

    void npcSerialize()
    {
        List<Serialized_npcDiags> S_npcDiag = new List<Serialized_npcDiags>();
        foreach (var child in npcDiags)
        {
            Serialized_npcDiags np = new Serialized_npcDiags()
            {
                extraData = child.extraData,
                tag = child.tag,
                text = child.text,
                ID = child.ID,
                endConversation = child.endConversation,
                rect = child.rect,
                outSetIndex = playerDiags.IndexOf(child.outputSet),
                outNPCIndex = npcDiags.IndexOf(child.outputNPC),
                outActionIndex = actionNodes.IndexOf(child.outAction),
                expand = child.expand,
                sprite = child.sprite,
                vars = child.vars,
                varKeys = child.varKeys

            };
            S_npcDiag.Add(np);
        }
        S_npcDiags = S_npcDiag;
    }

    void playerSerialize()
    {
        List<Serialized_playerDiags> S_playerDiag = new List<Serialized_playerDiags>();

        //Serialize commentSets
        foreach (var child in playerDiags)
        {
            Serialized_playerDiags np = new Serialized_playerDiags()
            {
                commentCount = child.comment.Count,
                ID = child.ID,
                rect = child.rect,
                pTag = child.playerTag,
                endConversation = child.endConversation,
                expand = child.expand,
                sprite = child.sprite,
                vars = child.vars,
                varKeys = child.varKeys
            };
            //Serialize comments inside this set
            np.s_comment = new List<Serialized_comment>();
            for (int i = 0; i < np.commentCount; i++)
            {
                Serialized_comment sc = new Serialized_comment()
                {
                    text = child.comment[i].text,
                    outRect = child.comment[i].outRect,
                    outputAnswerIndex = npcDiags.IndexOf(child.comment[i].outputAnswer),
                    outActionIndex = actionNodes.IndexOf(child.comment[i].outAction),
                    inputSetIndex = playerDiags.IndexOf(child),
                    extraData = child.comment[i].extraData
                };
                np.s_comment.Add(sc);
            }

            S_playerDiag.Add(np);
        }

        S_playerDiags = S_playerDiag;
    }

    void actionSerialize()
    {
        List<Serialized_actionNodes> S_actionNode = new List<Serialized_actionNodes>();
        foreach (var child in actionNodes)
        {
            Serialized_actionNodes np = new Serialized_actionNodes()
            {
                gameObjectName = child.gameObjectName,
                pauseHere = child.pauseHere,
                methodName = child.methodName,
                methodIndex = child.methodIndex,
                opts = child.opts,
                nameOpts = child.nameOpts,
                paramIndex = child.paramType,
                nameIndex = child.nameIndex,
                param_bool = child.param_bool,
                param_float = child.param_float,
                param_int = child.param_int,
                param_string = child.param_string,
                ID = child.ID,
                rect = child.rect,
                outPlayerIndex = playerDiags.IndexOf(child.outPlayer),
                outNPCIndex = npcDiags.IndexOf(child.outNPC),
                outActionIndex = actionNodes.IndexOf(child.outAction),
                more = child.more,
                ovrStartNode = child.ovrStartNode,
                renameDialogue = child.renameDialogue
            };
            S_actionNode.Add(np);
        }
        S_actionNodes = S_actionNode;
    }

    List<Answer> npcDeserialize()
    {
        List<Answer> temp_npcDiags = new List<Answer>();
        foreach (var child in S_npcDiags)
        {
            temp_npcDiags.Add(new Answer());
            temp_npcDiags[temp_npcDiags.Count - 1].text = child.text;
            temp_npcDiags[temp_npcDiags.Count - 1].endConversation = child.endConversation;
            temp_npcDiags[temp_npcDiags.Count - 1].ID = child.ID;
            temp_npcDiags[temp_npcDiags.Count - 1].rect = child.rect;
            temp_npcDiags[temp_npcDiags.Count - 1].extraData = child.extraData;
            temp_npcDiags[temp_npcDiags.Count - 1].tag = child.tag;
            temp_npcDiags[temp_npcDiags.Count - 1].expand = child.expand;
            temp_npcDiags[temp_npcDiags.Count - 1].sprite = child.sprite;
            temp_npcDiags[temp_npcDiags.Count - 1].vars = child.vars;
            temp_npcDiags[temp_npcDiags.Count - 1].varKeys = child.varKeys;
        }

        return temp_npcDiags;
    }

    List<CommentSet> playerDeserialize()
    {
        List<CommentSet> temp_playerDiags = new List<CommentSet>();
        foreach (var child in S_playerDiags)
        {
            temp_playerDiags.Add(new CommentSet());
            temp_playerDiags[temp_playerDiags.Count - 1].ID = child.ID;
            temp_playerDiags[temp_playerDiags.Count - 1].rect = child.rect;
            temp_playerDiags[temp_playerDiags.Count - 1].playerTag = child.pTag;
            temp_playerDiags[temp_playerDiags.Count - 1].endConversation = child.endConversation;
            temp_playerDiags[temp_playerDiags.Count - 1].sprite = child.sprite;
            temp_playerDiags[temp_playerDiags.Count - 1].vars = child.vars;
            temp_playerDiags[temp_playerDiags.Count - 1].varKeys = child.varKeys;
            temp_playerDiags[temp_playerDiags.Count - 1].expand = child.expand;

            for (int i = 0; i < child.commentCount; i++)
            {
                CommentSet s = temp_playerDiags[temp_playerDiags.Count - 1];
                s.comment.Add(new Comment());
                s.comment[i].text = child.s_comment[i].text;
                s.comment[i].extraData = child.s_comment[i].extraData;
                s.comment[i].outRect = child.s_comment[i].outRect;
            }
        }

        return temp_playerDiags;
    }

    List<ActionNode> actionDeserialize()
    {
        List<ActionNode> temp_actionNodes = new List<ActionNode>();
        foreach (var child in S_actionNodes)
        {
            temp_actionNodes.Add(new ActionNode());

            temp_actionNodes[temp_actionNodes.Count - 1].gameObjectName = child.gameObjectName;
            temp_actionNodes[temp_actionNodes.Count - 1].methodName = child.methodName;
            temp_actionNodes[temp_actionNodes.Count - 1].methodIndex = child.methodIndex;
            temp_actionNodes[temp_actionNodes.Count - 1].opts = child.opts;
            temp_actionNodes[temp_actionNodes.Count - 1].nameOpts = child.nameOpts;
            temp_actionNodes[temp_actionNodes.Count - 1].paramType = child.paramIndex;
            temp_actionNodes[temp_actionNodes.Count - 1].nameIndex = child.nameIndex;
            temp_actionNodes[temp_actionNodes.Count - 1].param_string = child.param_string;
            temp_actionNodes[temp_actionNodes.Count - 1].param_int = child.param_int;
            temp_actionNodes[temp_actionNodes.Count - 1].param_float = child.param_float;
            temp_actionNodes[temp_actionNodes.Count - 1].param_bool = child.param_bool;
            temp_actionNodes[temp_actionNodes.Count - 1].pauseHere = child.pauseHere;
            temp_actionNodes[temp_actionNodes.Count - 1].ID = child.ID;
            temp_actionNodes[temp_actionNodes.Count - 1].rect = child.rect;
            temp_actionNodes[temp_actionNodes.Count - 1].more = child.more;
            temp_actionNodes[temp_actionNodes.Count - 1].ovrStartNode = child.ovrStartNode;
            temp_actionNodes[temp_actionNodes.Count - 1].renameDialogue = child.renameDialogue;

        }

        return temp_actionNodes;
    }

    //Now we can connect all of the nodes 
    void ConnectNodes()
    {

        for (int i = 0; i < playerDiags.Count; i++) //Connect Player Nodes
        {
            for (int ii = 0; ii < playerDiags[i].comment.Count; ii++)
            {
                playerDiags[i].comment[ii].inputSet = playerDiags[i];

                if (S_playerDiags[i].s_comment[ii].outputAnswerIndex >= 0)
                    playerDiags[i].comment[ii].outputAnswer = npcDiags[S_playerDiags[i].s_comment[ii].outputAnswerIndex];
                else
                    playerDiags[i].comment[ii].outputAnswer = null;

                if (S_playerDiags[i].s_comment[ii].outActionIndex >= 0)
                    playerDiags[i].comment[ii].outAction = actionNodes[S_playerDiags[i].s_comment[ii].outActionIndex];
                else
                    playerDiags[i].comment[ii].outAction = null;
            }
        }

        for (int i = 0; i < npcDiags.Count; i++) //Connect NPC Nodes
        {
            if (S_npcDiags[i].outSetIndex >= 0)
                npcDiags[i].outputSet = playerDiags[S_npcDiags[i].outSetIndex];
            else
                npcDiags[i].outputSet = null;

            if (S_npcDiags[i].outNPCIndex >= 0)
                npcDiags[i].outputNPC = npcDiags[S_npcDiags[i].outNPCIndex];
            else
                npcDiags[i].outputNPC = null;

            if (S_npcDiags[i].outActionIndex >= 0)
                npcDiags[i].outAction = actionNodes[S_npcDiags[i].outActionIndex];
            else
                npcDiags[i].outAction = null;
        }

        for (int i = 0; i < actionNodes.Count; i++) //Connect Action Nodes
        {
            if (S_actionNodes[i].outPlayerIndex >= 0)
                actionNodes[i].outPlayer = playerDiags[S_actionNodes[i].outPlayerIndex];
            else
                actionNodes[i].outPlayer = null;

            if (S_actionNodes[i].outNPCIndex >= 0)
                actionNodes[i].outNPC = npcDiags[S_actionNodes[i].outNPCIndex];
            else
                actionNodes[i].outNPC = null;

            if (S_actionNodes[i].outActionIndex >= 0)
                actionNodes[i].outAction = actionNodes[S_actionNodes[i].outActionIndex];
            else
                actionNodes[i].outAction = null;
        } 

    }
}
