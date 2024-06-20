# import multiprocessing     # not sure if this is needed for calling functions off of queue
import time

def soundclips_to_text(queue_sound, queue_text, queue_cancel):
    print("soundclips_to_text")

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        # Wait for another sound clip to pop off the queue
        if queue_sound.empty():
            time.sleep(2)
        else:
            clip = queue_sound.get()
            
            print("TODO: process sound clip")