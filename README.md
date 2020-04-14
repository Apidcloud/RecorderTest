# Headless Unity Screen Recording

Headless mode is made possible for standalone build (i.e., windows, Mac, and Linux), through batch mode (`-batchMode`).

In this test repository, everything is done locally, but the textures/images could be sent to a server that is responsible for rendering them into video instead. Search for `SERVER` within `ScreenRecorder.cs` for that.

## Setup
### Build
The first step is to build the project to the desired target. You can either do it manually through the Editor (**File > Build Settings > Build**), or through cli, as described in the subsections below.

To make it easier to access, in **standalone** scenarios, in case you opt for the cli, the resulting file will be saved to an automatically created `Build` folder. **Don't forget to `cd` to the project folder first.**

The **WebGl** build, in case you opt for the cli, will output it to `WebGl/Build`, and the textures will be saved into the persistent path, which is an **IndexedDB**.

#### Windows x64
`"C:\Program Files\Unity\Editor\Unity.exe” -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWin64 -logFile buildLog.txt`

#### Windows x86
`"C:\Program Files\Unity\Editor\Unity.exe” -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWin -logFile buildLog.txt`

#### MacOS
`/Applications/Unity/Hub/Editor/2018.4.10f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildMac -logFile buildLog.txt`

#### Linux
Run the build resulting file as above, while setting `-executeMethod` to `BuildHelper.BuildLinux` or `BuildHelper.BuildLinux64` or `BuildHelper.BuildLinuxUniversal`

#### WebGL
`<unity-executable-or-app-path> -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWeb -logFile buildLog.txt`

### Run Build in Batch Mode (Standalone)

The project is prepared to run in batch mode through a coroutine that will save the first 30 frames to .bmp files to **Build/ScreenRecorder**.

#### Windows
`./Build/AutomaticRecording.exe -batchMode -logFile batchLog.txt`

#### Mac
`sudo ./Build/AutomaticRecording.app/Contents/MacOS/AutomaticRecording -batchMode -logFile batchLog.txt`

### Run WebGl build

#### Extra setup
Before anything else, _if you rebuilt the WebGl build_, some extra code must be added to the `index.html` file within `WebGl/Build` folder.

At the end of html tag `head`, add a reference to a new script:

`<script src="../handleDatabase.js"></script>`

At the beginning of `body`, add a new html element:

`<img id="testImage">`

This script will run after 30 frames are stored into the IndexedDB, and will change the background (i.e., `testImage`) to the first captured image as a test, to see if everything is working.

#### Run build

**Note that the screen of the game will be black, because we are only concerned about running the project in headless/batch mode.**

To actually run the build in the browser, you should do it through a local server, be it through Apache or python. For instance:

`python -m SimpleHTTPServer 8080`

And open `http://localhost:8080/WebGl/Build/`

### Video Rendering

After getting the images, it is possible to render them to video (e.g., `.mp4`) through something like **ffpmeg**. 

An example of that, for standalone builds, is to `cd` to `Build/ScreenRecorder` and finally run:

`ffmpeg -r 30 -f image2 -s 1920x1080 -i frame%04d.bmp -vcodec libx264 -crf 25  -pix_fmt yuv420p video.mp4`