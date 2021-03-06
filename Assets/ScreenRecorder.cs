﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

class BitmapEncoder
{
	public static void WriteBitmap(Stream stream, int width, int height, byte[] imageData)
	{
		using (BinaryWriter bw = new BinaryWriter(stream)) {

			// define the bitmap file header
			bw.Write ((UInt16)0x4D42); 								// bfType;
			bw.Write ((UInt32)(14 + 40 + (width * height * 4))); 	// bfSize;
			bw.Write ((UInt16)0);									// bfReserved1;
			bw.Write ((UInt16)0);									// bfReserved2;
			bw.Write ((UInt32)14 + 40);								// bfOffBits;
	 
			// define the bitmap information header
			bw.Write ((UInt32)40);  								// biSize;
			bw.Write ((Int32)width); 								// biWidth;
			bw.Write ((Int32)height); 								// biHeight;
			bw.Write ((UInt16)1);									// biPlanes;
			bw.Write ((UInt16)32);									// biBitCount;
			bw.Write ((UInt32)0);  									// biCompression;
			bw.Write ((UInt32)(width * height * 4));  				// biSizeImage;
			bw.Write ((Int32)0); 									// biXPelsPerMeter;
			bw.Write ((Int32)0); 									// biYPelsPerMeter;
			bw.Write ((UInt32)0);  									// biClrUsed;
			bw.Write ((UInt32)0);  									// biClrImportant;

			// switch the image data from RGB to BGR
			for (int imageIdx = 0; imageIdx < imageData.Length; imageIdx += 3) {
				bw.Write(imageData[imageIdx + 2]);
				bw.Write(imageData[imageIdx + 1]);
				bw.Write(imageData[imageIdx + 0]);
				bw.Write((byte)255);
			}
			
		}
	}

}

/// <summary>
/// Captures frames from a Unity camera in real time
/// and writes them to disk using a background thread.
/// </summary>
/// 
/// <description>
/// Maximises speed and quality by reading-back raw
/// texture data with no conversion and writing 
/// frames in uncompressed BMP format.
/// Created by Richard Copperwaite.
/// </description>
/// 
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(AudioRecorder))]
public class ScreenRecorder : MonoBehaviour 
{
	// Public Properties
	public int maxFrames; // maximum number of frames you want to record in one video
	public int frameRate = 30; // number of frames to capture per second

	// The Encoder Thread
	private Thread encoderThread;

	// Texture Readback Objects
	private RenderTexture tempRenderTexture;
	private Texture2D tempTexture2D;

	// Timing Data
	private float captureFrameTime;
	private float lastFrameTime;
	private int frameNumber;
	private int savingFrameNumber;

	// Encoder Thread Shared Resources
	private Queue<byte[]> frameQueue;
	private string persistentDataPath;
	private int screenWidth;
	private int screenHeight;
	private bool threadIsProcessing;
	private bool terminateThreadWhenDone;

	private AudioRecorder audioRecorder;

	private bool threadStarted = false;

	void Start () 
	{
		Debug.Log ("ScreenRecorder.Start called");
		
		#if UNITY_WEBGL
			Application.runInBackground = true;
			persistentDataPath = Application.persistentDataPath;
		#else
			// Set target frame rate (optional)
			Application.targetFrameRate = frameRate;
			persistentDataPath = "./ScreenRecorder";
		#endif
		
		Debug.Log ("Capturing to: " + persistentDataPath + "/");

		#if !UNITY_WEBGL
		if (!System.IO.Directory.Exists(persistentDataPath))
		{
			System.IO.Directory.CreateDirectory(persistentDataPath);
		}
		#endif

		// Prepare textures and initial values
		screenWidth = 400;
		screenHeight = 400;
		
		tempRenderTexture = new RenderTexture(screenWidth, screenHeight, 0);
		tempTexture2D = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
		//RenderTexture.active = tempRenderTexture;
		GetComponent<Camera>().targetTexture = tempRenderTexture;

		audioRecorder = GetComponent<AudioRecorder>();

		frameNumber = 0;
		savingFrameNumber = 0;

		#if !UNITY_WEBGL
			frameQueue = new Queue<byte[]> ();
		
			captureFrameTime = 1.0f / (float)frameRate;
			lastFrameTime = Time.time;

			// Kill the encoder thread if running from a previous execution
			if (encoderThread != null && (threadIsProcessing || encoderThread.IsAlive)) {
				threadIsProcessing = false;
				encoderThread.Join();
			}

			// Start a new encoder thread
			threadIsProcessing = true;
			encoderThread = new Thread (EncodeAndSave);
			encoderThread.Start();

			threadStarted = true;
			
			audioRecorder.StartWriting(persistentDataPath + "/audio_output.wav");
			StartCoroutine(TakeScreenShot(GetComponent<Camera>()));
		#else
			audioRecorder.StartWriting(persistentDataPath + "/audio_output.wav");
			StartCoroutine(TakeScreenShotSimple(GetComponent<Camera>()));
		#endif
	}

