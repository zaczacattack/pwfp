using UnityEngine;
using System.Collections;

public class minUIExample : MonoBehaviour {

    void Start()
    {
        gameObject.AddComponent<VIDE_Data>();
    }

    void OnGUI () {
	    if (VIDE_Data.isLoaded)
        {
            var data = VIDE_Data.nodeData; //Quick reference
            if (data.currentIsPlayer) // If it's a player node, let's show all of the available options as buttons
            {
                for (int i = 0; i < data.playerComments.Length; i++)
                {
                    if (GUILayout.Button(data.playerComments[i])) //When pressed, set the selected option and call Next()
                    {
                        data.selectedOption = i;
                        VIDE_Data.Next();
                    }
                }
            } else //if it's a NPC node, Let's show the comment and add a button to continue
            {
                GUILayout.Label(data.npcComment[data.npcCommentIndex]);

                if (GUILayout.Button(">")){
                    VIDE_Data.Next();
                }
            }
			if (data.isEnd) // If it's the end, let's just call EndDialogue
                {
                    VIDE_Data.EndDialogue();
                }
        } else // Add a button to begin conversation if it isn't started yet
        {
            if (GUILayout.Button("Start Convo"))
            {
                VIDE_Data.BeginDialogue(GetComponent<VIDE_Assign>()); //We've attached a DialogueAssign to this same gameobject, so we just call the component
            }
        }
	}
}
