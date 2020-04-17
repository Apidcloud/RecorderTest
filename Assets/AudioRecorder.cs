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

    //default for uncompressed wav
    private int headerSize = 44;
    
    private bool recOutput = false;
    
    //private FileStream fileStream;

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

    private List<byte> audioByteList = new List<byte>();

    private string desiredFilename = string.Empty;

    public void StartWriting(string name = DEFAULT_FILENAME){

        desiredFilename = name;

        Debug.Log("Started writing audio: " + desiredFilename);
       
        //fileStream = new FileStream(name, FileMode.Create);
        //byte emptyByte = new byte();
    
        // preparing the header
        for(int i = 0; i < headerSize; i++) 
        {
            audioByteList.Add(new byte());
            //fileStream.WriteByte(emptyByte);
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

        audioByteList.AddRange(bytesData);
    
        //fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private int seekOriginAndSetHeader(byte[] copyArr, int currentHeaderIndex){
        for (int i = 0; i < copyArr.Length; i++)
        {
            // make sure the current index is in a valid range
            if (currentHeaderIndex < audioByteList.Count) {
                audioByteList[currentHeaderIndex++] = copyArr[i];
            }
        }
        return currentHeaderIndex;
    }

    private void WriteHeader()
    {
        //using (fileStream)
        //{
            //fileStream.Seek(0, SeekOrigin.Begin);

            Debug.Log("AUDIO BYTE COUNT: " + audioByteList.Count);

            int headerCurrentByteIndex = 0;

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            //fileStream.Write(riff,0,4);
            headerCurrentByteIndex = seekOriginAndSetHeader(riff, headerCurrentByteIndex);

            //fileStream.Length-8 or audioByteList.Count-8
            byte[] chunkSize = BitConverter.GetBytes(audioByteList.Count - 8);
            //fileStream.Write(chunkSize,0,4);
            headerCurrentByteIndex = seekOriginAndSetHeader(chunkSize, headerCurrentByteIndex);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            //fileStream.Write(wave,0,4);
            headerCurrentByteIndex = seekOriginAndSetHeader(wave, headerCurrentByteIndex);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            //fileStream.Write(fmt,0,4);
            headerCurrentByteIndex = seekOriginAndSetHeader(fmt, headerCurrentByteIndex);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            //fileStream.Write(subChunk1,0,4);
            headerCurrentByteIndex = seekOriginAndSetHeader(subChunk1, headerCurrentByteIndex);
        
            UInt16 two = 2;
            UInt16 one = 1;
        
            byte[] audioFormat = BitConverter.GetBytes(one);
            //fileStream.Write(audioFormat, 0, 2);
            headerCurrentByteIndex = seekOriginAndSetHeader(audioFormat, headerCurrentByteIndex);
        
            byte[] numChannels = BitConverter.GetBytes(two);
            //fileStream.Write(numChannels, 0, 2);
            headerCurrentByteIndex = seekOriginAndSetHeader(numChannels, headerCurrentByteIndex);
        
            byte[] sampleRate = BitConverter.GetBytes(outputRate);
            //fileStream.Write(sampleRate, 0, 4);
            headerCurrentByteIndex = seekOriginAndSetHeader(sampleRate, headerCurrentByteIndex);

        
            byte[] byteRate = BitConverter.GetBytes(outputRate*4);
            // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            //fileStream.Write(byteRate, 0, 4);
            headerCurrentByteIndex = seekOriginAndSetHeader(byteRate, headerCurrentByteIndex);

            UInt16 four = 4;
            byte[] blockAlign = BitConverter.GetBytes(four);
            //fileStream.Write(blockAlign, 0, 2);
            headerCurrentByteIndex = seekOriginAndSetHeader(blockAlign, headerCurrentByteIndex);
        
            UInt16 sixteen = 16;
            byte[] bitsPerSample = BitConverter.GetBytes(sixteen);
            //fileStream.Write(bitsPerSample, 0, 2);
            headerCurrentByteIndex = seekOriginAndSetHeader(bitsPerSample, headerCurrentByteIndex);
        
            byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            //fileStream.Write(dataString, 0, 4);
            headerCurrentByteIndex = seekOriginAndSetHeader(dataString, headerCurrentByteIndex);
        
            // fileStream.Length - headerSize or audioByteList.Count - headerSize
            byte[] subChunk2 = BitConverter.GetBytes(audioByteList.Count - headerSize);
            //fileStream.Write(subChunk2, 0, 4);
            headerCurrentByteIndex = seekOriginAndSetHeader(subChunk2, headerCurrentByteIndex);

            // write all bytes at once at the end of the process, so that in webgl the key isn't retrieved
            // while we are still writing the audio data into the database
            File.WriteAllBytes(desiredFilename, audioByteList.ToArray());
            //fileStream.Close();
            //fileStream.Dispose();
        //}
    }
}