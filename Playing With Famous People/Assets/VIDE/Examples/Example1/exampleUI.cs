using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

public class exampleUI : MonoBehaviour
{
    //This script will handle everything related to dialogue interface
    //It will use a VIDE_Data component to load dialogues and retrieve node data

    //REMEMBER! to have 1 VIDE_Data component within the scene

    //These are just references to UI components and objects in the scene
    public Text npcText;
    public Text npcName;
    public Text playerText;
    public GameObject itemPopUp;
    public GameObject uiContainer;

    //We'll use these later
    bool dialoguePaused = false;
    bool animatingText = false;

    //Player/NPC Image component
    public Image NPCSprite;
    public Image PlayerSprite;

    //Demo variables
    public List<string> example_Items = new List<string>();
    public List<string> example_ItemInventory = new List<string>();

    //We'll be using this to store the current player dialogue options
    private List<Text> currentOptions = new List<Text>();

    //Here I'm assigning the variable a new component of its required type
    void Start()
    {
        VIDE_Data.OnActionNode += ActionHandler; //Subscribe to listen to triggered actions
        VIDE_Data.OnLoaded += OnLoadedAction; //Subscribe
        VIDE_Data.LoadDialogues(); //Load all dialogues to memory so that we dont spend time doing so later
    }

    //Just so we know when we finished loading all dialogues, then we unsubscribe
    void OnLoadedAction()
    {
        Debug.Log("Finished loading all dialogues");
        VIDE_Data.OnLoaded -= OnLoadedAction;
    }

    void OnDisable()
    {
        VIDE_Data.OnActionNode -= ActionHandler;
    }

    //This begins the conversation (Called by examplePlayer script)
    public void Begin(VIDE_Assign diagToLoad)
    {
        //Let's clean the NPC text variables
        npcText.text = "";
        npcName.text = "";

        //First step is to call BeginDialogue, passing the required VIDE_Assign component 
        //This will store the first Node data in VIDE_Data.nodeData
        //But before we do so, let's subscribe to certain events that will allow us to easily
        //Handle the node-changes
        VIDE_Data.OnActionNode += ActionHandler;
        VIDE_Data.OnNodeChange += NodeChangeAction;
        VIDE_Data.OnEnd += EndDialogue;

        SpecialStartNodeOverrides(diagToLoad); //This one checks for special cases when overrideStartNode could change right before starting a conversation

        VIDE_Data.BeginDialogue(diagToLoad); //Begins conversation, will call the first OnNodeChange
        uiContainer.SetActive(true);
    }

    //Demo on yet another way to modify the flow of the conversation
    void SpecialStartNodeOverrides(VIDE_Assign diagToLoad)
    {
        //Get the item from CrazyCap to trigger this one on Charlie
        if (diagToLoad.alias == "Charlie")
        {
            if (example_ItemInventory.Count > 0 && diagToLoad.overrideStartNode == -1)
                diagToLoad.overrideStartNode = 16;
        }
    }

