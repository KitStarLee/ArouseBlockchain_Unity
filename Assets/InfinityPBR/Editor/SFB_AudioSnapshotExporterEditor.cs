using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(SFB_AudioSnapshotExporter))]
[CanEditMultipleObjects]
[Serializable]
public class SFB_AudioSnapshotExporterEditor : Editor {

	public override void OnInspectorGUI()
	{
		Texture myTexture = Resources.Load("infinitypbrExporter") as Texture;
		Texture titleTexture =  Resources.Load("titleAudioSnapshotExporter") as Texture;
		SFB_AudioSnapshotExporter myScript = target as SFB_AudioSnapshotExporter;

		float newWidth = (Screen.width / 2) - 50.0f;
		float newHeight = newWidth * 0.15f;
		GUILayout.Label(titleTexture, GUILayout.Width(newWidth), GUILayout.Height(newHeight));

		if (!myScript.audioMixer)																	// If we haven't chosen an AudioMixer yet...
			EditorGUILayout.HelpBox("Please choose an Audio Mixer below.", MessageType.Warning, false);	// Display a dialogue

		// Display an object field that accepts AudioMixer objects
		myScript.audioMixer = EditorGUILayout.ObjectField("Audio Mixer", myScript.audioMixer, typeof(UnityEngine.Audio.AudioMixer), false) as UnityEngine.Audio.AudioMixer;

		if (myScript.outputName == "" || myScript.outputName == null)								// If outputName is null or empty...
			myScript.outputName = "Custom Export";													// Make it "Custom Export" by default

		if (myScript.audioMixer)																	// If we have chosen an AudioMixer
		{
			if (myScript.audioMixerName != myScript.audioMixer.name)								// If the name of the AudioMixer is different from audioMixerName...
			{
				myScript.audioMixerName	= myScript.audioMixer.name;									// Make audioMixerName be the name of the selected AudioMixer
				myScript.ReloadData();																// And reload the data for the new mixer
			}

			if (myScript.audioClips.Count < 2)
			{
				// Display an error box warning user that they need to populate the clips as children of the current object.
				EditorGUILayout.HelpBox("Warning:  For each Clip in your track...\n\n1. Create a child object under this object.\n2. Name the child the same name as the exposed Volume parameter in the Audio Mixer.\n3. Add an Audio Source component to the child object.\n4. Assign your AudioClip to the \"Audio Clip\" parameter.\n5. Assign the AudioGroup for that clip in the \"Output\" parameter.\n6. Click the Reload Data button below\n\n** You can automate much of this process.  Check the instructions in the comments before the \"SetupChildren\" function in \"SFB_AudioSnapshotExporter.cs\".", MessageType.Error);
				if(GUILayout.Button("Reload AudioMixer Data"))										// If this button is clicked...
					myScript.ReloadData();															// Call the function
			}
			else
			{
				EditorGUILayout.BeginHorizontal (GUILayout.Width(newWidth));
				// Display a dialogue box describing what the Reload Data button does and when to use it.
				EditorGUILayout.HelpBox("Data will be loaded when you select or change the Audio Mixer.  If you change Mixer data, press button to reload.", MessageType.Info);
				if(GUILayout.Button("Reload AudioMixer Data"))											// If this button is clicked...
					myScript.ReloadData();																// Call the function
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.Space();

				// Only display this section when the editor is in Play Mode.
				if (Application.isPlaying)																// If we are in play mode
				{
					// Display a dialogue box describing what the "Export All Snapshots" button does and how to use it.
					//EditorGUILayout.HelpBox("BATCH EXPORTING SNAPSHOTS\n1. Only available in play mode.\n2. MORE INFO GOES HERE\n3. Click \"Export All Snapshots\".", MessageType.Info);
					GUI.color = Color.green;
					if(GUILayout.Button("Export All Snapshots"))										// If this button is clicked...
						myScript.StartBatch();															// Call the function
					GUI.color = Color.white;
				}
				else
					EditorGUILayout.HelpBox("BATCH EXPORTING SNAPSHOTS\n\nEnter Play Mode to batch export all Snapshots at once.", MessageType.Warning);

				EditorGUILayout.Space();

				// Display a dialogue box describing what the "Export Selected Snapshot" button does and how to use it.
				EditorGUILayout.HelpBox("EXPORTING A SINGLE SNAPSHOT\n\n1. Select the snapshot you'd like to export in the Audio Mixer window.\n2. Choose a custom name.  This will be appended to the Audio Mixer Name for the final file, and will overwrite any current file.\n3. Click \"Export Selected Snapshot\".\n\nCurrent export name:  " + myScript.audioMixer.name + "_" + myScript.outputName + ".wav", MessageType.Info);

				myScript.outputName = EditorGUILayout.TextField("Custom Name: ", myScript.outputName);	// Custom Name field
				if(GUILayout.Button("Export Selected Snapshot"))										// If this button is clicked...
					myScript.ExportSnapshots(false);													// Call the function
			}


		}

		EditorGUILayout.Space();

		GUI.skin = Resources.Load("InfinityPBRSkin") as GUISkin;
		GUILayout.Box("InfinityPBR produces some of the highest quality AAA models on the Asset Store, featuring massive customization and bonus features like Music, Sound Effects, Mesh Morphing & Concept Art.  Please check us out.", GUILayout.Width(newWidth));

		if(GUILayout.Button("Visit InfinityPBR.com", GUILayout.Width(newWidth)))										// If this button is clicked...
			Application.OpenURL("https://www.InfinityPBR.com/");

		float newHeight2 = Screen.width * 0.625f;


		GUILayout.Label(myTexture, GUILayout.Width(newWidth), GUILayout.Height(newHeight2));

		EditorGUILayout.Space();

		GUI.skin = null;

		// In general users will likely not care about the details.  For those intersted in whats going on under
		// the hood, we allow them to toggle the defualt inspector data on or off.
		myScript.displayInspector = EditorGUILayout.Toggle("Show Script Data", myScript.displayInspector);
		if (myScript.displayInspector)
			DrawDefaultInspector();																// Draws the default Inspector look from the target script
	}
}
