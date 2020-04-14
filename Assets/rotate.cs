using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor.Recorder;

public class rotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    { /*
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var imageRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();

        imageRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        

        settings.AddRecorderSettings(imageRecorder);
        var recorderController = new RecorderController(settings);

        Debug.Log("Start Recording: " + imageRecorder.OutputFile);

        Debug.Assert(recorderController.StartRecording());
        Debug.Assert(recorderController.IsRecording()); */
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(10, 0, 10, Space.Self);
    }
}
