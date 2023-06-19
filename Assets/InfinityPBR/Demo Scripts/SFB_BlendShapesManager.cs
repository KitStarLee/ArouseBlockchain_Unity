using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

/// <summary>
/// You're free to reuse this script for models that aren't produced by S.F. Bay Studios, Inc / InfinityPBR.com
/// Please, if you do so, mention us at some point -- that'd be nice of you :)
/// 
/// This script, attached to an object with properly named blend shapes, will make it easy for users to modify
/// the shapes both in the editor and during run time.
/// </summary>

[Serializable]
public class SFB_BlendShapesManager : MonoBehaviour {

	public List<SFB_BlendShapeObject> blendShapeObjects = new List<SFB_BlendShapeObject>();
	public List<SFB_InspectorObject> inspectorObjects = new List<SFB_InspectorObject>();
	public List<TextAsset> presetObjects = new List<TextAsset>();
	public bool showWireframe = true;
	public string presetName;
	public bool showPresets;
	public bool showShapes;
	public bool showData;
	public bool showScripting;
	public int selectedShape;
	public float globalRangeModifier = 0.5f;

	[Serializable]
	public class SFB_InspectorObject {
		public int objectID;
		public int shapeID;
		public int blendShapeID;
	}

	[Serializable]
	public class SFB_BlendShapeObject {
		public string name;
		public Mesh meshObject;
		public SkinnedMeshRenderer renderer;
		public List<SFB_BlendShape> blendShapes = new List<SFB_BlendShape>();
		public int primaryShapes = 0;
		public bool expandedInspector = false;
	}

	[Serializable]
	public class SFB_BlendShape {
		public string name;
		public string fullName;
		public bool isMinus = false;
		public bool isPlus = false;
		public int inspectorID;
		public int id;
		public float minValue = 0.0f;
		public float maxValue = 0.0f;
		public float value;
		public float sliderValue;
		public float changeDuration = 0.0f;
		public bool isVisible;
		public List<SFB_BlendMatch> blendMatches = new List<SFB_BlendMatch> ();
		public bool presetExport = true;
		public int presetExportValue = 1;  // 1 = export, 0 = do not export, 2 = export random
	}

	[Serializable]
	public class SFB_BlendMatch {
		public string name;
		public int objectID;
		public int shapeID;
	}

	public void SFB_BS_ToggleRange(int value){
		for (int i = 0; i < inspectorObjects.Count; i++){
			SFB_InspectorObject inspectorObject = inspectorObjects [i];
			SFB_BlendShape blendShape = blendShapeObjects [inspectorObject.objectID].blendShapes [inspectorObject.shapeID];
			if (value == 0) {
				blendShape.minValue = value;
				blendShape.maxValue = value;
			} else {
				if (blendShape.isPlus) {
					blendShape.minValue = -value;
					blendShape.maxValue = value;
				} else {
					blendShape.minValue = 0;
					blendShape.maxValue = value;
				}
			}
		}
	}

	public void RandomizeAll(){
		for (int i = 0; i < inspectorObjects.Count; i++) {
			SFB_BlendShapeObject blendShapeObject = blendShapeObjects[inspectorObjects[i].objectID];
			GameObject inGameObject = blendShapeObject.renderer.gameObject;
			if (inGameObject.activeSelf){
				SFB_BlendShape blendShape = blendShapeObject.blendShapes[inspectorObjects[i].shapeID];
				SFB_BS_SetRandom(blendShape);
			}
		}
	}