	void OnApplicationQuit()
    {
		// make sure to stop writing audio
		audioRecorder.StopWriting();
	}

	void OnDisable() 
	{
		// make sure to stop writing audio
		audioRecorder.StopWriting();
		// Reset target frame rate
		Application.targetFrameRate = -1;

		// Inform thread to terminate when finished processing frames
		terminateThreadWhenDone = true;
	}

	void StopMainTasks(){
		// make sure to stop writing audio
		audioRecorder.StopWriting();
		// Reset target frame rate
		Application.targetFrameRate = -1;

		// Inform thread to terminate when finished processing frames
		terminateThreadWhenDone = true;
	}

	void Update(){
		if (!threadIsProcessing && threadStarted){
			Debug.Log("Encoding thread has finished. Closing application.");
			threadStarted = false;
			Application.Quit();
		}
	}

	private bool saving = false;

	/*
	 * Simple screennshot example in the main thread
	 * Could be called as StartCoroutine(TakeScreenShotSimple())
	 */
	public IEnumerator TakeScreenShotSimple(Camera camera)
	{
		Debug.Log ("Screenshot Recording Single Thread -- STARTED");

		WaitForEndOfFrame waitForFrame = new WaitForEndOfFrame();

		while (true)
		{
			//Wait for frame
			yield return waitForFrame;

			if (frameNumber <= maxFrames && !saving)
			{
				saving = true;
				frameNumber++;

				tempRenderTexture = RenderTexture.active;
				RenderTexture.active = camera.targetTexture;
				camera.Render();

				//Debug.Log("CAMERA TARGET TEXTURE WIDTH: " + camera.targetTexture.width);
				//Debug.Log("CAMERA TARGET TEXTURE HEIGHT: " + camera.targetTexture.height);

				tempTexture2D = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
				tempTexture2D.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
				tempTexture2D.Apply();

				RenderTexture.active = tempRenderTexture;
				
				Color[] pix = tempTexture2D.GetPixels(100, 100, 1, 1);

				Debug.Log(pix[0].ToString());

				// Encode texture into JPG
				byte[] bytes = tempTexture2D.EncodeToJPG();

				Debug.Log("IMG " + frameNumber + " byte count: " + bytes.Length);

				var resultNumber = frameNumber.ToString().PadLeft(4, '0');

				// save in memory
				string filename = resultNumber + ".jpg";
				var path = persistentDataPath + "/" + filename;
				File.WriteAllBytes(path, bytes);

				Debug.Log ("Frame " + frameNumber);

				// don't wait for next frame to stop recording audio
				if (frameNumber >= maxFrames){
					audioRecorder.StopWriting();
				}

				saving = false;
			}
			else if (frameNumber > maxFrames && !saving) {
				// sync IndexedDB (browser)
				#if UNITY_WEBGL
					// Flush database in the web build
					Application.ExternalEval("_JS_FileSystem_Sync();");
					// Handle database
					Application.ExternalEval("handleDatabase();");
				#endif

				Debug.Log ("SCREENRECORDER FINISHED");

				break;
			}
		}
	}

