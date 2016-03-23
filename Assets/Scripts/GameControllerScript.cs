/// <summary>
/// A script that controls gameplay.
/// Author: Julie Chien 12/10/2014
/// </summary>
/// 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameControllerScript : MonoBehaviour
{
	// References to the Dot Script and the line-drawing script
	public DotScript dotScript;
	public VectorLine vectorLineScript;

	// Reference to the Dot prefab
	public Transform dotPrefab;

	// List of like-colored dots the user has collected with a single swipe
	private List<GameObject> dotsTouched = new List<GameObject> ();

	// List of Dots that are to be destroyed
	private List<GameObject> dotsToDestroy = new List<GameObject> ();

	// The target size and speed for shrinking a dot that's being destroyed
	public float shrinkTargetScale = 0.01f;
	public float shrinkSpeed = 10.0f;

	// Width and height of the game grid
	public int gridWidth = 6;
	public int gridHeight = 6;

	// Number of dots colors
	public int numberOfColors = 5;

	// Array of colors for the dots
	public static  Color[] colorIDs = {
		new Color(170f/255f,255f/255f,0/255f),
		new Color(255f/255f,170f/255f,0f/255f),
		new Color(255f/255f,0f/255f,170f/255f),
		new Color(170f/255f,0f/255f,255f/255f),
		new Color(0f/255f,170f/255f,255f/255f),
	};

	// On start, spawn the board
	void Start ()
	{   
		StartCoroutine (Spawn ());
	}

	void Update ()
	{
		// Shrink the dots in list of dots to destroy
		foreach (GameObject dotToDestroy in dotsToDestroy) {
			ShrinkDot(dotToDestroy);
		}
		
		// Touch handling
		if (Input.touchCount > 0) {
			Touch touch = Input.GetTouch (0);
			
			// If user's finger is touching the screen, figure out which dots the user's picking up
			if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved) {
				RaycastHit2D hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint ((Input.GetTouch (0).position)), Vector2.zero);
				
				// Touched a dot
				if (hit.collider != null && hit.collider.gameObject.name == "Dot(Clone)") {
					GameObject dotTouched = hit.collider.gameObject;
					DotScript dotTouchedScript = dotTouched.GetComponent ("DotScript") as DotScript;
					
					// Determine if we should add the newly touched dot to the list of connected dots.
					// If the new dot passed all the tests, add dot to list of connected dots
					if (ShouldAddDotToList(dotTouched, dotTouchedScript)) {
						dotsTouched.Add (dotTouched);
						
						// If this is the first dot in the list, set the line color of the connecting line to be the color of the dot
						if (dotsTouched.Count == 1) {
							vectorLineScript.lineColor = colorIDs[dotTouchedScript.colorID];
						}
						
						// Connect this dot to the line
						vectorLineScript.AddPoint(dotTouched.transform.position);
					}
				}
				
				// When user releases the touch, it's time to destroy some dots
			} else if (touch.phase == TouchPhase.Ended) {
				if (dotsTouched.Count > 1) {
					
					DotScript lastDotInListScript = dotsTouched [dotsTouched.Count - 1].GetComponent ("DotScript") as DotScript;
					
					// Keeps track of which columns need replenishing after destroying dots.
					// The index is the column number, and the value is the number of dots to
					// replenish in that column
					int[] dotsToReplenish = new int[gridWidth];
					
					// If dots form a square, destroy all dots on board of matching color
					if (FormsSquare (dotsTouched)) {
						dotsToReplenish = ShrinkDotsWithColorID (lastDotInListScript.colorID);
						
						// Replenish the board with dots, excluding the color that was just square'd away
						StartCoroutine (ReplenishExcludingColorID (lastDotInListScript.colorID, dotsToReplenish));
						
						// Otherwise, destroy the string of connected dots
					} else {
						foreach (GameObject dotTouched in dotsTouched) {
							// Figure out coordinates of dot destroyed and note it in the replenish list 
							Point point = ScreenCoordsToPoint (dotTouched.transform.position);
							dotsToReplenish [point.x] += 1;
							
							// Prepare the dot for destruction
							dotsToDestroy.Add (dotTouched);
						}
						StartCoroutine (ReplenishExcludingColorID (-1, dotsToReplenish));
					}
					StartCoroutine(DestroyDotsAfterDelay(0.5f));
				} 
				
				// Clear the list of dots touched and clear the line
				if (dotsTouched.Count > 0) {
					dotsTouched.Clear ();
					vectorLineScript.ClearLine();
				}
			}
		} 
	}

	// Spawns the initial set of dots that fall down when the game is started
	IEnumerator Spawn ()
	{
		Vector3 topLeft = Camera.main.ViewportToWorldPoint (new Vector3 (0, 1, 0));

		//Spawn 1 row at a time with a delay between each row. 
		for (int row = 0; row < gridHeight; row++) {
			for (int col = 0; col < gridWidth; col++) {
				int colorID = Random.Range (0, numberOfColors);
				dotScript.setColor (colorID);
				dotScript.colorID = colorID;
				GameObject dot = Instantiate (dotPrefab, new Vector2 (0.75f * (col - 2.5f), topLeft.y + row + 2), transform.rotation) as GameObject;
			}
			yield return new WaitForSeconds (0.01f);
		}
	}

	// Determine if we should add the newly touched dot to the list of connected dots
	bool ShouldAddDotToList(GameObject dotTouched, DotScript dotTouchedScript) 
	{
		bool addDotToList = false;
		
		// Always add the new dot if the dot list is empty
		if (dotsTouched.Count == 0) {
			addDotToList = true;
			
			// Otherwise, let's test the new dot to see if it's a viable candidate for the list
		} else {
			
			// If there are already dots in the list, first check that the new dot matches in color
			bool colorMatches = false;
			
			DotScript lastDotInListScript = dotsTouched [dotsTouched.Count - 1].GetComponent ("DotScript") as DotScript;
			if (lastDotInListScript.colorID == dotTouchedScript.colorID) {
				colorMatches = true;
			}
			
			// If color matches, check if the new dot is adjacent to the last dot in the list
			if (colorMatches) {
				
				bool adjacent = false;
				Point lastDotTouchedPoint = ScreenCoordsToPoint (dotsTouched [dotsTouched.Count - 1].transform.position);
				Point dotTouchedPoint = ScreenCoordsToPoint (dotTouched.transform.position);
				
				if (isAdjacent (lastDotTouchedPoint, dotTouchedPoint)) {
					adjacent = true;
				}
				
				if (adjacent == true) {
					
					// If the new dot is adjacent, matches in color, and not already in the list, it's ready to be added to the list
					if (!dotsTouched.Contains (dotTouched)) {
						addDotToList = true;
						
						// If it's adjacent, matches in color, and is already in the list, check to see if it closes the loop on a square
					} else {
						List<GameObject> dotsTouchedCopy = new List<GameObject>(dotsTouched);
						dotsTouchedCopy.Add (dotTouched);
						
						if (FormsSquare(dotsTouchedCopy))
							addDotToList = true;
					}
				}
			}
		}
		return addDotToList;
	}

	// Check if the connected dots form a square
	private bool FormsSquare (List<GameObject> dotsList)
	{
		if (dotsList.Count < 5) {
			return false;
		}
		
		// Find the first duplicate dot (the point that could be the loop close on a square)
		int startIndex = -1;
		
		for (int i = 0; i < dotsList.Count - 4; i++) {
			if (dotsList [i] == dotsList [i + 4]) {
				startIndex = i;
				break;
			}
		}
		
		// If there's no duplicate dot, no square is possible - return false
		if (startIndex == -1) {
			return false;
		}
		
		// Otherwise, check if the points form a parallelogram with 1 right angle. if so, the points form a square
		
		Point p1 = ScreenCoordsToPoint (dotsList [startIndex].transform.position);
		Point p2 = ScreenCoordsToPoint (dotsList [startIndex + 1].transform.position);
		Point p3 = ScreenCoordsToPoint (dotsList [startIndex + 2].transform.position);
		Point p4 = ScreenCoordsToPoint (dotsList [startIndex + 3].transform.position);
		
		if ((p3 == p1 + (p2 - p1) + (p4 - p1)) && ((p2 - p1) * (p4 - p1) == 0)) {
			return true;
		} 
		
		return false;
		
	}
	
	// Check if pointInQuestion is adjacent to centerPoint, used to determine if dots are adjacent to one another
	// and eligible for pickup
	private bool isAdjacent (Point centerPoint, Point pointInQuestion)
	{
		if (pointInQuestion.x == centerPoint.x) {
			if (pointInQuestion.y == centerPoint.y + 1 || pointInQuestion.y == centerPoint.y - 1)
				return true;
		} else if (pointInQuestion.y == centerPoint.y) {
			if (pointInQuestion.x == centerPoint.x + 1 || pointInQuestion.x == centerPoint.x - 1)
				return true;
		}
		return false;
	}

	// Destroys the dots-to-be-destroyed after a delay. 
	// Allows for shrinking animation to complete before dots are destroyed.
	private IEnumerator DestroyDotsAfterDelay(float delay)
	{
		yield return new WaitForSeconds (delay);
		foreach (GameObject dotToDestroy in dotsToDestroy) {
			Destroy(dotToDestroy);
		}
		dotsToDestroy.Clear ();
	}

	// Animate the dot shrinking
	void ShrinkDot(GameObject dot) 
	{
		dot.transform.localScale = Vector3.Lerp (dot.transform.localScale, new Vector3 (shrinkTargetScale, shrinkTargetScale, shrinkTargetScale), Time.deltaTime * shrinkSpeed);
	}

	// Shrink all dots of specified color, add them to the destroy list, and returns array of info for replenishing dots
	private int[] ShrinkDotsWithColorID (int colorID)
	{
		int[] dotsToReplenish = new int[gridWidth];

		foreach (GameObject dot in GameObject.FindGameObjectsWithTag("Dot")) {
			DotScript dotScript = dot.GetComponent ("DotScript") as DotScript;

			if (dotScript.colorID == colorID) {
				Point point = ScreenCoordsToPoint (dot.transform.position);
				if (point.x >= 0 && point.x < gridWidth) {
				dotsToReplenish [point.x] += 1;
					dotsToDestroy.Add(dot);
				}
			}
		}
		return dotsToReplenish;
	}

	// Replenish dots according to values passed in dotsReplenishArray, excluding the specified colored ID
	private IEnumerator ReplenishExcludingColorID (int colorIDToExclude, int[] dotsReplenishArray)
	{
		Vector3 topLeft = Camera.main.ViewportToWorldPoint (new Vector3 (0, 1, 0));

		int maxDotsInColumn = dotsReplenishArray.Max ();

		// Generate new dots row by row
		for (int row = maxDotsInColumn; row > 0; row--) {
			for (int col = 0; col < dotsReplenishArray.Length; col++) {
				if (dotsReplenishArray [col] >= row) {

					// Keep generating random colors until you get one that's not the color to exclude
					int colorID;
					while ((colorID = Random.Range (0, numberOfColors)) == colorIDToExclude);

					dotScript.setColor (colorID);
					dotScript.colorID = colorID;
					GameObject dot = Instantiate (dotPrefab, new Vector2 (0.75f * (col - 2.5f), topLeft.y + (maxDotsInColumn - row) + 1), transform.rotation) as GameObject;
				}
			}
			yield return new WaitForSeconds (0.01f);
		}
	}
	
	// Convert a dot's position on the screen to simple integer grid coordinates
	private Point ScreenCoordsToPoint (Vector2 screenCoords)
	{
		float workingX;
		float workingY;

		workingX = screenCoords.x + 1.9f;
		workingY = screenCoords.y + 1.7f;
		workingX = workingX / 0.75f;
		workingY = workingY / 0.7f;

		int roundedX = (int)Mathf.Round (workingX);
		int roundedY = (int)Mathf.Round (workingY);

		return new Point (roundedX, roundedY);
	}
}

// An integer point
public struct Point
{
	public int x;
	public int y;
	
	public Point (int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	
	public static Point operator +(Point p1, Point p2) 
	{
		return new Point(p1.x + p2.x, p1.y + p2.y);
	}
	
	public static Point operator -(Point p1, Point p2) 
	{
		return new Point(p1.x - p2.x, p1.y - p2.y);
	}
	
	//dot product
	public static int operator *(Point p1, Point p2) 
	{
		return p1.x * p2.x + p1.y * p2.y;
	}

	public static bool operator ==(Point p1, Point p2) 
	{
		if (p1.x == p2.x && p1.y == p2.y) {
			return true;
		}
		return false;
	}
	
	public static bool operator !=(Point p1, Point p2) 
	{
		return !(p1 == p2);
	}
	
	public override string ToString ()
	{
		return (string.Format ("(x:{0}, y:{1})", x, y));
	}
}


