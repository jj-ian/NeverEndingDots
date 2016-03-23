/// <summary>
/// Simple 2D Line Drawing Script.
/// Original Author: Eric Haines (Eric5h5)
/// Modified by Julie Chien 12/11/2014 to draw lines connecting Dots. Julie's modifications are noted.
/// Retrieved from the Unity wiki 12/11/2014, URL: http://wiki.unity3d.com/index.php?title=VectorLine
/// </summary>
/// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class VectorLine : MonoBehaviour
{
	public int numberOfPoints = 2;
	public Color lineColor = Color.red;
	public int lineWidth = 3;
	public bool drawLines = true;
	private Material lineMaterial;
	private Camera cam;

	// List of points on the line. 
	// Originally an array, changed to a list so vertices can be dynamically added at runtime - Julie
	private List<Vector2> linePoints = new List<Vector2> ();
	
	void Awake ()
	{
		lineMaterial = new Material ("Shader \"Lines/Colored Blended\" {" +
			"SubShader { Pass {" +
			"   BindChannels { Bind \"Color\",color }" +
			"   Blend SrcAlpha OneMinusSrcAlpha" +
			"   ZWrite Off Cull Off Fog { Mode Off }" +
			"} } }");
		lineMaterial.hideFlags = HideFlags.HideAndDontSave;
		lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		cam = camera;
	}
	
	void Update ()
	{
		// If the user's touching the screen and the line has at least 2 points in it, 
		// have the last endpoint of the line follow the user's touch - added by Julie
		if (linePoints.Count > 1 && Input.touchCount > 0) {
			Touch touch = Input.GetTouch (0);
			linePoints [linePoints.Count - 1] = Camera.main.ScreenToWorldPoint (touch.position);
		}
	}

	// Add a point to the line, along with an additional point for finger tracking - added by Julie
	public void AddPoint (Vector2 newPoint)
	{
		if (linePoints.Count > 0) {
			linePoints [linePoints.Count - 1] = newPoint;
			linePoints.Add (newPoint);
		} else {
			linePoints.Add (newPoint);
			linePoints.Add (newPoint);
		}
	}

	// Clears the line - added by Julie
	public void ClearLine ()
	{
		linePoints.Clear ();
	}
	
	void OnPostRender ()
	{
		if (!drawLines || linePoints == null || linePoints.Count < 2)
			return;
		
		float nearClip = cam.nearClipPlane + 0.00001f;
		int end = linePoints.Count - 1;
		float thisWidth = 1f / Screen.width * lineWidth * 0.5f;
		
		lineMaterial.SetPass (0);
		GL.Color (lineColor);
		
		if (lineWidth == 1) {
			GL.Begin (GL.LINES);
			for (int i = 0; i < end; ++i) {
				GL.Vertex (new Vector3 (linePoints [i].x, linePoints [i].y, nearClip));
				GL.Vertex (new Vector3 (linePoints [i + 1].x, linePoints [i + 1].y, nearClip));
			}
		} else {
			GL.Begin (GL.QUADS);
			for (int i = 0; i < end; ++i) {
				Vector3 perpendicular = (new Vector3 (linePoints [i + 1].y, linePoints [i].x, nearClip) -
					new Vector3 (linePoints [i].y, linePoints [i + 1].x, nearClip)).normalized * thisWidth;
				Vector3 v1 = new Vector3 (linePoints [i].x, linePoints [i].y, nearClip);
				Vector3 v2 = new Vector3 (linePoints [i + 1].x, linePoints [i + 1].y, nearClip);
				GL.Vertex (v1 - perpendicular);
				GL.Vertex (v1 + perpendicular);
				GL.Vertex (v2 + perpendicular);
				GL.Vertex (v2 - perpendicular);

			}
		}
		GL.End ();
	}
	
	void OnApplicationQuit ()
	{
		DestroyImmediate (lineMaterial);
	}
}