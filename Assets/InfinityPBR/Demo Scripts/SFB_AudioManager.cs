using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SFB_AudioManager : MonoBehaviour {

	public List<SFB_AudioClips> audioClips = new List<SFB_AudioClips>();
	public List<SFB_AudioClips> audioLoops = new List<SFB_AudioClips>();
	public bool pitchByTimescale = true;							// Adjust pitch by timescale?

	private AudioSource audioSource;								// AudioSource component
	private Animator animator;										// Animator component

	private bool pitchBySpeed = false;								// Adjust pitch by speed?
	private bool volumeBySpeed = false;								// Adjust volume by speed?
	private float pitchMin = 0.0f;									// Minimum pitch
	private float pitchMax = 1.0f;									// Maximum pitch
	private float desiredPitch = 1.0f;								// Current desired pitch
	private float volumeMin = 0.0f;									// Minimum volume
	private float volumeMax = 1.0f;									// Maximum volume
	private float volumeSpeed = 1.0f;								// Speed modifier for volume changes
	private float desiredMin = 0.0f;								// The goal minimum volume
	private float desiredMax = 1.0f;								// The goal maximium volume
	private bool loopPlaying = false;								// Is a loop currently playing?
	//private float minLoopVolume = 0.2f;								// Minimum volume for loop adjusted by speed
	//private float maxLoopVolume = 1.0f;								// Maximum volume for loop adjusted by speed

	[System.Serializable]
	public class SFB_AudioClips{
		public string name;											// Name of the group
		public AudioClip[] audioClips;								// Array of clips to randomly choose from
		public int chanceOfPlaying = 100;							// Chance that this sound will play when triggered
		public float volume = 1.0f;									// Volume to play at
		public bool volumeBySpeed = false;							// Adjust volume by animation speed?
		public float minVolume = 0.2f;								// Minimum volume (used for adjustments)
		public bool pitchBySpeed = false;							// Adjust pitch by speed?
		public float minPitch = 1.0f;								// Minimum pitch
		public float maxPitch = 1.0f;								// Maximum pitch
	}

	void Start(){
		if (!audioSource){											// If we haven't assigned this...
			audioSource = GetComponent<AudioSource> ();				// Cache the component
		}
		if (!animator){												// If we haven't assigned this...
			animator = GetComponent<Animator>();					// Cache the component
		}
	}

	void Update(){
		UpdatePitch ();												// Update Pitch method
		if (audioSource.isPlaying) {								// If the source is playing
			UpdateVolume ();										// Update volume method
		}
	}

	// Updates the pitch during runtime
	void UpdatePitch(){
		if (pitchBySpeed) {																						// If we are adjusting pitchBySpeed
			float pitchRangeNegative = 1.0f - pitchMin;															// Compute negative range
			float pitchRangePositive = pitchMax - 1.0f;															// compute positive range
			float locomotion = animator.GetFloat ("locomotion");												// Get the speed value
			if (locomotion > 0.0f) {																			// If we are moving forward
				desiredPitch = 1.0f + (pitchRangePositive * locomotion);										// set pitch
			} else if (locomotion < 0.0f) {																		// If we are moving backward
				desiredPitch = 1.0f + (pitchRangeNegative * locomotion);										// set pitch
			} else {																							// Otherwise
				desiredPitch = 1.0f;																			// default pitch
			}
		} else {																								// otherwise
			desiredPitch = 1.0f;																				// Default pitch
		}
		audioSource.pitch = Mathf.MoveTowards (audioSource.pitch, desiredPitch, Time.deltaTime);				// Adjust pitch over time
		if (pitchByTimescale) {																					// If we are adjusting by time
			audioSource.pitch = audioSource.pitch * Time.timeScale;												// Adjust pitch by timescale
		}
	}

	// Updates the volume during runtime
	void UpdateVolume(){
		if (volumeMin != desiredMin) {																			// If volumeMin isn't at desired
			volumeMin = Mathf.MoveTowards (volumeMin, desiredMin, volumeSpeed * Time.deltaTime);				// Move it closer to desired
		}
		if (volumeMax != desiredMax) {																			// If volumeMax isn't at desired
			volumeMax = Mathf.MoveTowards (volumeMax, desiredMax, volumeSpeed * Time.deltaTime);				// Move it closer to desired
		}
		audioSource.volume = volumeMax;																			// Set volume to maxVolume
		if (volumeBySpeed) {																					// If volume is being set by speed
			// Adjust volume to be between min and max based on locomotion float value from the animator
			audioSource.volume = Mathf.Clamp (audioSource.volume * Mathf.Abs (animator.GetFloat ("locomotion")), volumeMin, volumeMax);
		}
		if (audioSource.volume == 0.0f) {																		// If the volume is 0
			volumeBySpeed = false;																				// Set volumeBySpeed to false
			audioSource.Stop ();																				// Stop playing the sound
		}
	}

	// Will play an audioClip once
	void PlayAudio(string name){
		int index = AudioClipIndex (name);																		// Get the index of the named group
		if (Random.Range (0, 100) >= (100 - audioClips [index].chanceOfPlaying)) {								// Only if a random chance is positive
			float volume = audioClips [index].volume;															// grab the volume
			// Get an audioClip randomly
			AudioClip audioClip = audioClips [index].audioClips [Random.Range (0, audioClips [index].audioClips.Length)];
			if (audioClips [index].volumeBySpeed) {																// If we are adjusting volume by speed
				// Clamp the volume
				volume = Mathf.Clamp (volume * Mathf.Abs (animator.GetFloat ("locomotion")), audioClips [index].minVolume, audioClips [index].volume);
			}
			AudioSource.PlayClipAtPoint(audioClip, transform.position, volume);									// Play the clip at point
		}
	}

	// Call this to start the loop by name
	public void StartLoop(string name){
		if (!loopPlaying) {										// If a loop isn't already playing
			int index = AudioLoopIndex (name);					// Get the index of the named loop
			desiredMin = audioLoops [index].minVolume;			// Set desired minimum
			desiredMax = audioLoops [index].volume;				// Set desired Max
			//minLoopVolume = audioLoops [index].minVolume;		// Set minimum volume
			//maxLoopVolume = audioLoops [index].volume;			// Set maximum volume
			volumeBySpeed = audioLoops [index].volumeBySpeed;	// Set volumeBySpeed
			pitchBySpeed = audioLoops [index].pitchBySpeed;		// Set pitchBySpeed
			pitchMin = audioLoops [index].minPitch;				// Set minimum pitch
			pitchMax = audioLoops [index].maxPitch;				// Set maximum pitch
			// Choose a random audioClip from the list
			AudioClip audioClip = audioLoops [index].audioClips [Random.Range (0, audioLoops [index].audioClips.Length)];
			audioSource.clip = audioClip;						// Set the clip in the AudioSource
			audioSource.Play ();								// Play the clip
			loopPlaying = true;									// Set loopPlaying true
		}
	}

	// Call this to stop the loop (should fade out)
	public void StopLoop(){
		desiredMin = 0.0f;										// Turn volume down
		desiredMax = 0.0f;										// Turn volume down
		loopPlaying = false;									// Set loop playing false
	}

	// Returns the Index of a named group
	private int AudioClipIndex(string name){
		for (int i = 0; i < audioClips.Count; i++){										// For each audioClips group
			if (audioClips [i].name == name) {											// If the name matches
				return i;																// Return index
			}
		}
		Debug.LogError ("Did not find an audioClips[] entry named \"" + name + "\".");	// Send error to console
		return 0;																		// return 0
	}

	// Returns the Index of a named group
	private int AudioLoopIndex(string name){
		for (int i = 0; i < audioLoops.Count; i++){										// For each audioClips group
			if (audioLoops [i].name == name) {											// If the name matches
				return i;																// Return index
			}
		}
		Debug.LogError ("Did not find an audioLopos[] entry named \"" + name + "\".");	// Send error to console
		return 0;																		// return 0
	}
}
