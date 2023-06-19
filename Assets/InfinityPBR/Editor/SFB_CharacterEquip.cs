using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor;

public class SFB_CharacterEquip : MonoBehaviour {

    static GameObject target;                                                   // The target to match
    private static string rootBoneName = "BoneRoot";                            // Name of the root bone structure [IMPORTANT AND MAY BE DIFFERENT FOR YOUR MODEL!]
    private static SkinnedMeshRenderer targetRenderer;                          // Renderer we are targetting
    private static string subRootBoneName;                                      // Bone name
    private static GameObject thisBoneRoot;                                     // Current bone root

    [MenuItem("Window/Infinity PBR/Character Equip %#c")]                       // Provides a menu item & shortcut
    static void EquipCharacter()
    {
        if (Selection.activeGameObject)                                         // If there is an activeObject selected
        {
            Debug.Log("Starting Character Equip Workflow.");
            Dictionary<string, Transform> boneMap;                              // Holds all the bones
            boneMap = new Dictionary<string, Transform>();                      // Create the new dictionary
            target = Selection.activeGameObject;                                // Set the target to the selected object

            // Make sure there is a proper root bone structure
            bool isValidObject = false;                                         // Assume false
            foreach (Transform childCheck in target.transform)                  // For each child transform
            {
                if (childCheck.name == rootBoneName)                            // if this child name is the rootBoneName
                {
                    isValidObject = true;                                       // Make this a valid object
                }
                if (childCheck.GetComponent<SkinnedMeshRenderer>())               // If this has a skinnedMeshRenderer
                {
                    targetRenderer = childCheck.GetComponent<SkinnedMeshRenderer>();  // Set targetRenderer
                    Debug.Log("Set Target Renderer: " + targetRenderer.name);
                    foreach (Transform bone in targetRenderer.bones)            // For each bone...
                    {
                        boneMap[bone.name] = bone;                              // Add the bone to the dictionary
                    }
                }
            }

            if (isValidObject)                                                  // If the object is proper, we will find and equip the valid attached equipment.
            {
                foreach (Transform child in target.transform)                   // For each child of the target
                {
                    bool isEquipmentObject = false;                             // Assume it is not equipment
                    SkinnedMeshRenderer subChildRenderer = null;                // Renderer of the subChild

                    foreach (Transform subChild in child.transform)             // For each child of the child
                    {
                        if (subChild.name == rootBoneName)                      // If the subChild name is the rootBoneName
                        {
                            thisBoneRoot = subChild.gameObject;                 // This is the root
                            isEquipmentObject = true;                           // This is valid equipment
                        }
                        if (subChild.GetComponent<SkinnedMeshRenderer>())       // If the subChild has a skinnedMeshRenderer
                        {
                            // If the subChild has a SkinnedMeshRenderer, assign the renderer, bone name, and target;
                            Debug.Log("Name: " + subChild.gameObject.name);
                            subChildRenderer = subChild.gameObject.GetComponent<SkinnedMeshRenderer>();
                            Debug.Log("2: " + subChildRenderer.name);
                            subRootBoneName = subChildRenderer.rootBone.name;
                            Debug.Log("3: " + subRootBoneName);
                            targetRenderer = subChildRenderer;
                        }
                    }

                    if (isEquipmentObject && subChildRenderer)                  // If this is valid equipment and we have a subChildRenderer...
                    {
                        Debug.Log(child.name + " is valid equipment!");
                        Transform[] boneArray = subChildRenderer.bones;         // Set the boneArray to be all the bones from the subChildRenderer
                        for (int i = 0; i < boneArray.Length; i++)              // For each bone...
                        {
                            string boneName = boneArray[i].name;                // Get the bone name
                            if (boneMap.ContainsKey(boneName))                  // if the dictionary for the target bones contains this bone name...
                            {
                                boneArray[i] = boneMap[boneName];               // Set the array to match the bone from the Dictionary
                            }
                        }

                        subChildRenderer.bones = boneArray;                     // Assing the boneArray to the bones
                        foreach (Transform bone in targetRenderer.bones)        // For each bone...
                        {
                            if (bone.name == subRootBoneName)                   // Is the bone name the same as subRootBoneName?
                            {
                                subChildRenderer.rootBone = bone;               // Assign the root bone to this
                            }
                        }

                        PrefabUtility.UnpackPrefabInstance(child.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                        DestroyImmediate(thisBoneRoot, true);                   // Destroy the bones of the subChildRenderer
                    }
                }

                /* NOTE: In a previous version I had a prefab created automatically. I'm removing this as (1) it's not really
                 * hard at all to create a prefab by draggin, (2) I think there's little point to making lots of prefabs with
                 * different versions, and (3) one less thing to support...coding isn't my strong suit! :D
                 */

                // Finally, we will remove "Dummy001" from the objects.
                Transform[] allChildren = target.GetComponentsInChildren<Transform>();   // Get all children of the target
                foreach (Transform child in allChildren)                        // And for each one...
                {
                    if (child.gameObject.name == "Dummy001")                    // If the name is Dummy001
                    {
                        PrefabUtility.UnpackPrefabInstance(child.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                        DestroyImmediate(child.gameObject);                     // Destroy it
                    }
                }
            }
            Debug.Log("Character Equip Complete!!");
        }
        else
        {
            Debug.Log("No Object Selected!");
        }
    }
}