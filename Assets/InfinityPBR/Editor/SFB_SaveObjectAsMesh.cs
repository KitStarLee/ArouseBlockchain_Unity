using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

/* SCRIPT INFO
This script is intended to be used on objects that have a SkinnedMeshRenderer attached, or objects
that have children who have SkinnedMeshRenderers attached.  It will save a new mesh version of those
objects, with a normal MeshRenderer.  If used on an environment piece that has Blend Shapes, the 
Blend Shapes will be saved (although no longer editable).  If used on a character (often the children
of the main object will be separate meshes), each individual mesh will be saved as a new mesh in the
current form it is in.  That means you can save a pose from an animation as well as adjustments made
to the Blend Shape values.

The script will create an organized file list, and also replace the in-scene versions with the new
"Statue" versions.  This way, an entire scene filled with environment pieces that have
SkinnedMeshRenderers attached can be saved as static meshes and replaced in a single click.

For characters, it's best to make a copy of your character before you run the script, as the character
will be replaced.

Names that have duplicates in the file structure will have a number added to the end of them, so be
aware of the potential for duplicate meshes, although you won't have to worry about overwriting
already exported meshes.

This script is intended for editor use only.  It may work during runtime (perhaps with some
modification) but will likely not be very fast.

Please visit http://www.InfinityPBR.com to check out everything we produce.
*/

public class SFB_SaveObjectAsMesh : MonoBehaviour {

	[MenuItem("Window/Infinity PBR/Save As Mesh(es)")]
	static void ExportMeshes () {
		// Make sure we have all the needed folders
		SFB_CreateFolder("Assets", "InfinityPBR");
		SFB_CreateFolder("Assets/InfinityPBR", "Exported Meshes");

		// Run through each object that is currently selected
		for (int i = 0; i < Selection.objects.Length; i++){
			float currentPercent = (Mathf.Round(i) + 1.0f) / (Mathf.Round(Selection.objects.Length));						// Percent Complete
			EditorUtility.DisplayProgressBar("Creating Meshes", "(" + (i + 1) + " of " + Selection.objects.Length + ") Working on " + Selection.objects[i].name, currentPercent);
			GameObject thisObject = Selection.objects [i] as GameObject;												// Variable for this object
			string parentFolder = "Assets/InfinityPBR/Exported Meshes";							// Path to the folder that holds all of our exports
			string folderName = thisObject.name;													// Get a new folder name for this object
			int fileNumber = SFB_GetFileNumber("/InfinityPBR/Exported Meshes/" + folderName, thisObject.name);		// Get a sequential number if needed
			string path = parentFolder + "/" + folderName;											// Full path
			SFB_CreateFolder(parentFolder, folderName);												// Create the folder
			Component[] childrenSMR	= thisObject.GetComponentsInChildren<SkinnedMeshRenderer>();		// Find all SkinnedMeshRenderer components
			foreach (SkinnedMeshRenderer childSMR in childrenSMR)																// For all we found
			{
				GameObject obj = childSMR.gameObject;														// Assign the current object
				Renderer thisRenderer = obj.GetComponent<Renderer>();								// Get the renderer component
				Material thisMaterial = thisRenderer.sharedMaterial;								// Save the material we're using
				Mesh mesh = SFB_BakeMesh(path, obj, fileNumber);									// Save the mesh
				DestroyImmediate(thisRenderer);														// Remove the SkinnedMeshRenderer
				MeshFilter newMeshFilter = obj.AddComponent<MeshFilter>() as MeshFilter;			// Add a normal mesh filter
				newMeshFilter.mesh = mesh;															// Assign the mesh to the mesh filter
				mesh.RecalculateBounds();															// Recalculate the bounds of the mesh
				MeshRenderer newRenderer = obj.AddComponent<MeshRenderer>() as MeshRenderer;		// Add a normal mesh renderer
				newRenderer.sharedMaterial = thisMaterial;											// Put the material on the new mesh
				SFB_BlendShapesManager thisSFBBlendShapesManager = obj.GetComponent<SFB_BlendShapesManager>();	// See if the object has a SFB_BlendShapesManager script attached
				if (thisSFBBlendShapesManager)														// If it does...
					DestroyImmediate(thisSFBBlendShapesManager);
				var animator					= obj.GetComponent<Animator>();						// See if the object has an animator attached
				if (animator)																		// if it does...
					DestroyImmediate(animator);														// Remove the animator component
			}
			Animator animatorParent = thisObject.GetComponent<Animator>();							// See if the object has an animator attached
			if (animatorParent)																		// if it does...
				DestroyImmediate(animatorParent);
			SFB_SaveAsPrefab(path + "/" + folderName + "_" + fileNumber + ".prefab", thisObject);	// Save this as a prefab
			EditorUtility.ClearProgressBar();															// Clear the progress bar
		}
	}

