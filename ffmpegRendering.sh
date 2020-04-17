#!/bin/sh
ffmpeg -nostdin -y -r 60 -f image2 -s 1920x1080 -i "./Build/ScreenRecorder/frame%04d.bmp" -i "./Build/ScreenRecorder/audio_output.wav" -shortest -vcodec libx264 -crf 25 -pix_fmt yuv420p "./Build/ScreenRecorder/_video.mp4"