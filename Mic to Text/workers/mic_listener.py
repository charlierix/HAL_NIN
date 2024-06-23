# https://www.youtube.com/watch?v=k6nIxWGdrS

import datetime
import numpy as np
import pyaudio

FORMAT = pyaudio.paInt16
RATE = 44100
CHUNK_LENGTH = 3        # seconds

# Keeps running until the cancel queue is populated
# Records small sound snippets from the mic and puts them on the queues
# A sample placed on a queue is CHUNK_LENGTH * 2
# queue_even gets items starting at time=0, time=0+CHUNK_LENGTH*2 ...
# queue_odd gets items starting at time=CHUNK_LENGTH, time=CHUNK_LENGTH+CHUNK_LENGTH*2 ...
def mic_to_soundclips(queue_even, queue_odd, queue_cancel):
    # how does it know that input is mic and not line in?  maybe a default?
    audio = pyaudio.PyAudio()
    stream = audio.open(format=FORMAT, channels=1, rate=RATE, input=True, frames_per_buffer=1024)

    chunk = b''
    prev_chunk = b''
    counter = 0

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        # Get a small snippet of sound from the mic
        prev_chunk = chunk
        chunk = record_chunk(stream)

        # Place on one of the two queues
        queue = queue_even if counter % 2 == 0 else queue_odd
        queue.put((datetime.utcnow(), prev_chunk + chunk))

        counter += 1
    
    stream.stop_stream()
    stream.close()
    audio.terminate()

# Records CHUNK_LENGTH seconds of audio into a list
def record_chunk(stream):
    all_data = b''

    for _ in range(0, int(RATE / 1024 * CHUNK_LENGTH)):
        data = stream.read(1024)
        all_data += data

    return np.frombuffer(all_data, np.int16).astype(np.float32) / 32768.0      # I'm guessing it's 32768 because format is pyaudio.paInt16.  I saw another example that was using 8 bit and they divided by 255