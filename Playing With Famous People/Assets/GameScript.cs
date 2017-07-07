using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityStandardAssets.Characters.FirstPerson;

public class GameScript : MonoBehaviour{

    public Image textBox;
    public Text aiDialogue;
    public Text playerDialogue;
    public string[] aiLines;
    public string[] playerLines;
    public List<Text> currentOptions;
    public List<GameObject> currentOptionsAsGameObjects;
    public bool inConversation;
    public int step;
    public bool firstTime;
    public GameObject uiContainer;
    public GameObject player;
    public RigidbodyFirstPersonController rbfpc;

    // VIDE crap
    public VIDE_Assign videAss;

    void Start () {
        Cursor.visible = true;
        Begin(videAss);
        //VIDE_Data.BeginDialogue(videAss);
        //aiDialogue.text = "Welcome to the Troll Question game.  Select a dialogue option to continue.";
        //playerDialogue.text = string.Join("\n", VIDE_Data.nodeData.playerComments);
        //VIDE_Data.OnActionNode += ActionHandler;
        //VIDE_Data.OnNodeChange += NodeChangeAction;
        //VIDE_Data.OnEnd -= EndDialogue; 
        var data = VIDE_Data.nodeData;
        aiDialogue.text = VIDE_Data.nodeData.npcComment[data.npcCommentIndex];
    }

	void Update () {
        var data = VIDE_Data.nodeData;

        print(inConversation);

        if (inConversation) {
            rbfpc.enabled = false;
            Cursor.visible = true;
        } else {
            rbfpc.enabled = true;
            Cursor.visible = false;
        }

        if (VIDE_Data.isLoaded) {
            if (!data.pausedAction) {
                if (Input.GetKeyDown(KeyCode.Alpha1)) {
                    data.selectedOption = 0;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2)) {
                    data.selectedOption = 1;
                }
                if (Input.GetKeyDown(KeyCode.Alpha3)) {
                    data.selectedOption = 2;
                }
                if (Input.GetKeyDown(KeyCode.Alpha4)) {
                    data.selectedOption = 3;
                }
            }

            if (currentOptionsAsGameObjects.Count != 0) {
                foreach (GameObject option in currentOptionsAsGameObjects) {
                    //Color the Player options. Blue for the selected one
                    for (int i = 0; i < currentOptions.Count; i++) {
                        currentOptions[i].color = Color.white;
                        if (i == data.selectedOption) {
                            currentOptions[i].color = Color.yellow;
                        }
                    }
                }
            }
        }

        //print(data.npcComment[data.npcCommentIndex]);
        //text.text = data.npcComment[data.npcCommentIndex];
        //print(VIDE_Data.nodeData.selectedOption);

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            VIDE_Data.Next();
            //aiDialogue.text = string.Empty;
            //playerDialogue.text = string.Empty;
        }
        if (inConversation) {
            //text.text = CurrentText();
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus)) {
            step++;
        }
	}

    void NodeChangeAction(VIDE_Data.NodeData data) {

        foreach (Text text in currentOptions) {
            Destroy(text);
        }
        currentOptions = new List<Text>();
        currentOptionsAsGameObjects = new List<GameObject>();



        if (data.currentIsPlayer) {
            playerLines = new string[data.playerComments.Length];
            currentOptionsAsGameObjects = new List<GameObject>();

            for (int i = 0; i < data.playerComments.Length; i++) {
                GameObject newLine = Instantiate(playerDialogue.gameObject, playerDialogue.transform.position, Quaternion.identity);
                newLine.SetActive(true);
                newLine.transform.SetParent(playerDialogue.transform.parent, true);
                newLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30 - (30 * (i + 1)));
                newLine.GetComponent<Text>().text = data.playerComments[i];
                currentOptions.Add(newLine.GetComponent<Text>());
            }

            foreach (Text option in currentOptions) {
                //Vector3[] corners = new Vector3[4];
                //option.gameObject.GetComponent<RectTransform>().GetWorldCorners(corners);
                currentOptionsAsGameObjects.Add(option.gameObject);
            }
        }
        if (!data.currentIsPlayer) {
            aiDialogue.text = VIDE_Data.nodeData.npcComment[data.npcCommentIndex];
        }
    }

    public string CurrentText() {
        string output = string.Empty;
        return output;
    }

    public void UpdateSelected(GameObject button) {
        var data = VIDE_Data.nodeData;
        data.selectedOption = currentOptions.IndexOf(button.transform.parent.GetComponent<Text>());
    }

    void ActionHandler(int action) {
        if (action == 12) {
            BeginAssault();
        }
        if (action == 8) {
            Explore();
        }
    }

    public void BeginAssault() {
        print("begin assault");
        EndDialogue(VIDE_Data.nodeData);
    }

    public void Explore() {
        print("explore");
        EndDialogue(VIDE_Data.nodeData);
    }

    void EndDialogue(VIDE_Data.NodeData data) {
        inConversation = false;
        VIDE_Data.OnActionNode -= ActionHandler;
        VIDE_Data.OnNodeChange -= NodeChangeAction;
        VIDE_Data.OnEnd -= EndDialogue;
        uiContainer.SetActive(false);
        VIDE_Data.EndDialogue();
    }

    public void Begin(VIDE_Assign diagToLoad) {
        inConversation = true;
        aiDialogue.text = "";
        playerDialogue.text = "";

        VIDE_Data.OnActionNode += ActionHandler;
        VIDE_Data.OnNodeChange += NodeChangeAction;
        VIDE_Data.OnEnd += EndDialogue;

        //SpecialStartNodeOverrides(diagToLoad); //This one checks for special cases when overrideStartNode could change right before starting a conversation

        VIDE_Data.BeginDialogue(diagToLoad); //Begins conversation, will call the first OnNodeChange
        uiContainer.SetActive(true);
    }

    public void OnTriggerEnter(Collider other) {
        if (other.tag == "Player") {
            Begin(videAss);
        }
    }
}
