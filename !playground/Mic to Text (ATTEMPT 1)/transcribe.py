from datetime import timedelta
import json5        # json5 supports json with comments
import keyboard
import multiprocessing

from workers.mic_listener import mic_to_soundclips
from workers.transcriber import soundclips_to_text
from workers.text_filter import filter_text_streams

if __name__ == "__main__":
    print('Press backspace to quit...')

    with open('config.json', 'r') as f:
        config = json5.load(f)

    # These queues are how the processes send messages between themselves
    queue_even = multiprocessing.Queue()        # receives clips starting at time=0 (0-6, 6-12, 12-18 ...)
    queue_odd = multiprocessing.Queue()         # receives starting at time=3 (3-9, 9-15, 15-21 ...)
    queue_text1 = multiprocessing.Queue()       # receives from both transcriber processes
    queue_text2 = multiprocessing.Queue()       # receives from the the process that decides the final words
    queue_cancel = multiprocessing.Queue()

    # Kick off processes
    mic_listener = multiprocessing.Process(target=mic_to_soundclips, args=(queue_even, queue_odd, queue_cancel, config))
    transcriber_even = multiprocessing.Process(target=soundclips_to_text, args=(queue_even, queue_text1, 'even', timedelta(seconds=0), queue_cancel, config))
    transcriber_odd = multiprocessing.Process(target=soundclips_to_text, args=(queue_odd, queue_text1, 'odd', timedelta(seconds=config['audio']['half_length']), queue_cancel, config))
    text_filter = multiprocessing.Process(target=filter_text_streams, args=(queue_text1, queue_text2, queue_cancel, config))

    # TODO: instead of writing to queue_text2, do http puts

    mic_listener.start()
    transcriber_even.start()
    transcriber_odd.start()
    text_filter.start()

    # Wait for backspace key
    # NOTE: keyboard relies on root access
    while True:
        if keyboard.read_key() == 'backspace':
            queue_cancel.put('stop it')
            break

    # Clean up
    mic_listener.join()
    transcriber_even.join()
    transcriber_odd.join()
    text_filter.join()

    print('finished')