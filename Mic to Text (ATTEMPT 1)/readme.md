> # FLAW
> 
> For attempt 1, I had two transcribers sending to a single filter, but word times weren't lining up and a single transcriber seems to do a good job
>
> So this was just creating a lot of complexity and latency for no benefit (couldn't even get the word merging to work)
>
> Just keeping this project around in case some of it might be useful

# Stream Microphone to Text

https://www.youtube.com/watch?v=k6nIxWGdrS4

## transcribe.py

Main entry point.  Reads config, sets up processes and interprocess queues.  Waits for ctrl+c

## Stream microphone data

uses pyaudio

Records small snippets of audio and places them onto queue for processing

Since there's a chance that words will get chopped mid snippet, this creates two streams of audio clips that are offset by half length:

If the half length is 3 seconds, then one of the queues gets clips starting at time = 0, 6, 12, 18 ...
The other queue gets clips starting at time = 3, 9, 15, 21 ...

**Object placed on queue: models.SoundClip**
*all times are utc*
```
start time
stop time
sound clip in the format that whisper wants (numpy array)
```

## Convert audio clips to text

https://github.com/SYSTRAN/faster-whisper

There are two instances of this receiving sound clips staggered by half length.  This is so words that get clipped mid snippet would be in the middle of the other process's stream

It uses whisper to translate into words

There is an attempt to ignore silence, but sometimes words get halucinated

> CPU mode was taking longer than the length of the clip to translate.  Multiple instances didn't help, so it appears the only option is GPU, which requires these two to be installed
>
> https://developer.nvidia.com/cublas     (can just install cuda toolkit)
>
> https://developer.nvidia.com/cudnn      (need to download, unzip, copy dlls from bin into "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\ --version-- \bin")
>
> https://developer.nvidia.com/rdp/cudnn-archive        (latest is v9, but whisper wants v8 -- will probably change in the future)

**Object placed on queue: models.TranscribedWord**
*all times are utc, each item placed on queue is one word*
```
clip start time
clip stop time

transcribe start time
transcribe stop time

word start time
word stop time

probability %,
word text
```

## Filter raw text results

## Output Endpoint