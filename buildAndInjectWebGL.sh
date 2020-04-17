#!/bin/sh

set -e

echo "You are currently on $OSTYPE"

if [[ "$OSTYPE" == "linux-gnu" ]]; then
    # linux gnu
    echo 'Detected linux gnu'
    echo 'But missing commands to build headlessly. Please add them to runWebGLBuild.sh win32 statement.'
elif [[ "$OSTYPE" == "darwin"* ]]; then
    # Mac OS
    echo 'Detected MacOS'
    echo 'Building for WebGL platform headlessly...'
    /Applications/Unity/Hub/Editor/2018.4.10f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath ./ -executeMethod BuildHelper.BuildWeb -logFile
    echo 'Injecting code onto WebGl/Build/index.html...'
    node WebGl/inject.js
elif [[ "$OSTYPE" == "cygwin" ]]; then
    # POSIX compatibility layer and Linux environment emulation for Windows
    echo 'Detected Windows'
    echo 'But missing commands to build headlessly. Please add them to runWebGLBuild.sh cygwin statement.'
elif [[ "$OSTYPE" == "msys" ]]; then
    # Lightweight shell and GNU utilities compiled for Windows (part of MinGW)
    echo 'Detected Windows'
    echo 'But missing commands to build headlessly. Please add them to runWebGLBuild.sh msys statement.'
elif [[ "$OSTYPE" == "win32" ]]; then
    # Maybe when in powershell
    echo 'Detected Windows'
    echo 'But missing commands to build headlessly. Please add them to runWebGLBuild.sh win32 statement.'
elif [[ "$OSTYPE" == "freebsd"* ]]; then
    # freebsd (servers)
    echo 'Detected freebsd'
    echo 'But missing commands to build headlessly. Please add them to runWebGLBuild.sh freebsd statement.'
else
    # Unknown.
    echo 'Unknown operating system. Please add ' + $OSTYPE ' statement to runWebGLBuild.sh and its commands accordingly.'
fi