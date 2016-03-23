/// <summary>
/// A script that controls a Dot.
/// Author: Julie Chien 12/10/2014
/// </summary>
/// 
using UnityEngine;
using System.Collections;

public class DotScript : MonoBehaviour {

	public int colorID = 0; 
	private int updateCounter = 0;

	// Set the color of the dot
	public void setColor(int colorID) {
		SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer> ();
		renderer.color = GameControllerScript.colorIDs[colorID]; 
	}
	
	void Update () {
		// Add a push for the first 10 frames so dots fall faster when they spawn
	    if (updateCounter < 10) {
			gameObject.rigidbody2D.AddForce (new Vector2 (0, -100));
		}
		updateCounter++;
	}

}
