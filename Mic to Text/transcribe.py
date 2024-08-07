import datetime
import json5        # json5 supports json with comments
import keyboard
import multiprocessing
import os
import time

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
        if keyboard.read_key() == 'backspace':      # read_key is blocking this process
            print('quitting...')
            queue_cancel.put('stop it')     # aparently, items left in the queue will cause join() to never complete.  So each process will pop one item off the queue
            queue_cancel.put('stop it')

            while not queue_cancel.empty():
                time.sleep(0.15)

            break

    # Clean up
    while not queue_sound.empty():      # the queue needs to be empty or join never finishes (cancel_join_thread didn't work)
        queue_sound.get()

    print('joining mic_listener')
    mic_listener.join()
    
    print('joining transcriber')
    transcriber.join()

    print('finished')