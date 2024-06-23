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

**Tuple placed on queue**
```
datetime.utcnow(),
sound clip in the format that whisper wants (numpy array)
```

## Convert audio clips to text

https://github.com/SYSTRAN/faster-whisper

There are two instances of this receiving sound clips staggered by half length.  This is so words that get clipped mid snippet would be in the middle of the other process's stream

It uses whisper to translate into words

There is an attempt to ignore silence, but sometimes words get halucinated

**Tuple placed on queue**
*each item placed on queue is one word*
```
utc start,
utc end,
probability %,
word text
```

## Filter raw text results

## Output Endpoint