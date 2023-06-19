using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Reflection;
using UnityEngine.Audio;


// Part of this script, basically the exporting math bits, were derived from Gregorio Zanon's script
// http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734
// https://gist.github.com/darktable/2317063
//
// The rest is provided by S.F. Bay Studios, Inc (http://www.InfinityPBR.com).  Any Composer who wishes to sell their work to
// be used in Unity is granted a license to include these scripts in their package, so long as no code is changed and all
// notices such as these remain.

#if UNITY_EDITOR
// This is intended to export a group of clips as one combined .wav file, based on the volume settings in a Unity Audio Mixer.
// It requires some setup, but is otherwise straight forward.  Remember to include the SFB_AudioSnapshotExporterEditor.cs script
// in the "Editor" folder for a prettier inspector experience.
public class SFB_AudioSnapshotExporter : MonoBehaviour {
	public AudioMixer audioMixer;															// The AudioMixer we're exporting
	public float volumeAdjust = 1.0f;														// Adjustment for exported volume, 0.0-1.0
	[HideInInspector]																		
	public String audioMixerName;															// Name of the AudioMixer, used for export naming
	[HideInInspector]																		
	public bool displayInspector = false;													// Toggle value for whether to display default inspector or not
	[HideInInspector]																		
	public String outputName;																// Name for a single exported Snapshot
	[HideInInspector]																		
	public int onSnapshot;																	// Keeps track of which snapshot we're exporting
	[HideInInspector]																		
	public bool batchExporting 	= false;													// Toggle for when we are in the middle of a batch export process
	[HideInInspector]																		
	public int nextDelay	= 0;															// Delays 2 frames so Snapshot can load.  [There is likely a better way to do this!]
	public Int16[] finalSamples;															// Array that holds the final sample values for the exported .wav
	public AudioMixerSnapshot[] audioSnapshots;												// Array that holds the Snapshots attached to audioMixer
	public AudioMixerGroup[] audioGroups;													// Array that holds the Audio Groups attached to audioMixer
	public List<AudioClips> audioClips = new List<AudioClips>();							// List of the audio clips, obtained from the child objects of the object this script is attached to
	public List<string> exposedParams;														// List of exposed parameters
	public List<string>[] groupEffects;														// List of group effects.  [Eventually this could export effects / filters too, I'd think]

	static float rescaleFactor = 32767; 													// to convert float to Int16

	// This class holds our audioClip data
	[System.Serializable]																	// Save value between sessions
	public class AudioClips
	{
		public AudioClip clip;																// The audio clip we are using
		public int groupID;																	// The group ID the clip is attached to
		public float snapshotVolume;														// The volume settings for each snapshot (0.0 - 1.0)
		[HideInInspector]
		public Int16[] samples;																// will hold the sample data
		[HideInInspector]
		public Byte[] bytes;																// will hold the byte data
		[HideInInspector]
		public int sampleCount;																// Number of samples in the clip

		public AudioClips(AudioClip newClip, int newGroup)									// Function to add a new entry
		{
			clip = newClip;
			groupID = newGroup;
		}

		// We will only run this when the volume isn't basically zero.  This is because it does take up resources to populate these huge arrays,
		// and originally there were issues with crashing after the process was done.  Also because while trying to fix some "popping" issues,
		// it was suggested that CPU could have been to blame (it wasn't), so I attempted to keep the CPU from doing things that were pointelss.
		public void GetSamplesMusic()
		{
			if (snapshotVolume > 0.05f)														// If the volume isn't nothing
				samples = GetSamplesFromClip(clip, snapshotVolume);							// Get all the samples
			sampleCount = samples.Length;													// Assign the total number of samples
		}
	}

	// Will print out a list of all the sample counts
	[ContextMenu ("Sample Count Report")]
	public void SampleCountReport(){
		foreach (AudioClips audioClip in audioClips) {
			Debug.Log ("Clip: " + audioClip.clip.name + " has " + audioClip.clip.samples + " samples");
		}
	}

