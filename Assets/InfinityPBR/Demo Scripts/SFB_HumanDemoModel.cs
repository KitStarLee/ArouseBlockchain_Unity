using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFB_HumanDemoModel : MonoBehaviour {

	public GameObject daggerBody;
	public GameObject daggerLeftHand;
	public GameObject daggerRightHand;

	public GameObject particleGeneric1;
	public GameObject positionGeneric1;

	public GameObject[] particlesGeneric2;
	public GameObject[] lightsGeneric2;

	public GameObject[] particlesGeneric3;
	public GameObject[] lightsGeneric3;

	public GameObject particleBarbarian1;
	public GameObject positionBarbarian1;

	public void TakeDaggerOut(string hand){
		daggerBody.SetActive (false);
		if (hand == "Left") {
			daggerLeftHand.SetActive(true);
			daggerRightHand.SetActive(false);
		} else if (hand == "Right") {
			daggerLeftHand.SetActive(false);
			daggerRightHand.SetActive(true);
		}
	}

	public void PutBackDagger(string hand){
		daggerBody.SetActive(true);
		daggerRightHand.SetActive(false);
		daggerLeftHand.SetActive(false);
	}

	public void StartCastGeneric1(){
		InvokeRepeating("CastGeneric1", 0.0f, 1.5f);
	}

	public void StopCastGeneric1(){
		CancelInvoke("CastGeneric1");
	}

	public void CastGeneric1(){
		GameObject newSpell = Instantiate(particleGeneric1, positionGeneric1.transform.position, Quaternion.identity);
		newSpell.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
		Destroy(newSpell, 5.0f);
	}

	public void StartCastGeneric2(){
		for (int l = 0; l < lightsGeneric2.Length; l++){
			lightsGeneric2[l].SetActive(true);
		}
		for (int p = 0; p < particlesGeneric2.Length; p++){
			particlesGeneric2[p].GetComponent<ParticleSystem>().Play();
		}
	}

	public void StopCastGeneric2(){
		for (int l = 0; l < lightsGeneric2.Length; l++){
			lightsGeneric2[l].SetActive(false);
		}
		for (int p = 0; p < particlesGeneric2.Length; p++){
			particlesGeneric2[p].GetComponent<ParticleSystem>().Stop();
		}
	}

	public void StartCastGeneric3(){
		for (int l = 0; l < lightsGeneric3.Length; l++){
			lightsGeneric3[l].SetActive(true);
		}
		for (int p = 0; p < particlesGeneric3.Length; p++){
			particlesGeneric3[p].GetComponent<ParticleSystem>().Play();
		}
	}

	public void StopCastGeneric3(){
		for (int l = 0; l < lightsGeneric3.Length; l++){
			lightsGeneric3[l].SetActive(false);
		}
		for (int p = 0; p < particlesGeneric3.Length; p++){
			particlesGeneric3[p].GetComponent<ParticleSystem>().Stop();
		}
	}

	public void StartCastBarbarian1(){
		InvokeRepeating("CastBarbarian1", 0.0f, 1.2f);
	}

	public void StopCastBarbarian1(){
		CancelInvoke("CastBarbarian1");
	}

	public void CastBarbarian1(){
		//GameObject newSpell = Instantiate(particleBarbarian1, positionBarbarian1.transform.position, positionBarbarian1.transform.rotation);
		//Destroy(newSpell, 5.0f);
		
		GameObject newSpell = Instantiate(particleBarbarian1, positionBarbarian1.transform.position, Quaternion.identity);
		newSpell.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
		Destroy (newSpell, 7.0f);
	}

}







