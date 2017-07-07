using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOver : MonoBehaviour, IPointerEnterHandler {

    public GameObject gameController;
    public GameScript gameScript;

	void Start () {
        gameScript = gameController.GetComponent<GameScript>();
	}

	void Update () {
		
	}

    public void OnPointerEnter(PointerEventData eventData) {
        gameScript.UpdateSelected(gameObject);
    }
}