	// This function will set up the needed objects, so long as you've done the following:
	// 1. Create an Audio Mixer, titled the name of the song, in the same folder as all of the audioClips.  Name the clips “SongName_LayerName”
	// 2. Create a new group for each layer under “Master” in the Groups section of the Mixer, name these just "LayerName" (no SongName_ prefix!) (It makes it easier to read in the Mixer)
	// 3. Expose the Volume (Attenuation) value for each group (select group, right click "volume" in inspector)
	// 4. Rename each exposed parameter (top right of the mixer, click and then right-click to rename) the same as the clip name
	// 5. Create a new object in the scene, called “Song Title”, with the quotes if you’d like.
	// 6. Attach “SFB_AudioSnapshotExporter” to the new object.
	// 7. Assign the Audio Mixer to the script.
	// 8. Right click the title of the script and choose "Setup Children Etc"
	[ContextMenu ("Setup Children Etc")]													// Add an option in the context menu
	public void SetupChildren(){
		if (audioMixer) {
			String prefixName = audioMixer.name + "_";										// Setup the prefix name
			prefixName = prefixName.Replace ("\"", "");										// Remove any quotes
			prefixName = prefixName.Replace (" ", "");										// Remove spaces
			// Destroy all children of this object, to avoid having it write the same objects over and over
			var children = new List<GameObject>();											// Get a list of all child objects
			foreach (Transform child in transform) children.Add(child.gameObject);			// For each one...	
			children.ForEach(child => DestroyImmediate(child));								// Destroy it!
			String pathToMixer = AssetDatabase.GetAssetPath (audioMixer);					// Get the path to the mixer
			pathToMixer = pathToMixer.Replace ("/" + audioMixer.name + ".mixer", "");		// remove the name from the path

			string[] lookFor = new string[] {pathToMixer};									// Setup the paths to look for
			string[] audioFiles = AssetDatabase.FindAssets ("t:AudioClip", lookFor);		// Look for all audioclips at the paths

			foreach (string guiID in audioFiles)											// For each guiID we found
			{
				string clipName = Path.GetFileNameWithoutExtension (AssetDatabase.GUIDToAssetPath (guiID));	// Get clipName
				GameObject go = new GameObject(clipName);									// Create an object
				go.transform.parent = this.transform;										// Parent that under this
				AudioSource audioSource = go.AddComponent<AudioSource>();					// Add an audiosource component
				audioSource.loop = true;													// Loop is true
				// Assign the clip
				audioSource.clip = (AudioClip)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath (guiID), typeof(AudioClip));

				String groupName = clipName.Replace (prefixName, "");						// Setup groupName to search for
				// Get all audioMixerGroups matching clipName
				AudioMixerGroup[] audioMixerGroups = audioMixer.FindMatchingGroups (groupName);
				if (audioMixerGroups.Length == 1)											// If there is only one result...
					audioSource.outputAudioMixerGroup = audioMixerGroups [0];				// Assign it to our AudioSource
				else if (audioMixerGroups.Length > 1)										// If there is more than one result...
					Debug.LogError ("More than one AudioGroup was found with the name " + groupName + "!");
				else
					Debug.LogError ("Couldn't find an AudioMixerGroup for " + clipName + " -- are you sure you spelled it correctly?");
			}
		} else
			Debug.LogError ("Please populate the Audio Mixer first");