	public void SFB_BS_SetRandom(SFB_BlendShape blendShape){
		blendShape.sliderValue = Mathf.Round(UnityEngine.Random.Range(blendShape.minValue, blendShape.maxValue));
		SetValue(inspectorObjects[blendShape.inspectorID].objectID, inspectorObjects[blendShape.inspectorID].shapeID, blendShape.id, blendShape.sliderValue);
		if (blendShape.isPlus && GetMinusShapeObject(blendShape.name) != 999999) {
			int minusShapeObject = GetMinusShapeObject (blendShape.name);
            if (minusShapeObject != 999999)
            { 
                int minusShapeID = GetMinusShapeID (blendShape.name);
                SFB_BlendShape minusShape = blendShapeObjects[minusShapeObject].blendShapes[minusShapeID];
                minusShape.sliderValue = -blendShape.sliderValue;
                SetValue(minusShapeObject, minusShapeID, minusShape.id, minusShape.sliderValue);
            }
		}
	}

	public void SFB_BS_TogglePresets(bool value){
		for (int i = 0; i < blendShapeObjects.Count; i++){
			for (int s = 0; s < blendShapeObjects[i].blendShapes.Count; s++){
				blendShapeObjects[i].blendShapes[s].presetExport	= value;
			}
		}
	}

	public void SFB_BS_TogglePresetsValue(int objectID, int value){
		for (int i = 0; i < inspectorObjects.Count; i++){
			SFB_InspectorObject inspectorObject = inspectorObjects [i];
			if (inspectorObject.objectID == objectID || objectID == 999999) {
				SFB_BlendShape blendShape = blendShapeObjects[inspectorObject.objectID].blendShapes[inspectorObject.shapeID];
				blendShape.presetExportValue	= value;
			}
		}
	}

	public void SFB_BS_ShowWireframe(bool value){
		#if UNITY_EDITOR
		for (int i = 0; i < blendShapeObjects.Count; i++){
			if (value) {
				EditorUtility.SetSelectedRenderState (blendShapeObjects [i].renderer, EditorSelectedRenderState.Highlight);
			} else {
				EditorUtility.SetSelectedRenderState (blendShapeObjects [i].renderer, EditorSelectedRenderState.Hidden);
			}
		}
		showWireframe = value;
		#endif
	}

	public void SetSelectedShape(int newValue){
		selectedShape = newValue;
	}

	public void SetValueUI(float newValue){
		int objectID = inspectorObjects [selectedShape].objectID;
		int shapeID = inspectorObjects[selectedShape].shapeID;
		SFB_BlendShape blendShapeData = blendShapeObjects [objectID].blendShapes [shapeID];
		if (blendShapeData.isPlus && GetMinusShapeObject(blendShapeData.name) != 999999) {
			int minusShapeObject = GetMinusShapeObject (blendShapeData.name);
            if (minusShapeObject != 999999)
            {
                int minusShapeID = GetMinusShapeID (blendShapeData.name);
                SFB_BlendShape minusShapeData = blendShapeObjects[minusShapeObject].blendShapes[minusShapeID];
                blendShapeData.sliderValue = newValue;
                minusShapeData.sliderValue = -newValue;
                SetValue(minusShapeObject, minusShapeID, minusShapeData.id, -newValue);
            }
		}
		SetValue(inspectorObjects[selectedShape].objectID, inspectorObjects[selectedShape].shapeID, inspectorObjects[selectedShape].blendShapeID, newValue);
	}

	public void SetValue(int objectID, int shapeID, int blendShapeID, float value){
        //Debug.Log("Sev Value " + objectID + ", " + shapeID + "," + blendShapeID + "," + value);
		blendShapeObjects [objectID].blendShapes [blendShapeID].value = blendShapeObjects [objectID].blendShapes [blendShapeID].sliderValue;
		if (blendShapeObjects [objectID].renderer) {
			blendShapeObjects [objectID].renderer.SetBlendShapeWeight (blendShapeID, value * globalRangeModifier);
			if (blendShapeObjects [objectID].blendShapes [shapeID].blendMatches.Count > 0) {
				for (int m = 0; m < blendShapeObjects[objectID].blendShapes[shapeID].blendMatches.Count; m++){
					int matchObject = blendShapeObjects[objectID].blendShapes[shapeID].blendMatches[m].objectID;
					int matchShape = blendShapeObjects[objectID].blendShapes[shapeID].blendMatches[m].shapeID;
					blendShapeObjects [matchObject].renderer.SetBlendShapeWeight (matchShape, value * globalRangeModifier);
				}
			}
		}
	}

