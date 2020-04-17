#!/bin/sh

set -e

echo "You are currently on $OSTYPE"


if [[ "$OSTYPE" == "linux-gnu" ]]; then
    # linux gnu
    echo 'Missing commands for linux gnu. Please add them to runBuildAndRender.sh corresponding statement.'
elif [[ "$OSTYPE" == "darwin"* ]]; then
    # Mac OS
    echo 'Detected MacOS'
    echo 'Running build headlessly...'
    sudo ./Build/AutomaticRecording.app/Contents/MacOS/AutomaticRecording -batchMode -logFile
    echo 'Rendering to video...'
    sudo bash ./ffmpegRendering.sh
elif [[ "$OSTYPE" == "cygwin" ]]; then
    # POSIX compatibility layer and Linux environment emulation for Windows
    echo 'Detected Windows'
    echo 'Running build headlessly...'
    ./Build/AutomaticRecording.exe -batchMode -logFile batchLog.txt
    echo 'Rendering to video...'
    bash ./ffmpegRendering.sh
elif [[ "$OSTYPE" == "msys" ]]; then
    # Lightweight shell and GNU utilities compiled for Windows (part of MinGW)
    echo 'Detected Windows'
    echo 'Running build headlessly...'
    ./Build/AutomaticRecording.exe -batchMode -logFile batchLog.txt
    echo 'Rendering to video...'
    bash ./ffmpegRendering.sh
elif [[ "$OSTYPE" == "win32" ]]; then
    # Maybe when in powershell
    echo 'Detected Windows'
    echo 'Running build headlessly...'
    ./Build/AutomaticRecording.exe -batchMode -logFile batchLog.txt
    echo 'Rendering to video...'
    bash ./ffmpegRendering.sh
elif [[ "$OSTYPE" == "freebsd"* ]]; then
    # freebsd (servers)
    echo 'Missing commands for freebsd. Please add them to runBuildAndRender.sh corresponding statement.'
else
    # Unknown.
    echo 'Unknown operating system. Please add ' + $OSTYPE ' statement to runBuildAndRender.sh and its commands accordingly.'
fi