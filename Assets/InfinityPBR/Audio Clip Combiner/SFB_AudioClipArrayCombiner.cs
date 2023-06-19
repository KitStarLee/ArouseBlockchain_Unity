using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


// Part of this script, basically the exporting math bits, were derived from Gregorio Zanon's script
// http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734
// https://gist.github.com/darktable/2317063
//
// The rest is provided by S.F. Bay Studios, Inc (http://www.InfinityPBR.com).
#if UNITY_EDITOR
public class SFB_AudioClipArrayCombiner : MonoBehaviour {
    [ContextMenuItem("Export Combined Audio Clips", "SaveNow")]
    public string outputName;
    public AudioLayer[] audioLayers;
	[HideInInspector]
	public bool displayInspector	= false;

    static float rescaleFactor = 32767; //to convert float to Int16

	const int HEADER_SIZE = 44;

	[System.Serializable]
	public class AudioLayer
	{
		public AudioClip[] clip;
		[HideInInspector]
		public bool expanded = false;
		[HideInInspector]
		public int clipNumber = 0;
		[HideInInspector]
		public string name = "No Clips Added";
		public float volume = 1;
		public float delay;
		[HideInInspector]
		public Int16[] samples;
		[HideInInspector]
		public Byte[] bytes;
		[HideInInspector]
		public int sampleCount;
		[HideInInspector]
		public int delayCount;
		[HideInInspector]
		public int onClip = 0;

		public void GetSamples(int clipNumber)
		{
			samples =  GetSamplesFromClip(clip[clipNumber], volume);
			delayCount = (int)(delay * clip[clipNumber].frequency * clip[clipNumber].channels);
			sampleCount = delayCount + samples.Length;
		}
	}

	public void NewLength(int newLength){
		AudioLayer[] newAudioLayers = new AudioLayer[newLength];					// Create a new array
		for (int i = 0; i < newLength; i++) {										// For each of the new length
			if (i >= audioLayers.Length)												// If we are beyond the length of the original
				break;																// break the loop
			newAudioLayers [i] = audioLayers [i];									// Copy the old data
		}
		audioLayers = new AudioLayer[newLength];									// Resize the array
		audioLayers	= newAudioLayers;												// Copy the data
	}

    public void SaveNow()
    {
		// Find total number of exports
		int totalExports	= 1;												// Start at 1...
		for(int n = 0; n < audioLayers.Length; n++)
		{
			totalExports 	*= audioLayers [n].clip.Length;						// Multiply by the number of clips in each layer
		}

		if (totalExports > 0) {
			float progressPercent	= 0.0f;
			int clipCount = 0;
			EditorUtility.DisplayProgressBar("Exporting Combined Audio Clips", "Clip " + clipCount + " of " + totalExports, progressPercent);
			string[] combinations;													// Start an array of all combinations
			combinations = new string[totalExports];								// Set the number of entries to the number of exports

			// Reset the onClip value for each layer
			for (int r = 0; r < audioLayers.Length; r++) {
				audioLayers [r].onClip = 0;
			}
				
			for (int l = 0; l < audioLayers.Length; l++) {							// For each layer...
				int exportsLeft = 1;												// Start at 1...
				for (int i = l; i < audioLayers.Length; i++) {							// For each layer left in the list (don't compute those we've already done)
					exportsLeft *= audioLayers [i].clip.Length;					// Find out how many exports are left if it were just those layers
				}

				int entriesPerValue = exportsLeft / audioLayers [l].clip.Length;	// Compute how many entires per value, if the total entries were exportsLeft
				int entryCount = 0;													// Set entryCount to 0

				for (int e = 0; e < combinations.Length; e++) {						// For all combinations
					if (l != 0)														// If this isn't the first layer
						combinations [e] = combinations [e] + ",";					// Append a "," to the String
					combinations [e] = combinations [e] + audioLayers [l].onClip;	// Append the "onClip" value to the string
					entryCount++;													// increase entryCount
					if (entryCount >= entriesPerValue) {							// if we've done all the entires for that "onClip" value...
						audioLayers [l].onClip++;									// increase onClip by 1
						entryCount = 0;												// Reset entryCount
						if (audioLayers [l].onClip >= audioLayers [l].clip.Length)	// if we've also run out of clips for this layer
							audioLayers [l].onClip = 0;								// Reset onClip count
					}
				}
			}

			int number = 0;															// for the file name
			// For each combination, save a .wav file with those clip numbers.
			foreach (var combination in combinations) {
				clipCount++;
				//progressPercent = clipCount / totalExports * 1.0f;
				progressPercent = clipCount / (float)totalExports;
				//Debug.Log ("progressPercent: " + progressPercent);
				EditorUtility.DisplayProgressBar("Exporting Combined Audio Clips", "Clip " + clipCount + " of " + totalExports, progressPercent);
				string[] clipsAsString	= combination.Split ("," [0]);
				SaveClip (outputName, number, clipsAsString, audioLayers);
				number++;
			}
		EditorUtility.ClearProgressBar();	
		} else {
			Debug.Log ("Nothing To Export! (or maybe a layer is missing clips?)");
		}
    }



