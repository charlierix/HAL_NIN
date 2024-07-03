# https://www.youtube.com/watch?v=k6nIxWGdrS4

from datetime import datetime, timezone
import numpy as np
import pyaudio
import wave

from .models import SoundClip

# Keeps running until the cancel queue is populated
# Records small sound snippets from the mic and puts them on the queues
# Items placed on the queues are instances of models.SoundClip
def mic_to_soundclips(queue_sound, queue_cancel, config, log_folder):
    config_audio = config['audio']
    rate = config_audio['rate']
    clip_length = config_audio['clip_length']
    should_log = config['should_log']

    # how does it know that input is mic and not line in?  maybe a default?
    audio = pyaudio.PyAudio()
    format = pyaudio.paInt16
    stream = audio.open(format=format, channels=1, rate=rate, input=True, frames_per_buffer=1024)

    clip = None
    full_sound = b''

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        clip = record_clip(stream, rate, clip_length, should_log, full_sound)
        queue_sound.put(clip)     # models.SoundClip

    stream.stop_stream()
    stream.close()
    audio.terminate()

    if should_log:
        write_wav_file(log_folder, audio, format, rate, full_sound)

# Records clip_length seconds of audio into a list
def record_clip(stream, rate, clip_length, should_log, full_sound):
    all_data = b''
    start = datetime.now(timezone.utc)

    for _ in range(0, int(rate / 1024 * clip_length)):
        data = stream.read(1024)
        all_data += data

    stop = datetime.now(timezone.utc)

    if should_log:
        full_sound += all_data

    return SoundClip(start, stop, np.frombuffer(all_data, np.int16).astype(np.float32) / 32768.0)       # I'm guessing it's 32768 because format is pyaudio.paInt16.  I saw another example that was using 8 bit and they divided by 255

def write_wav_file(log_folder, audio, format, rate, full_sound):
    filename = log_folder + '/audio.wav'

    sound_file = wave.open(filename, 'wb')     # wb is write bytes mode
    sound_file.setnchannels(1)
    sound_file.setsampwidth(audio.get_sample_size(format))
    sound_file.setframerate(rate)
    sound_file.writeframes(full_sound)
    sound_file.close()