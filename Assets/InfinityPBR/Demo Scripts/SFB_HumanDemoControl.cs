
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFB_HumanDemoControl : MonoBehaviour {

	public Animator[] animators;					// Human animators
	[SerializeField] private Transform cameraFollow;
	[SerializeField] private Transform male;
	[SerializeField] private Transform female;

	public Material[] maleBody;
	public Material[] maleHead;
	public Material[] femaleBody;
	public Material[] femaleHead;

	public SkinnedMeshRenderer maleRenderer;
	public SkinnedMeshRenderer femaleRenderer;

	public Color[] hairColors;
	public Material hairShader;
	public int hairColor = 0;
	public int wardrobeIndex = 0;

	public Button[] hairButtonsMale;
	public Button[] hairButtonsFemale;

	public List<DemoWardrobe> wardrobe = new List<DemoWardrobe>();

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			SuperRandom();
		}
	}
	
	public void SetRightHandWeight(float newValue){
		for (int i = 0; i < animators.Length; i++) {
			animators [i].SetLayerWeight (3, newValue);
		}
	}

	public void SetLeftHandWeight(float newValue){
		for (int i = 0; i < animators.Length; i++) {
			animators [i].SetLayerWeight (2, newValue);
		}
	}

	public void SetLocomotion(float newValue){
		for (int i = 0; i < animators.Length; i++) {
			animators [i].SetFloat ("locomotion", newValue);
		}
	}

	public void TriggerAnimation(string newTrigger){
		for (int i = 0; i < animators.Length; i++) {
			animators [i].SetTrigger (newTrigger);
		}
	}

	public void FollowMale()
	{
		Follow(male);
	}

	public void FollowFemale()
	{
		Follow(female);
	}

	public void SuperRandom()
	{
		UpdateBodyTextures(Random.Range(0, 6));
		UpdateHairColor(Random.Range(0, hairColors.Length));
		UpdateWardrobe(Random.Range(0,wardrobe.Count));
		UpdateHairStyleMale(Random.Range(0, hairButtonsMale.Length));
		UpdateHairStyleFemale(Random.Range(0, hairButtonsFemale.Length));
	}

	public void UpdateHairStyleMale(int i)
	{
		hairButtonsMale[i].onClick.Invoke();
	}
	
	public void UpdateHairStyleFemale(int i)
	{
		hairButtonsFemale[i].onClick.Invoke();
	}
	
	public void Follow(Transform target)
	{
		cameraFollow.parent = target;
		cameraFollow.localPosition = Vector3.zero;
		cameraFollow.localRotation = target.rotation;
	}

	public void NextHair()
	{
		UpdateHairColor(hairColor + 1);
	}
	
	public void PrevHair()
	{
		UpdateHairColor(hairColor - 1);
	}
	
	public void NextWardrobe()
	{
		UpdateWardrobe(wardrobeIndex + 1);
	}
	
	public void PrevWardrobe()
	{
		UpdateWardrobe(wardrobeIndex - 1);
	}
	
	public void UpdateWardrobe(int i)
	{
		if (i >= wardrobe.Count)
		{
			i = 0;
		}

		if (i < 0)
		{
			i = wardrobe.Count - 1;
		}

		wardrobeIndex = i;

		for (int w = 0; w < wardrobe[wardrobeIndex].wardrobeObjects.Count; w++)
		{
			wardrobe[wardrobeIndex].wardrobeObjects[w].wardrobe
				.SetActive(wardrobe[wardrobeIndex].wardrobeObjects[w].active);
		}
	}

	public void UpdateHairColor(int i)
	{
		if (i >= hairColors.Length)
		{
			i = 0;
		}

		if (i < 0)
		{
			i = hairColors.Length - 1;
		}

		hairColor = i;
		
		hairShader.SetColor("_Color", hairColors[hairColor]);
		hairShader.SetColor("_HighlightColor", hairColors[hairColor]);
		hairShader.SetColor("_SecondaryHighlightColor", hairColors[hairColor]);
		hairShader.SetColor("_HairVariationColor", hairColors[hairColor]);
	}

	public void UpdateBodyTextures(int i = 0)
	{
		
		Material[] maleMaterials = maleRenderer.materials;
		Material[] femaleMaterials = femaleRenderer.materials;
		maleMaterials[0] = maleBody[i];
		maleMaterials[1] = maleHead[i];
		femaleMaterials[0] = femaleBody[i];
		femaleMaterials[1] = femaleHead[i];
		maleRenderer.materials = maleMaterials;
		femaleRenderer.materials = femaleMaterials;
	}
}
