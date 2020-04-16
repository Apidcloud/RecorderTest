using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.IO;

[RequireComponent(typeof(AudioListener))]
public class AudioRecorder : MonoBehaviour
{
    private int bufferSize;
    private int numBuffers;
    private int outputRate = 44100;
    private const string DEFAULT_FILENAME = "audio_output.wav";
    private int headerSize = 44; //default for uncompressed wav
    
    private bool recOutput = false;
    
    private FileStream fileStream;

    void Awake(){
        outputRate = AudioSettings.GetConfiguration().sampleRate;
        // the following is deprecated and causes the sound system to reset
        // essentially yielding no audio whatsover (unity 2018)
        //AudioSettings.outputSampleRate = outputRate;
    }

    // Start is called before the first frame update
    void Start()
    {
        // can be obtained through AudionSettings.GetConfiguration()
        //AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartWriting(string name = DEFAULT_FILENAME){
        Debug.Log("Started writing audio: " + name);

        fileStream = new FileStream(name, FileMode.Create);
        byte emptyByte = new byte();
    
        // preparing the header
        for(int i = 0; i < headerSize; i++) 
        {
            fileStream.WriteByte(emptyByte);
        }

        recOutput = true;
    }

    public void StopWriting(){
        if (recOutput){
            recOutput = false;
            WriteHeader();
            Debug.Log("Audio writing stopped");
        } else {
            Debug.Log("Audio writing already stopped");
        }
    }

    private void OnAudioFilterRead(float[] data, int channels){
        if (!recOutput){
            return;
        }

        // at this point, audio data is interlaced
        ConvertAndWrite(data);
    }

    private void ConvertAndWrite(float[] dataSource)
    {
        Debug.Log("Converting and writing audio data source");

        Int16[] intData = new Int16[dataSource.Length];
        // converting in 2 steps : float[] to Int16[], 
        // then Int16[] to Byte[]
    
        byte[] bytesData = new byte[dataSource.Length*2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.
    
        //to convert float to Int16
        int rescaleFactor = 32767; 
    
        for (int i = 0; i < dataSource.Length;i++)
        {
            intData[i] = (Int16)(dataSource[i] * rescaleFactor);
            byte[] byteArr = new byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i*2);
        }
    
        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteHeader()
    {
        fileStream.Seek(0, SeekOrigin.Begin);
   
        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff,0,4);
   
        byte[] chunkSize = BitConverter.GetBytes(fileStream.Length-8);
        fileStream.Write(chunkSize,0,4);
   
        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave,0,4);
   
        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt,0,4);
   
        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1,0,4);
    
        UInt16 two = 2;
        UInt16 one = 1;
    
        byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);
    
        byte[] numChannels = BitConverter.GetBytes(two);
        fileStream.Write(numChannels, 0, 2);
    
        byte[] sampleRate = BitConverter.GetBytes(outputRate);
        fileStream.Write(sampleRate, 0, 4);
    
        byte[] byteRate = BitConverter.GetBytes(outputRate*4);
        // sampleRate * bytesPerSample*number of channels, here 44100*2*2
    
        fileStream.Write(byteRate, 0, 4);
   
        UInt16 four = 4;
        byte[] blockAlign = BitConverter.GetBytes(four);
        fileStream.Write(blockAlign, 0, 2);
    
        UInt16 sixteen = 16;
        byte[] bitsPerSample = BitConverter.GetBytes(sixteen);
        fileStream.Write(bitsPerSample, 0, 2);
    
        byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);
    
        byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
        fileStream.Write(subChunk2, 0, 4);
    
        fileStream.Close();
    }
}