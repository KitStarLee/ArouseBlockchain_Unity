using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Originally Posted by ArtOfSettling on the Unity Forums:  https://forum.unity3d.com/threads/animation-event-copier.140158/
/// 
/// Modified by S.F. Bay Studios (http://www.InfinityPBR.com) 
/// </summary>

public class AnimationEventCopier : EditorWindow
{
	private AnimationClip sourceObject;									// Source Clip
	private AnimationClip targetObject;									// Target Clip
	private GameObject sourceGameObject;								// Source Game Object
	private GameObject targetGameObject;								// Target Game Object
	private GameObject eraseObject;										// Target to erase Events

	private Vector2 scrollPos;

	[MenuItem("Window/Infinity PBR/Animation Event Manager")]			// Can load this via the window menu
     	static void Init()
     	{
     		GetWindow(typeof(AnimationEventCopier));						// Brings this window to the front
     	}
	void OnGUI()
	{
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
		// Show a title box and object fields
		EditorGUILayout.HelpBox ("SINGLE CLIP COPY\n\nChoose a source clip and a target clip.  Names do not need to match.  The " +
			"Animation Events and Clip Settings will be copied from the source to the target.", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		sourceObject = EditorGUILayout.ObjectField("Source", sourceObject, typeof(AnimationClip), true) as AnimationClip;
		targetObject = EditorGUILayout.ObjectField("Target", targetObject, typeof(AnimationClip), true) as AnimationClip;
		EditorGUILayout.EndHorizontal();

		// If both objects are present, show the copy button
		if (sourceObject != null && targetObject != null)
		{
			if (GUILayout.Button ("Copy Single Clip")) {
				Undo.RecordObject(targetObject, "Undo Clip Event Copy");
				CopyData (sourceObject, targetObject);
			}
			EditorGUILayout.HelpBox ("Source Event Count: " + AnimationUtility.GetAnimationEvents (sourceObject).Length + "\n\n" +
				"Target Event Count: " + AnimationUtility.GetAnimationEvents (targetObject).Length, MessageType.Info);
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		// Show a title box and object fields
		EditorGUILayout.HelpBox ("ALL CLIP COPY\n\nChoose a source GameObject and a target GameObject.  For all clips where the names" +
			" match, the Events & Clip Settings will be copied.\n\nNOTE:  \"Name\" of the clip is taken from the Animator Controller component, " +
			"as is the Animation Clip.  Because of this, each GameObject should have a *different* Animator Component attached.", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		sourceGameObject = EditorGUILayout.ObjectField("Source", sourceGameObject, typeof(GameObject), true) as GameObject;
		targetGameObject = EditorGUILayout.ObjectField("Target", targetGameObject, typeof(GameObject), true) as GameObject;
		EditorGUILayout.EndHorizontal();

		// If both objects are present, show the copy button
		if (sourceGameObject != null && sourceGameObject != null)
		{
			if (GUILayout.Button ("Copy All Clips")) {
				Undo.RecordObject(targetGameObject, "Undo Clip Event Copy");
				CopyAllData (sourceGameObject, targetGameObject);
			}
			EditorGUILayout.HelpBox ("Source Clip Count: " + AnimationUtility.GetAnimationClips (sourceGameObject).Length + "\n\n" +
				"Target Clip Count: " + AnimationUtility.GetAnimationClips (targetGameObject).Length, MessageType.Info);

			/* Uncoment this section if you'd like to see a list of each Clip with the number of events on it.  Good for debugging
			for (int i = 0; i < AnimationUtility.GetAnimationClips (sourceGameObject).Length; i++) {
				EditorGUILayout.LabelField ("Source Clip " + AnimationUtility.GetAnimationClips (sourceGameObject) [i].name, AnimationUtility.GetAnimationEvents (AnimationUtility.GetAnimationClips (sourceGameObject) [i]).Length + " Events");
			}
			for (int i = 0; i < AnimationUtility.GetAnimationClips (targetGameObject).Length; i++) {
				EditorGUILayout.LabelField ("Target Clip " + AnimationUtility.GetAnimationClips (targetGameObject) [i].name, AnimationUtility.GetAnimationEvents (AnimationUtility.GetAnimationClips (targetGameObject) [i]).Length + " Events");
			}	
			*/
		}

		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();

		// Show a title box and object fields
		EditorGUILayout.HelpBox ("UTILITIES\n\nThese buttons may be of use.  Backup your project first, if you're not sure!", MessageType.Info);
		if (GUILayout.Button ("Copy All SOURCE Names to TARGET")) {
			if (EditorUtility.DisplayDialog ("Copy Source Names", "Are you sure you want to copy all SOURCE names to TARGET?", "Confirm", "Cancel")) {
				CopyClipNames (sourceGameObject, targetGameObject);
			}
		}
		EditorGUILayout.Space ();
		if (GUILayout.Button ("Copy All TARGET Names to SOURCE")) {
			if (EditorUtility.DisplayDialog ("Copy Target Names", "Are you sure you want to copy all TARGET names to SOURCE?", "Confirm", "Cancel")) {
				CopyClipNames (targetGameObject, sourceGameObject);
			}
		}
		EditorGUILayout.Space ();
		EditorGUILayout.BeginHorizontal();
		eraseObject = EditorGUILayout.ObjectField("Object to Erase", eraseObject, typeof(GameObject), true) as GameObject;
		if (eraseObject) {
			if (GUILayout.Button ("Erase All Events")) {
				if (EditorUtility.DisplayDialog ("Erase Events", "Are you sure you want to Erase All Events?", "Confirm", "Cancel")) {
					EraseEvents (eraseObject);
				}
			}
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndScrollView ();
	}

	void EraseEvents(GameObject targetObject){
		AnimationClip[] targetClips = AnimationUtility.GetAnimationClips (targetObject);
		for (int i = 0; i < targetClips.Length; i++) {
			EraseEvent (targetClips [i]);
		}
	}

	void EraseEvent(AnimationClip targetClip){
		AnimationUtility.SetAnimationEvents(targetClip, null);
	}

	/// <summary>
	/// This will copy the names from one to the other.  It's somewhat dangerous, as it just goes by the number of clips.  There's no way to
	/// confirm that the names are SUPPOSED to be overwritten.  The intent is that one Animator Component is a copy of the other, each with
	/// one set of clips (the old and the new).  However, the clip names may have changed in the process, and this is faster than doing it
	/// by hand.
	/// </summary>
	/// <param name="copyFrom">Copy from.</param>
	/// <param name="copyTo">Copy to.</param>
	void CopyClipNames(GameObject copyFrom, GameObject copyTo){
		AnimationClip[] fromClips = AnimationUtility.GetAnimationClips (copyFrom);								// Get all source clips
		AnimationClip[] toClips = AnimationUtility.GetAnimationClips (copyTo);									// Get all target Clips
		for (int i = 0; i < fromClips.Length; i++) {
			if (toClips.Length >= i - 1) {
				string oldName = toClips [i].name;
				toClips [i].name = fromClips[i].name;
				Debug.Log(oldName + " is now " + toClips [i].name);
			}
		}
	}

	/// <summary>
	/// Copies the events from sourceAnimClip to targetAnimClip
	/// </summary>
	/// <param name="sourceAnimClip">Source animation clip.</param>
	/// <param name="targetAnimClip">Target animation clip.</param>
	void CopyData(AnimationClip sourceClip, AnimationClip targetClip)
	{
		EraseEvent (targetClip);																							// Remove all events
		AnimationUtility.SetAnimationEvents(targetClip, AnimationUtility.GetAnimationEvents (sourceClip));					// Copy Events
		AnimationUtility.SetAnimationClipSettings(targetClip, AnimationUtility.GetAnimationClipSettings(sourceClip));		// Copy Clip Settings
		Debug.Log ("Copying " + AnimationUtility.GetAnimationEvents (sourceClip).Length + " event from " + sourceClip.name + " to " + targetClip.name + " (" + AnimationUtility.GetAnimationEvents (targetClip).Length + " events)");
	}

	/// <summary>
	/// Searches the source matched clip names in target, and will copy the data over.
	/// </summary>
	/// <param name="source">Source.</param>
	/// <param name="target">Target.</param>
	void CopyAllData(GameObject source, GameObject target){
		AnimationClip[] animationClipsSource = AnimationUtility.GetAnimationClips (source);									// Get all source clips
		AnimationClip[] animationClipsTarget = AnimationUtility.GetAnimationClips (target);									// Get all target Clips
		Debug.Log ("Copying data from " + animationClipsSource.Length + " clips in " + source.name + " to " + animationClipsTarget.Length + " clips in " + target.name);
		for (int i = 0; i < animationClipsSource.Length; i++) {																// For each source Clip...
			bool foundMatch = false;																						// Check to make sure we found a match
			for (int n = 0; n < animationClipsTarget.Length; n++) {															// ... & For each target clip...
				if (animationClipsSource [i].name == animationClipsTarget [n].name) {										// ...If the names match
					CopyData (animationClipsSource [i], animationClipsTarget [n]);											// Copy data
					foundMatch = true;																						// Toggle true
				}
			}
			if (!foundMatch) {
				// Give a warning to the user, in case they have a spelling error or something.
				Debug.LogWarning ("Warning: There was no matching clip called " + animationClipsSource [i].name + " found.  " +
				"(Target name for this ID was " + animationClipsTarget [i].name + ")");
			}
		}
	}
}