	public int AddToInspectorObjects(int newObjectID, int newShapeID, int newBlendShapeID){
		int currentCount = inspectorObjects.Count;
		SFB_InspectorObject newObject = new SFB_InspectorObject ();;
		inspectorObjects.Add (newObject);
		inspectorObjects [currentCount] = new SFB_InspectorObject ();
		inspectorObjects [currentCount].objectID = newObjectID;
		inspectorObjects [currentCount].shapeID = newShapeID;
		inspectorObjects [currentCount].blendShapeID = newBlendShapeID;
		return currentCount;
	}

	public void AddObject(Mesh newMesh, SkinnedMeshRenderer newRenderer){
		int currentCount = blendShapeObjects.Count;
		SFB_BlendShapeObject newObject = new SFB_BlendShapeObject ();
		blendShapeObjects.Add (newObject);
		blendShapeObjects [currentCount] = new SFB_BlendShapeObject ();
		blendShapeObjects[currentCount].name = newMesh.name;
		blendShapeObjects[currentCount].meshObject = newMesh;
		blendShapeObjects[currentCount].renderer = newRenderer;
		int primaryShapes = 0;
		for (int i = 0; i < newMesh.blendShapeCount; i++){
			SFB_BlendShape newBlendShape = new SFB_BlendShape();;
			int currentShapeCount = blendShapeObjects [currentCount].blendShapes.Count;
			blendShapeObjects[currentCount].blendShapes.Add(newBlendShape);
			blendShapeObjects[currentCount].blendShapes[currentShapeCount] = new SFB_BlendShape();
			blendShapeObjects[currentCount].blendShapes[currentShapeCount].id = i;
			blendShapeObjects[currentCount].blendShapes[currentShapeCount].fullName = newMesh.GetBlendShapeName(i);
			string humanName = GetHumanName (newMesh.GetBlendShapeName (i));
			blendShapeObjects[currentCount].blendShapes[currentShapeCount].name = humanName;
			blendShapeObjects[currentCount].blendShapes[currentShapeCount].isVisible = DisplayThisBlendshape(newMesh.GetBlendShapeName(i));

			if (blendShapeObjects [currentCount].blendShapes [currentShapeCount].isVisible) {
				primaryShapes++;
			}

			if (humanName != "") {
				int originalLength = humanName.Length;
				string minusCheck = humanName.Replace ("Minus", "");
				if (minusCheck.Length != originalLength) {
					blendShapeObjects [currentCount].blendShapes [currentShapeCount].isMinus = true;
					blendShapeObjects [currentCount].blendShapes [currentShapeCount].isVisible = false;
				} else {
					if (blendShapeObjects [currentCount].blendShapes [currentShapeCount].isVisible) {
						blendShapeObjects [currentCount].blendShapes [currentShapeCount].inspectorID = AddToInspectorObjects (currentCount, currentShapeCount, i);
					}
				}
				string plusCheck = humanName.Replace ("Plus", "");
				if (plusCheck.Length != originalLength) {
					blendShapeObjects [currentCount].blendShapes [currentShapeCount].isPlus = true;
				}
			}
			blendShapeObjects[currentCount].blendShapes[currentShapeCount].value = blendShapeObjects[currentCount].renderer.GetBlendShapeWeight(i);
			blendShapeObjects[currentCount].blendShapes[currentShapeCount].sliderValue = blendShapeObjects[currentCount].renderer.GetBlendShapeWeight(i);
		}
		blendShapeObjects[currentCount].primaryShapes = primaryShapes;
	}

