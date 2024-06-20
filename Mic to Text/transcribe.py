import keyboard
import multiprocessing

from workers.mic_listener import mic_to_soundclips
from workers.transcriber import soundclips_to_text

if __name__ == "__main__":
    print("Press backspace to quit...")

    # These queues are how the processes send messages between themselves
    queue_even = multiprocessing.Queue()        # receives clips starting at time=0 (0-6, 6-12, 12-18 ...)
    queue_odd = multiprocessing.Queue()         # receives starting at time=3 (3-9, 9-15, 15-21 ...)
    queue_text = multiprocessing.Queue()
    queue_cancel = multiprocessing.Queue()

    # Kick off processes
    mic_listener = multiprocessing.Process(target=mic_to_soundclips, args=(queue_even, queue_odd, queue_cancel))
    transcriber_even = multiprocessing.Process(target=soundclips_to_text, args=(queue_even, queue_text, queue_cancel))
    transcriber_odd = multiprocessing.Process(target=soundclips_to_text, args=(queue_odd, queue_text, queue_cancel))

    mic_listener.start()
    transcriber_even.start()
    transcriber_odd.start()

    # Wait for backspace key
    # NOTE: keyboard relies on root access
    while True:
        if keyboard.read_key() == "backspace":
            queue_cancel.put("stop it")
            break

    # Clean up
    mic_listener.join()
    transcriber_even.join()
    transcriber_odd.join()

    print("finished")