	public static bool SaveClip(string filename, int exportNumber, string[] clipsAsString, AudioLayer[] audioLayers)
	{
		//Debug.Log ("Doing Export " + exportNumber);
		if (filename.Length <= 0)															// If the name hasn't been set
			filename = "CombinedAudio" + exportNumber;										// Use a default name
		else {																				// else
			filename = filename + "_" + exportNumber;										// Use the chosen name plus the number
		}
		filename += ".wav";																	// add the .wav extension

		var filepath	= "Assets/SFBayStudios/Exported Audio Files/" + filename;			// Set the file path

		// Make sure directory exists if user is saving to sub dir.
		Directory.CreateDirectory(Path.GetDirectoryName(filepath));

		using (var fileStream = CreateEmpty(filepath))										// Create an empty file
		{
			int sampleCount = ConvertAndWrite(fileStream, clipsAsString, audioLayers);

			//	 ClIP NUMBER CHANGE HERE
			WriteHeader(fileStream, audioLayers[0].clip[0], sampleCount);
		}
		AssetDatabase.ImportAsset(filepath);
		return true; // TODO: return false if there's a failure saving the file
	}

	static int ConvertAndWrite(FileStream fileStream, String[] clipsAsString, AudioLayer[] audioLayers)
    {
        int mostSamples = 0;																// Set this to 0
		//Debug.Log("audioLayers Lenght: " + audioLayers.Length);
		for (int c = 0; c < audioLayers.Length; c++) {										// For each Layer
			int clipNumber = int.Parse(clipsAsString[c]);									// Get the clip number as an int
			audioLayers[c].GetSamples(clipNumber);											// Run this function from the class
			mostSamples = Mathf.Max(mostSamples, audioLayers[c].sampleCount);				// Set mostSamples to the greatest one
		}


        
        Int16[] finalSamples = new Int16[mostSamples];										// The exported clip will have the mostSamples
		float[] sampleValues = new float[mostSamples];	

        for(int i = 0; i < mostSamples; i++)												// for each sample
        {
            float sampleValue = 0;															// Set variable for exported clip
            int sampleCount = 0;															// Set variable

            foreach (var audioLayer in audioLayers)											// For each layer....
            {
				if (i > audioLayer.delayCount && i < audioLayer.sampleCount)					// if we are not in the delay range and we are under the samplecount for the clip
                {
					// Add the value from this layer to the final (sampleValue)
                    sampleValue += (audioLayer.samples[i - audioLayer.delayCount] / rescaleFactor);
                    sampleCount++;
                }
            }
            
			sampleValues [i] += sampleValue;

			//if(sampleCount!=0)																// If we have done some samples (keep from dividing by 0)
            //    sampleValue /= sampleCount;													// compute sampleValue
           
        }


		float highSample = 0.0f;																			// Variable for the highest sample
		float lowSample = 0.0f;																				// Variable for the lowest sample
		for (int h = 0; h < mostSamples; h++) {																// For each sample...
			highSample = Mathf.Max (highSample, sampleValues [h]);											// Compute the highest sample
			lowSample = Mathf.Min (lowSample, sampleValues [h]);    										// Compute the lowest sample
		}
		float parameter = Mathf.InverseLerp(0.0f, Mathf.Max(highSample, lowSample * -1), 1.0f);				// Find the amount we need to multiply each sample by, based on the most extreme sample (high or low)

		for (int p = 0; p < mostSamples; p++) {																// For each sample...
			sampleValues [p] *= parameter;																	// Multiply the value by the parameter value															// Adjust the volume
		}

		for (int i2 = 0; i2 < mostSamples; i2++) {															// For each sample...
			finalSamples [i2] = (short)(sampleValues[i2] * rescaleFactor);									// Finalize the value
		}
		sampleValues = new float[0];																		// Clear this data














        Byte[] bytesData = ConvertSamplesToBytes(finalSamples);
        fileStream.Write(bytesData, 0, bytesData.Length);

        return mostSamples;
    }


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


    static Int16[] GetSamplesFromClip(AudioClip clip, float volume = 1)
    {
		//Debug.Log ("Getting Samples from clip " + clip.name);
        var samples = new float[clip.samples * clip.channels];
		//Debug.Log ("Samples: " + samples.Length);

        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        
		for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * volume * rescaleFactor);
        }
        return intData;
    }

    

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

        //UInt16 two = 2;
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

		//Byte[] subChunk2 = BitConverter.GetBytes(sampleCount * channelCount * 2);
        Byte[] subChunk2 = BitConverter.GetBytes(sampleCount * channelCount * 1);
        fileStream.Write(subChunk2, 0, 4);

        //		fileStream.Close();
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