    //Input related stuff (scroll through player choices and update highlight)
    void Update()
    {
        //Lets just store the Node Data variable for the sake of fewer words
        var data = VIDE_Data.nodeData;

        if (VIDE_Data.isLoaded) //Only if
        {
            //Scroll through Player dialogue options
            if (!data.pausedAction)
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    if (data.selectedOption < currentOptions.Count - 1)
                        data.selectedOption++;
                }
                if (Input.GetKeyDown(KeyCode.W))
                {
                    if (data.selectedOption > 0)
                        data.selectedOption--;
                }

                //Color the Player options. Blue for the selected one
                for (int i = 0; i < currentOptions.Count; i++)
                {
                    currentOptions[i].color = Color.black;
                    if (i == data.selectedOption) currentOptions[i].color = Color.blue;
                }
            }
        }
    }

    //examplePlayer.cs calls this one to move forward in the conversation
    public void CallNext()
    {
        //Let's not go forward if text is currently being animated, but let's speed it up.
        if (animatingText) { animatingText = false; return; }

        if (!dialoguePaused) //Only if
        {
            //We check for current extraData before moving forward to do special actions
            //ExtraDataLookUp returns true if an action requires to skip VIDE_Data.Next()
            //It will be true when we receive an item
            if (ExtraVariablesLookUp(VIDE_Data.nodeData, true)) return;

            VIDE_Data.Next(); //We call the next node and populate nodeData with new data
            return;
        }

        //This will just disable the item popup if it is enabled
        if (itemPopUp.activeSelf)
        {
            dialoguePaused = false;
            itemPopUp.SetActive(false);
        }
    }       

    //Another way to handle Action Nodes is to listen to the OnActionNode event, which sends the ID of the action node
    void ActionHandler(int action)
    {
        Debug.Log("ACTION TRIGGERED: " + action.ToString());
    }

    //We listen to OnNodeChange to update our UI with each new nodeData
    //This should happen right after calling VIDE_Data.Next()
    void NodeChangeAction(VIDE_Data.NodeData data)
    {
        //Reset some variables
        npcText.text = "";
        npcText.transform.parent.gameObject.SetActive(false);
        playerText.transform.parent.gameObject.SetActive(false);
        PlayerSprite.sprite = null;
        NPCSprite.sprite = null;

        //Look for dynamic text change in extraData
        ExtraVariablesLookUp(data, false);

        //If this new Node is a Player Node, set the player choices offered by the node
        if (data.currentIsPlayer)
        {
            //Set node sprite if there's any, otherwise try to use default sprite
            if (data.nodeSprite != null)
                PlayerSprite.sprite = data.nodeSprite;
            else if (VIDE_Data.assigned.defaultPlayerSprite != null)
                PlayerSprite.sprite = VIDE_Data.assigned.defaultPlayerSprite;

            SetOptions(data.playerComments);
            playerText.transform.parent.gameObject.SetActive(true);

        }
        else  //If it's an NPC Node, let's just update NPC's text and sprite
        {
            //Set node sprite if there's any, otherwise try to use default sprite
            if (data.nodeSprite != null)
            {
                //For NPC sprite, we'll first check if there's any "sprite" key
                //Such key is being used to apply the nodeSprite only when at a certain comment index
                //Check CrazyCap dialogue for reference
                if (data.extraVars.ContainsKey("sprite"))
                {
                    if (data.npcCommentIndex == (int) data.extraVars["sprite"])
                        NPCSprite.sprite = data.nodeSprite;
                    else
                        NPCSprite.sprite = VIDE_Data.assigned.defaultNPCSprite; //If not there yet, set default dialogue sprite
                }
                else
                {
                    NPCSprite.sprite = data.nodeSprite;
                }
            }
            else if (VIDE_Data.assigned.defaultNPCSprite != null)
                NPCSprite.sprite = VIDE_Data.assigned.defaultNPCSprite;

            StartCoroutine(AnimateText(data));

            //If it has a tag, show it, otherwise show the dialogueName
            if (data.tag.Length > 0)
                npcName.text = data.tag;
            else
                npcName.text = VIDE_Data.assigned.alias;

            npcText.transform.parent.gameObject.SetActive(true);
        }
    }

    //Check to see if there's extraData and if so, we do stuff
    bool ExtraVariablesLookUp(VIDE_Data.NodeData data, bool PreCall)
    {
        //Don't conduct extra variable actions if we are waiting on a paused action
        if (data.pausedAction) return false;

        if (!data.currentIsPlayer) //For player nodes
        {
            //Check for extra variables
            //This one finds a key named "item" which has the value of the item thats gonna be given
            //If there's an 'item' key, then we will assume there's also an 'itemLine' key and use it
            if (PreCall) //Checks that happen right before calling the next node
            {
                if (data.extraVars.ContainsKey("item") && !data.dirty)
                    if (data.npcCommentIndex == (int)data.extraVars["itemLine"])
                    {
                        if (data.extraVars.ContainsKey("item++")) //If we have this key, we use it to increment the value of 'item' by 'item++'
                        {
                            Dictionary<string, object> newVars = data.extraVars; //Clone the current extraVars content
                            int newItem = (int)newVars["item"]; //Retrieve the value we want to change
                            newItem += (int)data.extraVars["item++"]; //Change it as we desire
                            newVars["item"] = newItem; //Set it back   
                            VIDE_Data.UpdateExtraVariables(25, newVars); //Send newVars through UpdateExtraVariable method
                        }

                        //If it's CrazyCap, check his stock before continuing
                        //If out of stock, change override start node
                        if (VIDE_Data.assigned.alias == "CrazyCap")
                            if ((int)data.extraVars["item"]+1 >= example_Items.Count)
                                VIDE_Data.assigned.overrideStartNode = 28;


                        if (!example_ItemInventory.Contains(example_Items[(int)data.extraVars["item"]]))
                        {
                            GiveItem((int)data.extraVars["item"]);
                            return true;
                        }
                    }
            }

            if (data.extraVars.ContainsKey("nameLookUp"))
                nameLookUp(data);

        } else //for NPC nodes
        {
            //Nothing here yet ¯\_(ツ)_/¯
        }
        return false;
    }

    //Adds item to demo inventory, shows item popup, and pauses dialogue
    void GiveItem(int itemIndex)
    {
        example_ItemInventory.Add(example_Items[itemIndex]);
        itemPopUp.SetActive(true);
        string text = "You've got a <color=blue>" + example_Items[itemIndex] + "</color>!";
        itemPopUp.transform.GetChild(0).GetComponent<Text>().text = text;
        dialoguePaused = true;
    }

    //This uses the returned string[] from nodeData.playerComments to create the UIs for each comment
    //It first cleans, then it instantiates new options
    //This is for demo only, you shouldn´t instantiate/destroy so constantly
    public void SetOptions(string[] opts)
    {
        //Destroy the current options
        foreach (UnityEngine.UI.Text op in currentOptions)
            Destroy(op.gameObject);

        //Clean the variable
        currentOptions = new List<UnityEngine.UI.Text>();

        //Create the options
        for (int i = 0; i < opts.Length; i++)
        {
            GameObject newOp = Instantiate(playerText.gameObject, playerText.transform.position, Quaternion.identity) as GameObject;
            newOp.SetActive(true);
            newOp.transform.SetParent(playerText.transform.parent, true);
            newOp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20 - (20 * i));
            newOp.GetComponent<UnityEngine.UI.Text>().text = opts[i];
            currentOptions.Add(newOp.GetComponent<UnityEngine.UI.Text>());
        }
    }

    //This will replace any "[NAME]" with the name of the gameobject holding the VIDE_Assign
    //Will also replace [WEAPON] with a different variable
    void nameLookUp(VIDE_Data.NodeData data)
    {
        if (data.npcComment[data.npcCommentIndex].Contains("[NAME]"))
        data.npcComment[data.npcCommentIndex] = data.npcComment[data.npcCommentIndex].Replace("[NAME]", VIDE_Data.assigned.gameObject.name);

        if (data.npcComment[data.npcCommentIndex].Contains("[WEAPON]"))
        data.npcComment[data.npcCommentIndex] = data.npcComment[data.npcCommentIndex].Replace("[WEAPON]", example_ItemInventory[0]);
    }

    //Very simple text animation usin StringBuilder
    public IEnumerator AnimateText(VIDE_Data.NodeData data)
    {
        animatingText = true;
        string text = data.npcComment[data.npcCommentIndex];

        if (!data.currentIsPlayer)
        {
            StringBuilder builder = new StringBuilder();
            int charIndex = 0;
            while (npcText.text != text)
            {
                if (!animatingText) break; //CallNext() makes this possible to speed things up

                builder.Append(text[charIndex]);
                charIndex++;
                npcText.text = builder.ToString();
                yield return new WaitForSeconds(0.02f);
            }
        }

        npcText.text = data.npcComment[data.npcCommentIndex]; //Now just copy full text		
        animatingText = false;
    }

    //Unsuscribe from everything, disable UI, and end dialogue
    void EndDialogue(VIDE_Data.NodeData data)
    {
        VIDE_Data.OnActionNode -= ActionHandler;
        VIDE_Data.OnNodeChange -= NodeChangeAction;
        VIDE_Data.OnEnd -= EndDialogue;
        uiContainer.SetActive(false);
        VIDE_Data.EndDialogue();
    }

    //Example method called by an Action Node
    public void ActionGiveItem(int item)
    {
        //Do something here
    }
}