	public int GetMinusShapeID(string plusName){
		string minusName = plusName.Replace ("Plus", "Minus");
		for (int o = 0; o < blendShapeObjects.Count; o++){
			for (int s = 0; s < blendShapeObjects[o].blendShapes.Count; s++){
				if (blendShapeObjects [o].blendShapes [s].name == minusName) {
					return s;
				}
			}
		}
		//Debug.Log ("Oops! Cound not get minus shape ID.  (Shape name may include \"Plus\", but we could not find a corresponding \"Minus\"");
		return 999999;
	}

	public int GetMinusShapeObject(string plusName){
		string minusName = plusName.Replace ("Plus", "Minus");
		for (int o = 0; o < blendShapeObjects.Count; o++){
			for (int s = 0; s < blendShapeObjects[o].blendShapes.Count; s++){
				if (blendShapeObjects [o].blendShapes [s].name == minusName) {
					return o;
				}
			}
		}
        //Debug.Log("Oops! Cound not get minus shape object.  (Shape name may include \"Plus\", but we could not find a corresponding \"Minus\"");
        return 999999;
	}

	public int VisibleBlendShapes(Mesh newMesh){
		int totalObject = 0;
		for (int i = 0; i < newMesh.blendShapeCount; i++) {
			if (DisplayThisBlendshape (newMesh.GetBlendShapeName (i))) {
				totalObject++;
			}
		}
		return totalObject;
	}

	public int MatchedBlendShapes(Mesh newMesh){
		int totalObject = 0;
		for (int i = 0; i < newMesh.blendShapeCount; i++) {
			if (MatchThisBlendshape (newMesh.GetBlendShapeName (i))) {
				totalObject++;
			}
		}
		return totalObject;
	}

