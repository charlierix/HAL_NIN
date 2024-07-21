import numpy as np
import pyaudio
import time

# This runs in its own thread, looking for 'start' and 'stop' commands to come through on the command queue
# While in recording state, it streams from the mic
# Once told to stop, it puts the audio on the result queue
def mic_to_soundclips(queue_commands, queue_result, command_start, command_stop, config):
    config_audio = config['audio']
    rate = config_audio['rate']

    clips = None
    recording = False

    while True:
        if not queue_commands.empty():
            command = queue_commands.get()

            if command == command_start:
                print('Recording...')
                audio = pyaudio.PyAudio()
                format = pyaudio.paInt16
                stream = audio.open(format=format, channels=1, rate=rate, input=True, frames_per_buffer=1024)

                clips = []
                recording = True

            elif command == command_stop:
                print('Recording Stopped')
                recording = False
                
                stream.stop_stream()
                stream.close()
                audio.terminate()

                if len(clips) > 0:
                    clip = np.frombuffer(b''.join(clips), np.int16).astype(np.float32) / 32768.0       # I'm guessing it's 32768 because format is pyaudio.paInt16.  I saw another example that was using 8 bit and they divided by 255
                    queue_result.put(clip)
                else:
                    print('No audio was recorded')

            else:
                print(f'ERROR: Unknown command: {command}')

        elif recording:
            print('still recording...')
            clips.append(record_clip(stream, rate, 1))

        else:
            time.sleep(0.333)

# Records clip_length seconds of audio into a list
def record_clip(stream, rate, clip_length):
    retVal = b''

    for _ in range(0, int(rate / 1024 * clip_length)):
        retVal += stream.read(1024)

    return retVal