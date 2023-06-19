using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(SFB_AudioClipArrayCombiner))]
[CanEditMultipleObjects]
[Serializable]
public class SFB_AudioClipArrayCombinerEditor : Editor {

	public override void OnInspectorGUI()
	{
		Texture titleTexture = Resources.Load("titleAudioClipCombiner") as Texture;
		SFB_AudioClipArrayCombiner myScript = target as SFB_AudioClipArrayCombiner;
		float newWidth = ((Screen.width / 2) - 50);
		float newHeight = newWidth * 0.15f;
		GUILayout.Label(titleTexture, GUILayout.Width(newWidth), GUILayout.Height(newHeight));


		DrawDefaultInspector();	

		GUI.color = Color.green;
		if(GUILayout.Button("Export Clip Combinations"))									// If this button is clicked...
			myScript.SaveNow();															// Call the function
		GUI.color = Color.white;
	}
}
