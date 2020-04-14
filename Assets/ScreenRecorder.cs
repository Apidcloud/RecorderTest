using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

	void Start () 
	{
		Debug.Log ("Start called");
		
		// Set target frame rate (optional)
		
		Application.targetFrameRate = frameRate;
		
		// Prepare the data directory
		persistentDataPath = "./ScreenRecorder";

		Debug.Log ("Capturing to: " + persistentDataPath + "/");

		if (!System.IO.Directory.Exists(persistentDataPath))
		{
			System.IO.Directory.CreateDirectory(persistentDataPath);
		}
		
		// Prepare textures and initial values
		screenWidth = 1920;
		screenHeight = 1080;
		
		tempRenderTexture = new RenderTexture(screenWidth, screenHeight, 0);
		tempTexture2D = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
		RenderTexture.active = tempRenderTexture;
		GetComponent<Camera>().targetTexture = tempRenderTexture;

		frameQueue = new Queue<byte[]> ();
		
		frameNumber = 0;
		savingFrameNumber = 0;

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
		encoderThread.Start ();
		
		StartCoroutine(TakeScreenShot(GetComponent<Camera>()));
	}
	
	void OnDisable() 
	{
		// Reset target frame rate
		Application.targetFrameRate = -1;

		// Inform thread to terminate when finished processing frames
		terminateThreadWhenDone = true;
	}

	private bool saving = false;

	public IEnumerator TakeScreenShotSimple()
	{
		WaitForEndOfFrame waitForFrame = new WaitForEndOfFrame();

		while (true)
		{
			//Wait for frame
			yield return waitForFrame;

			if (frameNumber <= maxFrames && !saving)
			{
				saving = true;
				frameNumber++;

				tempTexture2D = new Texture2D(Screen.width, Screen.height);
				//Get Image from screen
				tempTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
				tempTexture2D.Apply();
				
				// Encode texture into PNG
				byte[] bytes = tempTexture2D.EncodeToPNG();

				// save in memory
				string filename = frameNumber + ".png";
				var path = "UnityHeadlessRenderingScreenshots" + "/" + filename;
				File.WriteAllBytes(path, bytes);

				saving = false;
			}
			else if (frameNumber > maxFrames) {
				break;
			}
			
		}
	}

	public IEnumerator TakeScreenShot(Camera camera)
	{
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

					frameNumber ++;

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
				this.enabled = false;
				break;
			}
		}
    }


	// example while running normally, but this method isn't called while in headless mode (batch mode)
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
				this.enabled = false;
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

				frameNumber ++;

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
			this.enabled = false;
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
				savingFrameNumber ++;
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
