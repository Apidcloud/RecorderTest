# Headless Unity Screen Recording

Headless mode is made possible for standalone build (i.e., windows, Mac, and Linux), through batch mode (`-batchMode`).

The web environment build (i.e., WebGL), though not really headless for now, is automated through `puppeteer`. Ideally, it will also be possible to make it work headlessly, but **there is still one last issue due to unity's rendering, as explained further below**.

In this test repository, everything is done locally, but the textures/images could be sent to a server that is responsible for rendering them into video instead. Search for `SERVER` within `Assets/ScreenRecorder.cs` for that.

## Setup
### Build
The first step is to build the project to the desired target. You can either do it manually through the Editor (**File > Build Settings > Build**), or through cli, as described in the subsections below.

To make it easier to access, in **standalone** scenarios, in case you opt for the cli, the resulting file will be saved to an automatically created `Build` folder. **Don't forget to `cd` to the project folder first.**

The **WebGl** build, in case you opt for the cli, will output it to `WebGl/Build`, and the textures will be saved into the persistent path, which is an **IndexedDB**.

**Before building the project to WebGL**, however, you should **disable** the `anti-aliasing` in unity's **quality settings**. If it is enabled, it will throw some OpenGL errors and the rendering will not work causing the output image to be black.

#### Windows x64
`$ "C:\Program Files\Unity\Editor\Unity.exe” -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWin64 -logFile buildLog.txt`

#### Windows x86
`$ "C:\Program Files\Unity\Editor\Unity.exe” -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWin -logFile buildLog.txt`

#### MacOS
`$ /Applications/Unity/Hub/Editor/2018.4.10f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildMac -logFile buildLog.txt`

#### Linux
Run the build resulting file as above, while setting `-executeMethod` to `BuildHelper.BuildLinux` or `BuildHelper.BuildLinux64` or `BuildHelper.BuildLinuxUniversal`

#### WebGL
`$ <unity-executable-or-app-path> -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWeb -logFile buildLog.txt`

### Run Build in Batch Mode (Standalone)

The project is prepared to run in batch mode through a coroutine that will save the first 30 frames to .bmp files to **Build/ScreenRecorder**.

#### Windows
`$ ./Build/AutomaticRecording.exe -batchMode -logFile batchLog.txt`

#### Mac
`$ sudo ./Build/AutomaticRecording.app/Contents/MacOS/AutomaticRecording -batchMode -logFile batchLog.txt`

### Run WebGl build

#### Extra setup
Before anything else, _if you rebuilt the WebGl build_, some extra code must be added to the `index.html` file within `WebGl/Build` folder.

At the end of html tag `head`, add a reference to a new script:

`<script src="../handleDatabase.js"></script>`

At the beginning of `body`, add a new html element:

`<img id="testImage">`

This script will run after 30 frames are stored into the IndexedDB, and will change the background (i.e., `testImage`) to the first captured image as a test, to see if everything is working.

#### Run build

To actually run the WebGL build in the browser, you will need a server. For instance:

`$ python -m SimpleHTTPServer 8080`

And open `http://localhost:8080/<path-to-WebGl/Build/>`

You should see something like below:

<img src="WebGl/screenshot_results/expectedWebGLResult.png" alt="expected webgl result" width="400"/>

**Note that the screen of the game window will be black, because we are only concerned about running the project in headless/batch mode.**

#### Run with Puppeteer

The idea of using something like Puppeteer is to run the browser automatically and in headless mode, while still executing the build and recording the images.

##### Basic Setup

1. Install [NodeJS](https://nodejs.org/en/) (10.x or higher)
2. Install Yarn globally with `$ npm i -g yarn` or download it at their [website](https://yarnpkg.com/en/docs/install)
3. `$ cd WebGl && yarn` to install the dependencies.
4. `$ yarn server` to run a local server
5. `$ yarn start` to run the WebGL build through puppeteer

##### Expected results

The example is taking 2 screenshots after running for a few seconds (`WebGl/ScreenshotResults`). `renderingResult.png` should be the same as `expectedWebGLResult.png`.

It is also possible to run the WebGL build in **no-gpu** scenarios (e.g., a server). To do so, just uncomment `--disable-gpu` within `WebGl/main.js`.

### Video Rendering

After getting the images (either locally or remotely), it is possible to render them to video (e.g., `.mp4`) through something like **ffpmeg**. 

An example of that, for standalone builds, is to `cd` to `Build/ScreenRecorder` and finally run:

`ffmpeg -r 30 -f image2 -s 1920x1080 -i frame%04d.bmp -vcodec libx264 -crf 25  -pix_fmt yuv420p video.mp4`