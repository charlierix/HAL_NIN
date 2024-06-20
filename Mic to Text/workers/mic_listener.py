# import multiprocessing
import time

def mic_to_soundclips(queue_even, queue_odd, queue_cancel):
    print("mic_to_soundclips")
    
    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        time.sleep(1)
