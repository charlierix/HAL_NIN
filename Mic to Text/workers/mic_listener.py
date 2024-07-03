# https://www.youtube.com/watch?v=k6nIxWGdrS4

from datetime import datetime, timezone
import numpy as np
import pyaudio
from .models import SoundClip

# Keeps running until the cancel queue is populated
# Records small sound snippets from the mic and puts them on the queues
# Items placed on the queues are instances of models.SoundClip
def mic_to_soundclips(queue_sound, queue_cancel, config):
    config_audio = config['audio']
    rate = config_audio['rate']
    clip_length = config_audio['clip_length']

    # how does it know that input is mic and not line in?  maybe a default?
    audio = pyaudio.PyAudio()
    stream = audio.open(format=pyaudio.paInt16, channels=1, rate=rate, input=True, frames_per_buffer=1024)

    clip = None

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        clip = record_clip(stream, rate, clip_length)
        queue_sound.put(clip)     # models.SoundClip

    stream.stop_stream()
    stream.close()
    audio.terminate()

# Records clip_length seconds of audio into a list
def record_clip(stream, rate, clip_length):
    all_data = b''
    start = datetime.now(timezone.utc)

    for _ in range(0, int(rate / 1024 * clip_length)):
        data = stream.read(1024)
        all_data += data

    stop = datetime.now(timezone.utc)

    return SoundClip(start, stop, np.frombuffer(all_data, np.int16).astype(np.float32) / 32768.0)       # I'm guessing it's 32768 because format is pyaudio.paInt16.  I saw another example that was using 8 bit and they divided by 255