	// This will save the object as a prefab
	static void SFB_SaveAsPrefab(string path, GameObject obj){
		//PrefabUtility.CreatePrefab(path, obj);															// Creates a prefab of obj at path
		PrefabUtility.SaveAsPrefabAsset(obj, path);
	}

	// This function will create a new mesh asset of the selected SkinnedMeshRenderer and save it to the Project
	static Mesh SFB_BakeMesh(string path, GameObject obj, int fileNumber){
		Mesh mesh = new Mesh();																			// Create a new mesh
		SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();				// Get the component
		skinnedMeshRenderer.BakeMesh(mesh);																// Bake the current mesh geometry to the new mesh we created
		string filePath = path + "/" + obj.name + "_" + fileNumber + "" + ".asset";						// File path to saved mesh
		AssetDatabase.CreateAsset(mesh, filePath);														// Save the mesh to Project
		return mesh;																					// Return the mesh asset
	}

	// This function will create the correct path folders if they do not already exist.
	static void SFB_CreateFolder(string path, string value){
		if (!AssetDatabase.IsValidFolder(path + "/" + value))											// If it isn't a folder already
			AssetDatabase.CreateFolder(path, value);													// Create the Folder
	}

	// This function will check the parent folder for a folder that already exists with the name we
	// want to use.  If it does, it'll send back the next available name, with numbers after it.
	static int SFB_GetFolderName(string parentFolder, string value){
		if (AssetDatabase.IsValidFolder(parentFolder + "/" + value))									// If the name is a valid folder, it means we already have one
		{
			int x = 2;																					// Lets start this at #2
			bool foundName = false;																		// We have not yet found the name
			while (!foundName){																			// do this while we haven't found the name
				if (!AssetDatabase.IsValidFolder(parentFolder + "/" + value + "_" + x))					// If we don't have a folder by the name of "[value] [x]"...
				{
					return x;																			// return "[value] [x]"
				}
				x++;																					// Increase value of x by 1
			}
		}
		return 0;																						// Return the value
	}

	// This function will check the file already exists with the name we
	// want to use.  If it does, it'll send back the next available name, with numbers after it.
	static int SFB_GetFileNumber(string parentFolder, string fileName){
		var x = 0;
		while (x < 5000)
		{
			string fullPath = Application.dataPath + "" + parentFolder + "/" + fileName + "_" + x + ".prefab";
			if (!System.IO.File.Exists(fullPath))
			{
				return x;
			}
			else
				x++;
		}
		return 0;
	}

	// This will delete the Animator component, if there is one attached
	static IEnumerator SFB_RemoveAnimator(GameObject obj){
		yield return new WaitForSeconds(0.1f);
		Animator animatorComponent = obj.GetComponent<Animator>();										// Get component for Animator
		DestroyImmediate(animatorComponent);															// Remove the component
	}

	// This script will remove SFB_BlendShapesManager.js from the object, if it's attached.  It's used for
	// anything using my blend shapes script that makes it easier to modify exposed shapes.  Specific
	// to my own packages and assets, or anyone else who decides to adopt the same naming convention.
	static void SFB_RemoveBlendShapesManagerScript(GameObject obj){
		SFB_BlendShapesManager thisSFBBlendShapesManager = obj.GetComponent<SFB_BlendShapesManager>();			// Get component for SFB_BlendShapesManager if it's attached
		if (thisSFBBlendShapesManager)																			// If the script is attached
		{
			SFB_DeleteBlendShapesManagerScripts(obj);															// Run this function
			thisSFBBlendShapesManager.enabled	= false;														// Disable the script
		}
	}

	// This will remove the script, but delayed 1/10th of a second, to avoid errors with other scripts / unity stuff.
	// It is only intended for objects that use SFBayStudios proprietary Blend Shape manager.
	static IEnumerator SFB_DeleteBlendShapesManagerScripts(GameObject obj){
		yield return new WaitForSeconds(0.1f);																					// Wait 0.1 second
		SFB_BlendShapesManager thisSFBBlendShapesManager = obj.GetComponent<SFB_BlendShapesManager>();			// Get component for SFB_BlendShapesManager
		DestroyImmediate(thisSFBBlendShapesManager);															// Remove the script
	}
}






