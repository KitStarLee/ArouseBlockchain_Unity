using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(SFB_BlendShapesManager))]
[CanEditMultipleObjects]
[Serializable]
public class SFB_BlendShapesManagerEditor : Editor  {

	public TextAsset randomRangeObject;
	private string[] presetExportOptions = new string[] {"Do Not Record", "Record Value", "Random on Load"};

	public override void OnInspectorGUI()
	{
		SFB_BlendShapesManager myScript = (SFB_BlendShapesManager)target;

		Undo.RecordObject (myScript, "Show Scripting Details");
		myScript.showScripting = EditorGUILayout.Foldout(myScript.showScripting, "Scripting Details");
		if (myScript.showScripting)
		{
			EditorGUILayout.HelpBox("SCRIPTING INDIVIDUAL VALUES\nCall SetSelectedShape(shapeIDNumber : int) first\nCall SetValueUI(newValue : float) to set value\nID # is listed next to Blend Shape name\n\nLOADING PRESET FILES\nCall SFB_BS_ImportPreset(presetID : int) to load a preset file", MessageType.Info);
		}

		Undo.RecordObject (myScript, "Show Presets");
		myScript.showPresets = EditorGUILayout.Foldout(myScript.showPresets, "Preset Files");
		if (myScript.showPresets)
		{
			EditorGUILayout.HelpBox("BLEND SHAPE PRESET FILES\nPreset Files are text files created by this script.  You can save settings, including \"Random\" settings, to fine-tune the randomization of the Blend Shapes.  Preset Files can be loaded at run time as well.", MessageType.Info);
			EditorGUILayout.BeginHorizontal ();
			TextAsset newPresetObject = null;
			newPresetObject = EditorGUILayout.ObjectField(newPresetObject, typeof(TextAsset), false, GUILayout.Height(30)) as TextAsset;
			EditorGUILayout.EndHorizontal ();

			if (newPresetObject)
			{
				myScript.presetObjects.Add(newPresetObject);
				newPresetObject	= null;
			}

			if (myScript.presetObjects.Count == 0)
				EditorGUILayout.HelpBox("Drop Preset Objects into the field above.", MessageType.Info);
			else
			{
				for (int p = 0; p < myScript.presetObjects.Count; p++){
					EditorGUILayout.BeginHorizontal ();
					if (!myScript.presetObjects[p])
						myScript.SFB_BS_RemovePreset(p);
					else
					{
						GUILayout.Label(myScript.presetObjects[p].name + " (" + p + ")");
						GUI.color 			= Color.green;
						if(GUILayout.Button("Load Preset", GUILayout.Width(100)))
						{
							myScript.SFB_BS_ImportPresetFile(myScript.presetObjects[p]);
						}
						GUI.color 			= Color.red;
						if(GUILayout.Button("Remove", GUILayout.Width(80)))
						{
							myScript.SFB_BS_RemovePreset(p);
						}
						GUI.color 			= Color.white;
					}
					EditorGUILayout.EndHorizontal ();
				}
			}

		}

		Undo.RecordObject (myScript, "Show Shapes");
		myScript.showShapes = EditorGUILayout.Foldout(myScript.showShapes, "Blend Shapes");
		if (myScript.showShapes)
		{
			if (myScript.globalRangeModifier == 0)
			{
				myScript.globalRangeModifier = 0.5f;
			}

			EditorGUILayout.BeginHorizontal ();
			Undo.RecordObject (myScript, "Change Preset Name");
			myScript.presetName = EditorGUILayout.TextField("Preset File Name: ", myScript.presetName);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			GUI.color 			= Color.green;
			if(GUILayout.Button("Export Values as Preset", GUILayout.Width(150)))
			{
				myScript.SFB_BS_ExportPreset();
			}
			GUI.color 			= Color.white;
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal ();
			if(GUILayout.Button("Export Ranges Preset", GUILayout.Width(150)))
			{
				myScript.SFB_BS_ExportRanges();
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.BeginHorizontal();
			randomRangeObject		= EditorGUILayout.ObjectField(randomRangeObject, typeof(TextAsset), false) as TextAsset;
			if(GUILayout.Button("Load Random Ranges", GUILayout.Width(150)))
			{
				myScript.SFB_BS_LoadRanges(randomRangeObject);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Toggle All:");
			if(GUILayout.Button("Include", GUILayout.Width(100)))
			{
				myScript.SFB_BS_TogglePresetsValue(999999, 1);
			}
			if(GUILayout.Button("Random", GUILayout.Width(100)))
			{
				myScript.SFB_BS_TogglePresetsValue(999999, 2);
			}
			if(GUILayout.Button("Exclude", GUILayout.Width(100)))
			{
				myScript.SFB_BS_TogglePresetsValue(999999, 0);
			}
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Global Range Modifier (Set to 0.5 to fix a bug introduced in Unity 2018.3)");
			myScript.globalRangeModifier =
				EditorGUILayout.FloatField(myScript.globalRangeModifier,
					GUILayout.Width(40));
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label("Random Range:");
			if(GUILayout.Button("Randomize All", GUILayout.Width(100)))
			{
				myScript.RandomizeAll();
			}
			if(GUILayout.Button("Set Max Range", GUILayout.Width(100)))
			{
				myScript.SFB_BS_ToggleRange(100);
			}
			if(GUILayout.Button("Set Range 0", GUILayout.Width(100)))
			{
				myScript.SFB_BS_ToggleRange(0);
			}
			EditorGUILayout.EndHorizontal ();

			if (myScript.blendShapeObjects.Count > 0)
			{
				for (int o = 0; o < myScript.blendShapeObjects.Count; o++){
					SFB_BlendShapesManager.SFB_BlendShapeObject blendShapeObject = myScript.blendShapeObjects[o];
					if (blendShapeObject == null || blendShapeObject.name == "")
					{
						Debug.Log("blendShapeObject is null. Try reloading blend shapes.");
						return;
					}
					else
					{
						//Debug.Log("blendShapeObject: " + blendShapeObject.name);
						//Debug.Log("blendShapeObject.meshObject: " + blendShapeObject.meshObject.name);
						Mesh blendShapeMesh = blendShapeObject.meshObject;

						string meshName = blendShapeMesh.name;
						if (blendShapeObject.primaryShapes > 0)
						{
							string fixedMeshName = meshName.Replace("_", " ");
							Undo.RecordObject(myScript, "Show Objects");
							myScript.blendShapeObjects[o].expandedInspector =
								EditorGUILayout.Foldout(myScript.blendShapeObjects[o].expandedInspector, fixedMeshName);
							if (!myScript.blendShapeObjects[o].expandedInspector)
							{
								
							}
							else
							{
								if (myScript.blendShapeObjects[o].blendShapes.Count != 0)
								{
									EditorGUILayout.BeginHorizontal();
									GUILayout.Label("Toggle All:");
									if (GUILayout.Button("Include", GUILayout.Width(100)))
									{
										myScript.SFB_BS_TogglePresetsValue(o, 1);
									}

									if (GUILayout.Button("Random", GUILayout.Width(100)))
									{
										myScript.SFB_BS_TogglePresetsValue(o, 2);
									}

									if (GUILayout.Button("Exclude", GUILayout.Width(100)))
									{
										myScript.SFB_BS_TogglePresetsValue(o, 0);
									}

									EditorGUILayout.EndHorizontal();
									EditorGUILayout.BeginHorizontal();
									SFB_BlendShapesManager.SFB_BlendShape blendShapeDataReset =
										new SFB_BlendShapesManager.SFB_BlendShape();
									if (GUILayout.Button("Randomize All", GUILayout.Width(100)))
									{
										GUI.color = Color.green;
										Undo.RecordObject(myScript, "Randomize Values");
										for (int r0 = 0; r0 < blendShapeObject.blendShapes.Count; r0++)
										{
											blendShapeDataReset = blendShapeObject.blendShapes[r0];
											if (blendShapeDataReset.isVisible)
											{
												myScript.SFB_BS_SetRandom(blendShapeDataReset);
											}
										}

										GUI.color = Color.white;
									}

									if (GUILayout.Button("Reset Values", GUILayout.Width(100)))
									{
										Undo.RecordObject(myScript, "Reset Values to Zero");
										for (int r = 0; r < blendShapeObject.blendShapes.Count; r++)
										{
											blendShapeDataReset = blendShapeObject.blendShapes[r];
											if (blendShapeDataReset.isVisible)
											{
												blendShapeDataReset.sliderValue = 0;
											}
										}
									}

									if (GUILayout.Button("Set Max Range", GUILayout.Width(100)))
									{
										Undo.RecordObject(myScript, "Set Max Range");
										for (int r2 = 0; r2 < blendShapeObject.blendShapes.Count; r2++)
										{
											blendShapeDataReset = blendShapeObject.blendShapes[r2];
											if (blendShapeDataReset.isVisible)
											{
												if (blendShapeDataReset.isPlus)
												{
													blendShapeDataReset.minValue = -100;
													blendShapeDataReset.maxValue = 100;
												}
												else
												{
													blendShapeDataReset.minValue = 0;
													blendShapeDataReset.maxValue = 100;
												}
											}
										}
									}

									if (GUILayout.Button("Set Range 0", GUILayout.Width(100)))
									{
										Undo.RecordObject(myScript, "Set Range 0");
										for (int r3 = 0; r3 < blendShapeObject.blendShapes.Count; r3++)
										{
											blendShapeDataReset = blendShapeObject.blendShapes[r3];
											if (blendShapeDataReset.isVisible)
											{
												blendShapeDataReset.minValue = 0;
												blendShapeDataReset.maxValue = 0;
											}
										}
									}

									EditorGUILayout.EndHorizontal();

									var shownRows = 0;
									for (int i = 0; i < blendShapeObject.blendShapes.Count; i++)
									{
										SFB_BlendShapesManager.SFB_BlendShape blendShapeData =
											blendShapeObject.blendShapes[i];
										if (blendShapeData.isVisible)
										{
											EditorGUILayout.BeginVertical(EditorStyles.helpBox);
											if (shownRows > 0)
												EditorGUILayout.Space();
											shownRows++;
											EditorGUILayout.BeginHorizontal();
											var displayName = blendShapeData.name;
											if (blendShapeData.isPlus)
											{
												displayName = displayName.Replace("Plus", "");
												displayName = displayName.Replace("plus", "");
											}

											GUILayout.Label(blendShapeData.inspectorID + ": " + displayName,
												GUILayout.Width(200));

											if (GUILayout.Button("Reset", GUILayout.Width(40)))
											{
												Undo.RecordObject(myScript, "Reset Value to Zero");
												blendShapeData.sliderValue = 0;
												myScript.SetValue(o, i, i, 0);
											}

											if (GUILayout.Button("Min", GUILayout.Width(40)))
											{
												Undo.RecordObject(myScript, "Set Min Value");
												if (blendShapeData.sliderValue < blendShapeData.maxValue)
													blendShapeData.minValue = blendShapeData.sliderValue;
											}

											if (GUILayout.Button("Max", GUILayout.Width(40)))
											{
												Undo.RecordObject(myScript, "Set Max Value");
												if (blendShapeData.sliderValue > blendShapeData.minValue)
													blendShapeData.maxValue = blendShapeData.sliderValue;
											}

											if (blendShapeData.isPlus &&
											    myScript.GetMinusShapeObject(blendShapeData.name) != 999999)
											{
												int minusShapeObject =
													myScript.GetMinusShapeObject(blendShapeData.name);
												int minusShapeID = myScript.GetMinusShapeID(blendShapeData.name);
												SFB_BlendShapesManager.SFB_BlendShape minusShapeData =
													myScript.blendShapeObjects[minusShapeObject]
														.blendShapes[minusShapeID];
												Undo.RecordObject(myScript, "Change Blend Shape Slider Value");
												var newValue = EditorGUILayout.Slider(blendShapeData.sliderValue, -100,
													100);
												blendShapeData.sliderValue = newValue;
												minusShapeData.sliderValue = -newValue;
											}
											else
											{
												Undo.RecordObject(myScript, "Change Blend Shape Slider Value");
												blendShapeData.sliderValue =
													EditorGUILayout.Slider(blendShapeData.sliderValue, 0, 100);
											}



											Undo.RecordObject(myScript, "Toggle Preset Export");
											blendShapeData.presetExportValue =
												EditorGUILayout.Popup(blendShapeData.presetExportValue,
													presetExportOptions);

											EditorGUILayout.EndHorizontal();
											EditorGUILayout.BeginHorizontal();
											GUILayout.Label("Random Range");
											int minLimit = 0;
											int maxLimit = 100;
											if (blendShapeData.isPlus)
												minLimit = -100;
											if (GUILayout.Button("Randomize", GUILayout.Width(80)))
											{
												myScript.SFB_BS_SetRandom(blendShapeData);
											}

											blendShapeData.minValue =
												EditorGUILayout.IntField(Mathf.RoundToInt(blendShapeData.minValue),
													GUILayout.Width(40));

											EditorGUILayout.MinMaxSlider(ref blendShapeData.minValue,
												ref blendShapeData.maxValue, minLimit, maxLimit, GUILayout.Width(150));
											blendShapeData.maxValue =
												EditorGUILayout.IntField(Mathf.RoundToInt(blendShapeData.maxValue),
													GUILayout.Width(40));
											EditorGUILayout.EndHorizontal();
											if (blendShapeData.sliderValue != blendShapeData.value)
											{
												Undo.RecordObject(myScript, "Update Blendshape Value");
												myScript.SetValue(o, i, blendShapeData.id, blendShapeData.sliderValue);
												if (blendShapeData.isPlus &&
												    myScript.GetMinusShapeObject(blendShapeData.name) != 999999)
												{
													int minusShapeObject =
														myScript.GetMinusShapeObject(blendShapeData.name);
													int minusShapeID = myScript.GetMinusShapeID(blendShapeData.name);
													SFB_BlendShapesManager.SFB_BlendShape minusShapeData =
														myScript.blendShapeObjects[minusShapeObject]
															.blendShapes[minusShapeID];
													Undo.RecordObject(myScript, "Update Blendshape Value");
													myScript.SetValue(minusShapeObject, minusShapeID, minusShapeData.id,
														minusShapeData.sliderValue);
												}
											}

											EditorGUILayout.EndVertical();
										}
									}
								}
							}
						}
					}
				}
			}
		}
		GUI.color 			= Color.green;
		if(GUILayout.Button("Reload Blend Shapes"))				
		{
			myScript.ReloadBlendShapes();			
		}

		GUI.color 			= Color.white;
		if (myScript.showWireframe)
		{
			if(GUILayout.Button("Turn Off Wireframe"))				
				myScript.SFB_BS_ShowWireframe(false);			
		}
		else
		{
			if(GUILayout.Button("Turn Off Wireframe"))				
				myScript.SFB_BS_ShowWireframe(true);			
		}

		GUI.color 			= Color.red;
		if(GUILayout.Button("Reset Values"))				
		{
			myScript.SFB_BS_ResetAll();						
		}
		GUI.color 			= Color.white;

		Undo.RecordObject (myScript, "Show Data");
		myScript.showData = EditorGUILayout.Foldout(myScript.showData, "Internal Script Data");
		if (myScript.showData)
		{
			DrawDefaultInspector();								// Draw the normal inspector data
		}
	}
}









