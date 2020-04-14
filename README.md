# Headless Unity Screen Recording

Headless mode is made possible for standalone build (i.e., windows, Mac, and Linux), through batch mode (`-batchMode`).

In this test repository, everything is done locally, but the textures/images could be sent to a server that is responsible for rendering them into video instead. Search for `SERVER` within `ScreenRecorder.cs` for that.

## Setup
### Build
The first step is to build the project to the desired target. You can either do it manually through the Editor (**File > Build Settings > Build**), or through cli, as described in the subsections below.

To make it easier to access, in **standalone** scenarios, in case you opt for the cli, the resulting file will be saved to an automatically created `Build` folder. **Don't forget to `cd` to the project folder first.**

In the **WebGl** build, however, the textures are saved into the persistent path, which is an **IndexedDB**.

#### WebGL
`<unity-executable-or-app-path> -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWeb -logFile buildLog.txt`

#### Windows x64
`"C:\Program Files\Unity\Editor\Unity.exe” -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWin64 -logFile buildLog.txt`

#### Windows x86
`"C:\Program Files\Unity\Editor\Unity.exe” -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWin -logFile buildLog.txt`

#### MacOS
`/Applications/Unity/Hub/Editor/2018.4.10f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildMac -logFile buildLog.txt`

#### Linux
Run the build resulting file as above, while setting `-executeMethod` to `BuildHelper.BuildLinux` or `BuildHelper.BuildLinux64` or `BuildHelper.BuildLinuxUniversal`

### Run Build in Batch Mode (Standalone)

The project is prepared to run in batch mode through a coroutine that will save the first 30 frames to .bmp files to **Build/ScreenRecorder**.

#### Windows
`./Build/AutomaticRecording.exe -batchMode -logFile batchLog.txt`

#### Mac
`sudo ./Build/AutomaticRecording.app/Contents/MacOS/AutomaticRecording -batchMode -logFile batchLog.txt`

### Run WebGl build

To run the build in the browser, you should do it through a server. For instance:
`python -m SimpleHTTPServer 8080`

This build

### Video Rendering

After getting the images, it is possible to render them to video (e.g., `.mp4`) through something like **ffpmeg**. Don't forget to `cd` to `Build/ScreenRecorder` before running the following command:

`ffmpeg -r 30 -f image2 -s 1920x1080 -i frame%04d.bmp -vcodec libx264 -crf 25  -pix_fmt yuv420p video.mp4`