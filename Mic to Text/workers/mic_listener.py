# https://www.youtube.com/watch?v=k6nIxWGdrS4

from datetime import datetime, timezone
import numpy as np
import pyaudio
from .models import SoundClip

# Keeps running until the cancel queue is populated
# Records small sound snippets from the mic and puts them on the queues
# A sample placed on a queue is half_length * 2
# queue_even gets items starting at time=0, time=0+half_length*2 ...
# queue_odd gets items starting at time=half_length, time=half_length+half_length*2 ...
# Items placed on the queues are instances of models.SoundClip
def mic_to_soundclips(queue_even, queue_odd, queue_cancel, config):
    config_audio = config['audio']
    rate = config_audio['rate']
    half_length = config_audio['half_length']

    # how does it know that input is mic and not line in?  maybe a default?
    audio = pyaudio.PyAudio()
    stream = audio.open(format=pyaudio.paInt16, channels=1, rate=rate, input=True, frames_per_buffer=1024)

    clip = None
    prev_clip = None
    counter = 0

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        # Get a small snippet of sound from the mic
        prev_clip = clip
        clip = record_clip(stream, rate, half_length)

        # Place on one of the two queues
        queue = queue_even if counter % 2 == 0 else queue_odd
        queue.put(concat_clips(prev_clip, clip))     # models.SoundClip

        counter += 1
    
    stream.stop_stream()
    stream.close()
    audio.terminate()

# Records half_length seconds of audio into a list
def record_clip(stream, rate, half_length):
    all_data = b''
    start = datetime.now(timezone.utc)

    for _ in range(0, int(rate / 1024 * half_length)):
        data = stream.read(1024)
        all_data += data

    stop = datetime.now(timezone.utc)

    return SoundClip(start, stop, np.frombuffer(all_data, np.int16).astype(np.float32) / 32768.0)       # I'm guessing it's 32768 because format is pyaudio.paInt16.  I saw another example that was using 8 bit and they divided by 255

def concat_clips(clip1, clip2):
    if clip1 is None:
        return clip2
    
    elif clip2 is None:
        return clip1

    return SoundClip(clip1.start_time, clip2.stop_time, clip1.clip + clip2.clip)