	public string GetHumanName(string blendShapeName){
		string humanName = "";
		string[] periodParse = blendShapeName.Split(new string[] { "." }, System.StringSplitOptions.None);

		if (periodParse.Length > 1) {
			string[] nameParse = periodParse [1].Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse.Length >= 3) {
				return nameParse [2];
			}
		} else {
			string[] nameParse2 = blendShapeName.Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse2.Length >= 3) {
				return nameParse2 [2];
			}
		}
		return humanName;
	}

	public string GetHumanNameMatch(string blendShapeName){
		string humanName = "";
		string[] periodParse = blendShapeName.Split(new string[] { "." }, System.StringSplitOptions.None);
		if (blendShapeName.Contains(".")) {
			string[] nameParse = periodParse [1].Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse.Length >= 4) {
				return nameParse [3];
			}
		} else {
			string[] nameParse2 = blendShapeName.Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse2.Length >= 4) {
				return nameParse2 [3];
			}
		}
		return humanName;
	}

	public bool MatchThisBlendshape(string blendShapeName){
		string[] periodParse = blendShapeName.Split(new string[] { "." }, System.StringSplitOptions.None);
		if (periodParse.Length > 1) {
			string[] nameParse = periodParse [1].Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse.Length > 3) {
				if (nameParse [1] == "BSM") {
					return true;
				}
			}
		} else {
			string[] nameParse2 = blendShapeName.Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse2.Length >= 3) {
				if (nameParse2 [1] == "BSM") {
					return true;
				}
			}
		}
		return false;
	}

	public bool DisplayThisBlendshape(string blendShapeName){
		string[] periodParse = blendShapeName.Split(new string[] { "." }, System.StringSplitOptions.None);
		if (periodParse.Length > 1) {
			string[] nameParse = periodParse [1].Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse.Length >= 3){
				if (nameParse [1] == "BS") {
					return true;
				}
			}
		} else {
			string[] nameParse2 = blendShapeName.Split(new string[] { "_" }, System.StringSplitOptions.None);
			if (nameParse2.Length >= 3){
				if (nameParse2 [1] == "BS") {
					return true;
				}
			}
		}
		return false;
	}

	public void ReloadBlendShapes(){
		blendShapeObjects.Clear ();
		inspectorObjects.Clear ();
		SkinnedMeshRenderer[] allChildren = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		Debug.Log("allChildren Length: " + allChildren.Length);
		int counter = 0;
		foreach (SkinnedMeshRenderer child in allChildren)
		{
			Debug.Log(counter);
			counter++;
			Debug.Log("Child: " + child.name);
			Mesh newMesh = child.sharedMesh;
			Debug.Log("newMesh: " + newMesh.name);
            Debug.Log("blendShapeCount: " + newMesh.blendShapeCount);
			if (newMesh.blendShapeCount > 0) {
				AddObject (newMesh, child);
			}
		}
		for (int m = 0; m < blendShapeObjects.Count; m++) {
			for (int i2 = 0; i2 < blendShapeObjects[m].meshObject.blendShapeCount; i2++){
				if (MatchThisBlendshape(blendShapeObjects[m].meshObject.GetBlendShapeName(i2))){
					string matchHumanName = GetHumanNameMatch (blendShapeObjects [m].meshObject.GetBlendShapeName (i2));
					int matchObjectID = m;
					int matchShapeID = i2;
					AddMatchToNamedShape (matchHumanName, blendShapeObjects [m].meshObject.GetBlendShapeName (i2), matchObjectID, matchShapeID);
				}
			}
		}
	}

	public void AddMatchToNamedShape(string humanName, string matchName, int matchObjectID, int matchShapeID){
		for (int o = 0; o < blendShapeObjects.Count; o++) {
			for (int s = 0; s < blendShapeObjects[o].blendShapes.Count; s++){
				if (blendShapeObjects [o].blendShapes [s].name == humanName) {
					SFB_BlendMatch newBlendMatch = new SFB_BlendMatch ();
					int currentMatchCount = blendShapeObjects[o].blendShapes[s].blendMatches.Count;
					blendShapeObjects[o].blendShapes[s].blendMatches.Add(newBlendMatch);
					blendShapeObjects[o].blendShapes[s].blendMatches[currentMatchCount] = new SFB_BlendMatch();
					blendShapeObjects[o].blendShapes[s].blendMatches[currentMatchCount].name = matchName;
					blendShapeObjects[o].blendShapes[s].blendMatches[currentMatchCount].objectID = matchObjectID;
					blendShapeObjects[o].blendShapes[s].blendMatches[currentMatchCount].shapeID = matchShapeID;
				}
			}
		}
	}

	public void FindMatches(string name, int id, int shapeID, int objectID){
		int childCount = gameObject.transform.childCount;
		int i2 = 0;
		for (int i = 0; i < childCount; i++){
			Transform childObject = gameObject.transform.GetChild (i);
			if (childObject.GetComponent<SkinnedMeshRenderer> ()) {
				SkinnedMeshRenderer newRenderer = childObject.GetComponent<SkinnedMeshRenderer> ();
				Mesh newMesh = newRenderer.sharedMesh;
				if (newMesh.blendShapeCount > 0) {
					CheckMatch (i2, newMesh, newRenderer, name, shapeID, objectID);
				}
				i2++;
			}
		}
	}

	public void CheckMatch(int matchObjectID, Mesh newMesh, SkinnedMeshRenderer newRenderer, string matchName, int shapeID, int objectID){
		if (MatchedBlendShapes (newMesh) != 0) {
			for (int i = 0; i < newMesh.blendShapeCount; i++){
				if (GetHumanNameMatch (newMesh.GetBlendShapeName (i)) == matchName) {
					SFB_BlendMatch newBlendMatch = new SFB_BlendMatch();
					int currentMatchCount = blendShapeObjects [objectID].blendShapes [shapeID].blendMatches.Count;
					blendShapeObjects[objectID].blendShapes[shapeID].blendMatches.Add(newBlendMatch);
					blendShapeObjects[objectID].blendShapes[shapeID].blendMatches[currentMatchCount] = new SFB_BlendMatch();
					blendShapeObjects[objectID].blendShapes[shapeID].blendMatches[currentMatchCount].name = newMesh.GetBlendShapeName(i);
					blendShapeObjects[objectID].blendShapes[shapeID].blendMatches[currentMatchCount].objectID = matchObjectID;
					blendShapeObjects[objectID].blendShapes[shapeID].blendMatches[currentMatchCount].shapeID = i;
				}
			}
		}
	}

	/// <summary>
	/// This is for editor use only.  To save players desired settings, best to use playerprefs or some other saving method.
	/// </summary>
	public void SFB_BS_ExportPreset(){
        string savedData = "SFB_BlendShapesManager V2";
        for (int i = 0; i < inspectorObjects.Count; i++){
            int presetExportValue = blendShapeObjects[inspectorObjects[i].objectID].blendShapes[inspectorObjects[i].blendShapeID].presetExportValue;
            if (presetExportValue != 0)
            {

                if (savedData != "")
                {
                    savedData = savedData + ",";
                }

                savedData = savedData + blendShapeObjects[inspectorObjects[i].objectID].name;
                savedData = savedData + ",";
                savedData = savedData + blendShapeObjects[inspectorObjects[i].objectID].blendShapes[inspectorObjects[i].blendShapeID].name;
                savedData = savedData + ",";

                //int presetExportValue = blendShapeObjects[inspectorObjects[i].objectID].blendShapes[inspectorObjects[i].blendShapeID].presetExportValue;
                //if (presetExportValue == 0)
                //	savedData = savedData + "*";
                //else 
                if (presetExportValue == 1)
                {
                    var currentWeight = blendShapeObjects[inspectorObjects[i].objectID].renderer.GetBlendShapeWeight(inspectorObjects[i].blendShapeID);
                    savedData = savedData + currentWeight;
                }
                else if (presetExportValue == 2)
                    savedData = savedData + "R";
            }
		}
		string presetPath = "Assets/InfinityPBR/Blendshape Preset Files/";
		string fileName = "";
		if (presetName == null || presetName == "")
		{
			fileName = this.name + SFB_BS_GetNewFileNumber(presetPath, this.name) + ".txt";
		}
		else
			fileName = presetName + SFB_BS_GetNewFileNumber(presetPath, presetName) + ".txt";

		if (!Directory.Exists(presetPath))
			System.IO.Directory.CreateDirectory(presetPath);

		System.IO.File.WriteAllText(presetPath + fileName, savedData);
		#if UNITY_EDITOR
		AssetDatabase.Refresh();
		#endif
	}

	/// <summary>
	/// This is for editor use only.  To save players desired settings, best to use playerprefs or some other saving method.
	/// </summary>
	public void SFB_BS_ExportRanges(){
		string savedData = "SFB_BlendShapesManager V2";
		for (int i = 0; i < inspectorObjects.Count; i++){
			if (savedData != "") {
				savedData = savedData + ",";
			}
            savedData = savedData + blendShapeObjects[inspectorObjects[i].objectID].name;
            savedData = savedData + ",";
            savedData = savedData + blendShapeObjects[inspectorObjects[i].objectID].blendShapes[inspectorObjects[i].blendShapeID].name;
            savedData = savedData + ",";
            savedData = savedData + "" + blendShapeObjects[inspectorObjects[i].objectID].blendShapes[inspectorObjects[i].blendShapeID].minValue;
			savedData = savedData + ",";
			savedData = savedData + "" + blendShapeObjects[inspectorObjects[i].objectID].blendShapes[inspectorObjects[i].blendShapeID].maxValue;
		}
		string presetPath = "Assets/InfinityPBR/Blendshape Preset Files/";
		string fileName = this.name + " Range " + SFB_BS_GetNewFileNumber(presetPath, this.name + " Range ") + ".txt";
		if (!Directory.Exists(presetPath))
			System.IO.Directory.CreateDirectory(presetPath);

		System.IO.File.WriteAllText(presetPath + fileName, savedData);
		#if UNITY_EDITOR
		AssetDatabase.Refresh();
		#endif
	}

	public void SFB_BS_LoadRanges(TextAsset presetFile){
		string contents = presetFile.text;
		string[] shapesText = contents.Split(new string[] { "," }, System.StringSplitOptions.None);
		for (int i = 1; i < shapesText.Length; i++){
            //Debug.Log("+0: " + shapesText[i]);
            //Debug.Log("+1: " + shapesText[i + 1]);
           //Debug.Log("+2: " + shapesText[i + 2]);
            //Debug.Log("+3: " + shapesText[i + 3]);
            int shapeID = i * 4;

            int objectID = GetBlendShapeObjectIndex(shapesText[i]);
            if (objectID != 999999)
            {
                int blendShapeID = GetBlendShapesIndex(objectID, shapesText[i + 1]);
                if (blendShapeID != 999999)
                {
                    blendShapeObjects[objectID].blendShapes[blendShapeID].minValue = float.Parse(shapesText[i + 2]);
                    blendShapeObjects[objectID].blendShapes[blendShapeID].maxValue = float.Parse(shapesText[i + 3]);

                }
            }


            i++;
            i++;
            i++;
		}
	}

	public void SFB_BS_ImportPresetFile(TextAsset presetFile){
		string contents = presetFile.text;
		string[] shapesText;
		shapesText = contents.Split(new string[] { "," }, System.StringSplitOptions.None);
		for (int i = 1; i < shapesText.Length; i++){    // START AT 1, since index 0 is the version information
           // Debug.Log("Processing Object " + shapesText[i] + ", Shape " + shapesText[i + 1] + ", Value " + shapesText[i + 2]);
            int objectIndex = GetBlendShapeObjectIndex(shapesText[i]);
            int shapeIndex = GetBlendShapesIndex(objectIndex, shapesText[i + 1]);

            if (shapeIndex != 999999)
            {
                SFB_BlendShape blendShapeData = blendShapeObjects[objectIndex].blendShapes[shapeIndex];
                if (shapesText[i + 2] == "R")
                {
                    SFB_BS_SetRandom(blendShapeData);
                }
                else
                {
                    float shapeValue = float.Parse(shapesText[i + 2]);
                    blendShapeData.sliderValue = shapeValue;
                    SetValue(objectIndex, shapeIndex, blendShapeData.id, shapeValue);

                    if (blendShapeData.isPlus && GetMinusShapeObject(blendShapeData.name) != 999999)
                    {
                        int minusShapeObject = GetMinusShapeObject(blendShapeData.name);
                        if (minusShapeObject != 999999)
                        {
                            int minusShapeID = GetMinusShapeID(blendShapeData.name);
                            SFB_BlendShape minusShape = blendShapeObjects[minusShapeObject].blendShapes[minusShapeID];
                            minusShape.sliderValue = -blendShapeData.sliderValue;
                            SetValue(minusShapeObject, minusShapeID, minusShape.id, minusShape.sliderValue);
                        }
                    }
                }

                if (blendShapeData.isPlus)
                {

                }

            }


            i++;
            i++;

           /*if (shapesText [i] != "*") {
				SFB_BlendShapeObject bsObject = blendShapeObjects [inspectorObjects [i].objectID];
				if (bsObject.renderer.gameObject.activeSelf) {
					SFB_BlendShape bsShape = bsObject.blendShapes[inspectorObjects[i].shapeID];
					if (shapesText [i] == "R") {
						SFB_BS_SetRandom (bsShape);
					} else {
						float shapeValue = float.Parse(shapesText[i]);
						SetValue(inspectorObjects[i].objectID, inspectorObjects[i].shapeID, inspectorObjects[i].blendShapeID, shapeValue);
						bsShape.sliderValue	= shapeValue;
					}
				}
			}*/
		}
	}

    public int GetBlendShapeObjectIndex(string name)
    {
        for (int i = 0; i < blendShapeObjects.Count; i++)
        {
            if (blendShapeObjects[i].name == name)
            {
                return i;
            }
        }
        Debug.Log("Potential Issue: Didn't find an index for Blend Shape Object named " + name);
        return 999999;
    }

    public int GetBlendShapesIndex(int objectIndex, string name)
    {
        for (int i = 0; i < blendShapeObjects[objectIndex].blendShapes.Count; i++)
        {
            if (blendShapeObjects[objectIndex].blendShapes[i].name == name)
            {
                return i;
            }
        }
        Debug.Log("Potential Issue: Didn't find an index for Blend Shape named " + name);
        return 999999;
    }

    public void GetBlendShapesIndex(string objectName, string name)
    {
        GetBlendShapesIndex(GetBlendShapeObjectIndex(objectName), name);
    }

    public void SFB_BS_ResetAll(){
		for (int i = 0; i < inspectorObjects.Count; i++) {
			SFB_BlendShapeObject bsObject = blendShapeObjects[inspectorObjects[i].objectID];
			SFB_BlendShape bsShape = bsObject.blendShapes[inspectorObjects[i].shapeID];
			SetValue(inspectorObjects[i].objectID, inspectorObjects[i].shapeID, inspectorObjects[i].shapeID, 0);
			if (bsShape.isPlus && GetMinusShapeObject(bsShape.name) != 999999) {
                int minusShapeObject = GetMinusShapeObject (bsShape.name);
                if (minusShapeObject != 999999)
                {
                    int minusShapeID = GetMinusShapeID (bsShape.name);
                    SFB_BlendShape minusShapeData = blendShapeObjects[minusShapeObject].blendShapes[minusShapeID];
                    minusShapeData.sliderValue = 0;
                    SetValue(minusShapeObject, minusShapeID, minusShapeData.id, 0);
                }
			}
			bsShape.sliderValue	= 0;
		}
	}

	public void ResetAllObjects(SFB_BlendShapeObject bsObject){
		int objectID = GetObjectID (bsObject);
		for (int i = 0; i < bsObject.blendShapes.Count; i++){
			SFB_BlendShape bsShape = bsObject.blendShapes [i];
			SetValue (objectID, i, i, 0);
			if (bsShape.isPlus && GetMinusShapeObject(bsShape.name) != 999999) {
				int minusShapeObject = GetMinusShapeObject(bsShape.name);
                if (minusShapeObject != 999999)
                { 
                  int minusShapeID = GetMinusShapeID(bsShape.name);
                    SFB_BlendShape minusShapeData = blendShapeObjects[minusShapeObject].blendShapes[minusShapeID];
                    minusShapeData.sliderValue = 0;
                    SetValue(minusShapeObject, minusShapeID, minusShapeData.id, 0);
                }
            }
			bsShape.sliderValue = 0;
		}
	}

	public int GetObjectID(SFB_BlendShapeObject blendShapeObject){
		for (int i = 0; i < inspectorObjects.Count; i++){
			if (blendShapeObjects [inspectorObjects [i].objectID] == blendShapeObject) {
				return inspectorObjects [i].objectID;
			}
		}
		Debug.Log ("Error!  Could not get object ID");
		return 0;
	}

	public int SFB_BS_GetNewFileNumber(string path, string fileName){
		int x = 0;
		while (x < 5000) {
			if (!System.IO.File.Exists (path + fileName + x + ".txt")) {
				return x;
			}
			x++;
		}
		Debug.Log ("Error.  Went through 5000 names already!  That's crazy.");
		return 0;
	}

	public void SFB_BS_RemovePreset(int presetID){
		presetObjects.RemoveAt (presetID);
	}
}
