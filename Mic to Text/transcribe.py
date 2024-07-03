import datetime
import json5        # json5 supports json with comments
import keyboard
import multiprocessing
import os

from workers.mic_listener import mic_to_soundclips
from workers.transcriber import soundclips_to_text

def get_log_folder():
    retVal = 'logs/' + datetime.datetime.now().strftime('%Y-%m-%d %H-%M-%S')
    os.makedirs(retVal)
    return retVal

if __name__ == "__main__":
    print('Press backspace to quit...')

    with open('config.json', 'r') as f:
        config = json5.load(f)

    log_folder = None if not config['should_log'] else get_log_folder()

    # These queues are how the processes send messages between themselves
    queue_sound = multiprocessing.Queue()       # receives audio clips (models.SoundClip)
    #queue_text = multiprocessing.Queue()       # receives words (should be http post)
    queue_cancel = multiprocessing.Queue()

    # Kick off processes
    mic_listener = multiprocessing.Process(target=mic_to_soundclips, args=(queue_sound, queue_cancel, config, log_folder))
    transcriber = multiprocessing.Process(target=soundclips_to_text, args=(queue_sound, queue_cancel, config, log_folder))
    
    # TODO: instead of writing to queue_text, do http posts

    mic_listener.start()
    transcriber.start()
    
    # Wait for backspace key
    # NOTE: keyboard relies on root access
    while True:
        if keyboard.read_key() == 'backspace':
            queue_cancel.put('stop it')
            break

    # Clean up
    mic_listener.join()
    transcriber.join()

    print('finished')