	// Multithread example (tested in standalone builds)
	public IEnumerator TakeScreenShot(Camera camera)
	{
		Debug.Log ("Screenshot recording Multi-thread -- STARTED");
		WaitForEndOfFrame waitForFrame = new WaitForEndOfFrame();

		while (true)
		{
			//Wait for frame
			yield return waitForFrame;

			if (frameNumber <= maxFrames)
			{
				// Calculate number of video frames to produce from this game frame
				// Generate 'padding' frames if desired framerate is higher than actual framerate
				float thisFrameTime = Time.time;
				int framesToCapture = ((int)(thisFrameTime / captureFrameTime)) - ((int)(lastFrameTime / captureFrameTime));

				// Capture the frame
				if(framesToCapture > 0)
				{
					RenderTexture.active = camera.targetTexture;
					camera.Render();
					
					tempTexture2D = new Texture2D(camera.targetTexture.width, camera.targetTexture.height,
						TextureFormat.RGB24, false);
					tempTexture2D.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
					tempTexture2D.Apply();
					// SERVER: we could send the texture here to a server, which then renders all images to a video (e.g., mp4)
					RenderTexture.active = null;
				}
				// Add the required number of copies to the queue
				for(int i = 0; i < framesToCapture && frameNumber <= maxFrames; ++i)
				{
					frameQueue.Enqueue(tempTexture2D.GetRawTextureData());

					frameNumber++;

					if(frameNumber % frameRate == 0)
					{
						Debug.Log ("Frame " + frameNumber);
					}
				}

				// don't wait for next frame to stop recording audio
				if (frameNumber >= maxFrames){
					audioRecorder.StopWriting();
				}
				
				lastFrameTime = thisFrameTime;
			}
			else //keep making screenshots until it reaches the max frame amount
			{
				// Inform thread to terminate when finished processing frames
				terminateThreadWhenDone = true;

				// Disable script
				//this.enabled = false;
				StopMainTasks();
				break;
			}
		}
		
    }

	// Example while running the game normally, but this method isn't called while in headless mode (batch mode)
	// The actual overload method is OnRenderImage,
	// but we don't need it since we are running this through a coroutine instead.
	void OnRenderImageTest(RenderTexture source, RenderTexture destination)
	{
		return;
		Debug.Log("rendering...");
		if (frameNumber <= maxFrames)
		{
			// Check if render target size has changed, if so, terminate
			if(source.width != screenWidth || source.height != screenHeight)
			{
				threadIsProcessing = false;
				//this.enabled = false;
				StopMainTasks();
				throw new UnityException("Render target size has changed!");
			}

			// Calculate number of video frames to produce from this game frame
			// Generate 'padding' frames if desired framerate is higher than actual framerate
			float thisFrameTime = Time.time;
			int framesToCapture = ((int)(thisFrameTime / captureFrameTime)) - ((int)(lastFrameTime / captureFrameTime));

			// Capture the frame
			if(framesToCapture > 0)
			{
				Graphics.Blit (source, tempRenderTexture);
				
				RenderTexture.active = tempRenderTexture;
				tempTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height),0,0);
				RenderTexture.active = null;
			}

			// Add the required number of copies to the queue
			for(int i = 0; i < framesToCapture && frameNumber <= maxFrames; ++i)
			{
				frameQueue.Enqueue(tempTexture2D.GetRawTextureData());

				frameNumber++;

				if(frameNumber % frameRate == 0)
				{
					Debug.Log ("Frame " + frameNumber);
				}
			}
			
			lastFrameTime = thisFrameTime;

		}
		else //keep making screenshots until it reaches the max frame amount
		{
			// Inform thread to terminate when finished processing frames
			terminateThreadWhenDone = true;

			// Disable script
			//this.enabled = false;
			StopMainTasks();
		}

		// Passthrough
		Graphics.Blit (source, destination);
	}
	
	private void EncodeAndSave()
	{
		Debug.Log ("SCREENRECORDER IO THREAD STARTED");

		while (threadIsProcessing)
		{
			if(frameQueue.Count > 0)
			{
				var resultNumber = savingFrameNumber.ToString().PadLeft(4, '0');

				// Generate file path
				string path = persistentDataPath + "/frame" + resultNumber + ".bmp";

				// Dequeue the frame, encode it as a bitmap, and write it to the file
				using(FileStream fileStream = new FileStream(path, FileMode.Create))
				{
					BitmapEncoder.WriteBitmap(fileStream, screenWidth, screenHeight, frameQueue.Dequeue());
					fileStream.Close();
				}

				// Done
				savingFrameNumber++;
				Debug.Log ("Saved " + savingFrameNumber + " frames. " + frameQueue.Count + " frames remaining.");
			}
			else
			{
				if(terminateThreadWhenDone)
				{
					break;
				}

				Thread.Sleep(1);
			}
		}

		terminateThreadWhenDone = false;
		threadIsProcessing = false;

		Debug.Log ("SCREENRECORDER IO THREAD FINISHED");
	}
}
