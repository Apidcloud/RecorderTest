using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor.Recorder;

public class rotate : MonoBehaviour
{
    void Start()
    { /*
        // Unity Recorder example through code:
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var imageRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();

        imageRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        settings.AddRecorderSettings(imageRecorder);
        var recorderController = new RecorderController(settings);

        Debug.Log("Start Recording: " + imageRecorder.OutputFile);

        Debug.Assert(recorderController.StartRecording());
        Debug.Assert(recorderController.IsRecording()); */
    }

    void Update()
    {
        this.transform.Rotate(30 * Time.deltaTime, 0, 30 * Time.deltaTime, Space.Self);
    }
}