		ReloadData ();																		// Reload the data
	}

	// This will load the data we require.
	public void ReloadData(){
		audioGroups = audioMixer.FindMatchingGroups ("Master");								// Find the "Master" Audio Group
		groupEffects = new List<string>[audioGroups.Length];								// Set the size of this list

		// NOTE:  At this time, we don't actually do anything with the effects.  At some point it should be possible to include some or all
		// of the audio filter/effects in the export, but for now we just deal with volume.
		for (int x = 0; x < audioGroups.Length; x++) {
			groupEffects[x] = new List<string>();											
			Array effects = (Array)audioGroups[x].GetType().GetProperty("effects").GetValue(audioGroups[x], null);
			for(int i = 0; i< effects.Length; i++)
			{
				var o = effects.GetValue(i);
				string effect = (string)o.GetType().GetProperty("effectName").GetValue(o, null);
				groupEffects[x].Add(effect);
			}
		}

		LoadAudioClips();																	// Call this function


		// Load the parameters we have exposed
		Array parameters = (Array)audioMixer.GetType().GetProperty("exposedParameters").GetValue(audioMixer, null);
		exposedParams = new List<string>(new string[parameters.Length]);
		for(int i = 0; i< parameters.Length; i++)											// For each parameter...
		{
			var o = parameters.GetValue(i);													
			string Param = (string)o.GetType().GetField("name").GetValue(o);
			exposedParams[i] = Param;
		}

		// Load the snapshots attached to the Audio Mixer
		audioSnapshots = (AudioMixerSnapshot[])audioMixer.GetType().GetProperty("snapshots").GetValue(audioMixer, null);
	}

	// This will convert decibel values (-80 to 20 or so, usually with 0 being "normal" value) to a normalized float.
	private float DecibelToLinear(float dB)
	{
		float linear = Mathf.Pow(10.0f, dB/20.0f);											// Do Math.
		return linear;
	}

	// Will tell the audioMixer to switch to this snapshot, with no transition
	public void SwitchToSnapshot(int snapshot){
		audioSnapshots[snapshot].TransitionTo(0.0f);
	}

	// Starts the batch export process.
	public void StartBatch(){
		onSnapshot = 0;																		// Reset this value
		batchExporting = true;																// Toggle that we are batchExporting
		ExportBatchSnapshot ();																// Call the next export
	}

	// This will move on to the next snapshot, or finish the process if there are no more.
	public void NextSnapshot(){
		onSnapshot++;																		// Increase the count we are on.
		if (onSnapshot >= audioSnapshots.Length) {											// If we are beyond the number of Snapshots available...
			batchExporting = false;															// Toggle false batchExporting
			EditorUtility.ClearProgressBar();												// Clear the GUI Progress Bar
		}
		else {																				// Otherwise...
			ExportBatchSnapshot ();															// Call the next export
		}
	}

	public void ExportBatchSnapshot(){
		SwitchToSnapshot (onSnapshot);														// Switch to the current snapshot we want to export
		nextDelay = 2;																		// set the delay for 2 frames
	}

	// The only reason we have the Update loop is to allow for the script to pause while the Snapshot switches values, which only seems
	// to happen after the frame has finished.  There is like an un-exposed method of doing all this without pressing play, but at the
	// moment I do not know what it is.  If we could figure that out, we could set this up to batch export without being in play mode.
	void Update(){
		if (nextDelay > 0) {																// If nextDelay is greater than zero...
			nextDelay--;																	// Subtract 1
			if (nextDelay == 0)																// If nextDelay is zero...
				ExportSnapshots (true);														// Start the actual export process
		}
	}

	// This is one of the heavy-lifting methods.  It will do the acutal exporting.
	public void ExportSnapshots(bool allSnapshots){

		// This will grab the volume for each clip, based on the exposed variables in the AudioMixer.  The exposed variables must have
		// the same name as the audio clip objects that are children of the object this script is attached to.
		foreach (AudioClips audioClip in audioClips) {										// For each clip....
			float currentValue	= 1.0f;														// Varibale to hold value, assume its full volume
			if (audioMixer.GetFloat(audioClip.clip.name, out currentValue))					// If there is an exposed parameter matching the audioClip.clip.name...
				audioClip.snapshotVolume = DecibelToLinear(currentValue);					// Get the float volume level.
			if (audioClip.snapshotVolume > 1.0f)											// If the volume level is above 1.0...
				audioClip.snapshotVolume = 1.0f;											// Limit it to 1.0
		}

		// This section will set up the exported .wav filename & load a progress bar
		string fixedSnapshotName;																				// Set a variable
		if (allSnapshots)																						// If we are batch exporting all snapshots...
			fixedSnapshotName = audioSnapshots [onSnapshot].name.Replace (" (" + audioMixer.name + ")", "");	// Remove the " ([name])" from the snapshot name
		else																									// If we are only doing a single export...
			fixedSnapshotName = outputName;																		// Use the user-controlled output name instead
		string filename = audioMixer.name + "_" + fixedSnapshotName + ".wav";									// Set up filename
		filename = filename.Replace (" ", "");																	// Remove spaces
		string filepath	= "Assets/Exported Music/" + audioMixer.name + "/" + filename;				// Full Path & Name
		Directory.CreateDirectory (Path.GetDirectoryName (filepath));											// Create Directory if it doesn't exist
		if (allSnapshots) {																						// If we are batch exporting...
			float currentProgress = ((onSnapshot + 1) * 1.0f) / (audioSnapshots.Length * 1.0f);					// Get our progress percentage
			// Display a progress bar for the user, since this can take quite a few seconds to complete.
			EditorUtility.DisplayProgressBar ("Exporting From " + audioMixer.name, filename + " (" + (onSnapshot + 1) + " of " + audioSnapshots.Length + ")", currentProgress);
		}
		else 																									// If single export...
			EditorUtility.DisplayProgressBar ("Exporting From " + audioMixer.name, filename, 0.5f);				// Display progress bar but put the status at 50%

		using (var fileStream = CreateEmpty (filepath)) {														// Create an empty file
			// mostSamples is a holdover from the original script I had for combining audio clips of variable lengths.  Its left in becuase
			// an improvement on this script could be to include more time, delays or other features that may make the final .wav a different
			// length than the clips themselves.
			int mostSamples = 0;																				// Setup variable to know how many samples to use
			string mostSamplesName = "";																		// Setup variable
			foreach (AudioClips audioClip in audioClips) {														// For each audioClip...
				if (audioClip.snapshotVolume > 0.05) {															// If the volume isn't basically off
					audioClip.GetSamplesMusic ();																// Run this function from the class 
					mostSamples = Mathf.Max (mostSamples, audioClip.sampleCount);								// Set mostSamples to the greatest one
					if (mostSamples == audioClip.sampleCount){													// If this has the most samples...
						mostSamplesName = audioClip.clip.name;													// Set variable
					}
				}
			}

			// This chunk will load the combined sample value from all the clips into the "finalSamples" array
			finalSamples = new Int16[mostSamples];																// The exported clip will have the mostSamples
			float[] sampleValues = new float[mostSamples];														// Setup an array, length is mostSamples

			foreach (AudioClips audioClip in audioClips) {														// For each audio clip
				if (audioClip.snapshotVolume > 0.05) {															// If the volume is high enough
					//Debug.Log(audioClip.clip.name + " Samples: " + audioClip.sampleCount);
					if (audioClip.sampleCount == mostSamples) {
						for (int i = 0; i < mostSamples; i++) {													// For each sample
							sampleValues [i] = sampleValues [i] + (audioClip.samples [i] / rescaleFactor);  	// Add the sample value
						}
					} else {
						Debug.LogError (audioClip.clip.name + " has fewer samples (" + audioClip.sampleCount + ") (length) than the longest clip (" + mostSamplesName + ") (" + mostSamples + ").  All clips need to be the same length.");
					}
				}
				ClearData (audioClip);																			// Clear the data for this clip
			}

			// We need to normalize the value, to avoid issues.
			volumeAdjust = Mathf.Clamp(volumeAdjust, 0.0f, 1.0f);												// Make sure volumeAdjust is between 0.0 and 1.0
			float highSample = 0.0f;																			// Variable for the highest sample
			float lowSample = 0.0f;																				// Variable for the lowest sample
			for (int h = 0; h < mostSamples; h++) {																// For each sample...
				highSample = Mathf.Max (highSample, sampleValues [h]);											// Compute the highest sample
				lowSample = Mathf.Min (lowSample, sampleValues [h]);    										// Compute the lowest sample
			}
			float parameter = Mathf.InverseLerp(0.0f, Mathf.Max(highSample, lowSample * -1), 1.0f);				// Find the amount we need to multiply each sample by, based on the most extreme sample (high or low)

			for (int p = 0; p < mostSamples; p++) {																// For each sample...
				sampleValues [p] *= parameter;																	// Multiply the value by the parameter value
				sampleValues [p] *= volumeAdjust;																// Adjust the volume
			}

			for (int i2 = 0; i2 < mostSamples; i2++) {															// For each sample...
				finalSamples [i2] = (short)(sampleValues[i2] * rescaleFactor);									// Finalize the value
			}
			sampleValues = new float[0];																		// Clear this data

			Byte[] bytesData = ConvertSamplesToBytes (finalSamples);											// An array of bytes based on our samples
			fileStream.Write (bytesData, 0, bytesData.Length);													// Write the actual file
			WriteHeader (fileStream, audioClips [0].clip, mostSamples);											// Write the file header
		}

		finalSamples = new Int16[0];																			// Clear this data

		AssetDatabase.ImportAsset (filepath);																	// Have Unity import the new file

		if (!allSnapshots)																						// If we are just doing a single export...
			EditorUtility.ClearProgressBar();																	// Clear the progress bar
		ClearData(null);																						// Clear all data from audioClip (otherwise it can crash the editor)
		if (allSnapshots)																						// If we are batch exporting...
			NextSnapshot ();																					// Move on to the Next Snapshot
	}

	// This will clear these arrays.  Otherwise, I found that Unity would start hanging, since there are thousands and thousands of values for these.
	public void ClearData(AudioClips audioClip){						
		if (audioClip == null) {																				// If we haven't passed an audioClip...
			foreach (AudioClips audioClipToClear in audioClips) {												// Then for each audioClip...
				audioClipToClear.samples = new Int16[0];														// Empty the array
				audioClipToClear.bytes = new Byte[0];															// Empty the array
			}
		}
		else {																									// Otherwise...
			audioClip.samples = new Int16[0];																	// Empty the array
			audioClip.bytes = new Byte[0];																		// Empty the array
		}
	}

	// This will load all the audioClips into our system, and notify the user if there are errors.  For each audioClip we want to include...
	// 1) A game object with the same name as the clip & exposed volume parameter needs to be a child of the object this script is attached to
	// 2) There must be an AudioSource component attached...
	//   2a) The audioClip must be loaded and...
	//   2b) The Audio Group must also be populated
	// 3) An exposed parameter for the volume must have the same name as the audioClip
	public void LoadAudioClips(){
		audioClips.Clear();																						// Clear the list so that we don't keep writing more and more
		foreach (Transform child in transform)																	// For each child of this object...
		{
			AudioSource audioSource = child.gameObject.GetComponent<AudioSource>();								// Assign the AudioSource component
			if (!audioSource) {																					// If we don't have an AudioSource, throw an error message
				Debug.LogError ("The child object " + child.gameObject.name + " has no Audio Source component attached.  Please attach an Audio Source, and assign the Clip (AudioClip) and Ouput (AudioGroup) parameters, and make sure the object has the same name as the exposed volume paramter in the mixer.");
				continue;																						// End this for each iteration
			}
			AudioMixerGroup audioGroup = audioSource.outputAudioMixerGroup;										// Assign the Audio Group
			if (!audioGroup) {																					// if an Audio Group isn't found, throw an error message
				Debug.LogError ("The child object " + child.gameObject.name + " does not have an Audio Group assigned to it's \"Output\" parameter.");
				continue;																						// End this for each iteration
			}

			bool foundMatch = false;																			// Set up a true/false, assuming false
			for (int g = 0; g < audioGroups.Length; g++) {														// For each audioGroup...
				if (audioGroups [g].name == audioGroup.name) {													// If the name of the audio group in our array is the same as the current one we're looking at...
					audioClips.Add (new AudioClips (audioSource.clip, g));										// Add that clip to the list
					foundMatch = true;																			// Toggle foundMatch true
					break;																						// Break the loop
				}
			}

			if (!foundMatch)																					// If we found no match, inform the user with an error message
				Debug.LogError ("The child object " + child.gameObject.name + " could not find an Audio Group match.  Please make sure the Audio Group assigned to the Output parameter is part of the Audio Mixer you're working on.");
		}
	}

	// The rest of the code was done by the other developer referenced at the top of the script.  It's clear what some of it does,
	// and not so clear about others.  I suggest not changing any value unless you know what you're doing.
	const int HEADER_SIZE = 44;

	// This will convert the sample value to bytes
	static Byte[] ConvertSamplesToBytes(Int16[] samples)
	{
		Byte[] bytesData = new Byte[samples.Length * 2];
		for (int i = 0; i < samples.Length; i++)
		{
			Byte[] byteArr = new Byte[2];
			byteArr = BitConverter.GetBytes(samples[i]);
			byteArr.CopyTo(bytesData, i * 2);
		}
		return bytesData;
	}

	// This gets the actual samples from the audioClip and multiplies it by volume, which we have taken from the Snapshot data of the Audio Mixer.
	// If we eventually add filters/effects, chances are the logic for all that may go in here, or near here.
	static Int16[] GetSamplesFromClip(AudioClip clip, float volume)
	{
		var samples = new float[clip.samples * clip.channels];
		clip.GetData(samples, 0);

		Int16[] intData = new Int16[samples.Length];

		for (int i = 0; i < samples.Length; i++)
		{
			intData[i] = (short)(samples[i] * volume * rescaleFactor);
		}
		return intData;
	}

	// Andrews note:  I have no idea what this all is, aside from a file header (my assumption)
	static void WriteHeader(FileStream fileStream, AudioClip clip, int sampleCount)
	{
		var frequency = clip.frequency;
		var channelCount = clip.channels;

		fileStream.Seek(0, SeekOrigin.Begin);

		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		fileStream.Write(riff, 0, 4);

		Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
		fileStream.Write(chunkSize, 0, 4);

		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		fileStream.Write(wave, 0, 4);

		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		fileStream.Write(fmt, 0, 4);

		Byte[] subChunk1 = BitConverter.GetBytes(16);
		fileStream.Write(subChunk1, 0, 4);

		UInt16 one = 1;

		Byte[] audioFormat = BitConverter.GetBytes(one);
		fileStream.Write(audioFormat, 0, 2);

		Byte[] numChannels = BitConverter.GetBytes(channelCount);
		fileStream.Write(numChannels, 0, 2);

		Byte[] sampleRate = BitConverter.GetBytes(frequency);
		fileStream.Write(sampleRate, 0, 4);

		Byte[] byteRate = BitConverter.GetBytes(frequency * channelCount * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
		fileStream.Write(byteRate, 0, 4);

		UInt16 blockAlign = (ushort)(channelCount * 2);
		fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		fileStream.Write(bitsPerSample, 0, 2);

		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		fileStream.Write(datastring, 0, 4);

		Byte[] subChunk2 = BitConverter.GetBytes(sampleCount * channelCount * 1);
		fileStream.Write(subChunk2, 0, 4);
	}

	static FileStream CreateEmpty(string filepath)
	{
		var fileStream = new FileStream(filepath, FileMode.Create);
		byte emptyByte = new byte();

		for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
		{
			fileStream.WriteByte(emptyByte);
		}

		return fileStream;
	}
}
#endif