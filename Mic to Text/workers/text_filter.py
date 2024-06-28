from datetime import datetime, timezone, timedelta
import time
#import json

def filter_text_streams(queue_text1, queue_text2, queue_cancel, config):
    config_filter = config["text_filter"]

    buffer = []     # list of (start, stop, probability, word)

    log = open("log.json", "w")

    # TODO: May also want a small history of probabilities.  when in silence, there are some low probability halucinations, maybe one word
    # with high probability.  So when in that state, there should be a stronger set of high probabilities before going to just blindly trusting
    #
    # It might be good enough to be in silence or active state

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            log.close()
            break

        # Pop any words from the queue into the buffer list
        while not queue_text1.empty():
            buffer.extend(queue_text1.get())        # a list of words is pushed to the queue, store each individual word in buffer [word,word...]

        # Figure out times
        now = datetime.now(timezone.utc)
        sendwindow_start = now - timedelta(seconds=config_filter["send_delay_max"])
        sendwindow_stop = now - timedelta(seconds=config_filter["send_delay_min"])

        words_in_window = get_words_in_window(buffer, sendwindow_start, sendwindow_stop)
        #send_words(words_in_window, queue_text2)

        log_window(words_in_window)

        time.sleep(0.333)


# Scans buffer for words that are in or before the window
# If the word is completely before the time window (too far in the past), it is simply removed
# If the word is in the window, it is removed from the buffer and put in the return list
# TODO: may want to sort the return list
def get_words_in_window(buffer, sendwindow_start, sendwindow_stop):
    retVal = []

    index = len(buffer) - 1     # it looks like len() needs to iterate to get the number, also pop needs to shift things left (I could be mistaken).  So work right to left

    while index >= 0:
        print("examining word: " + str(buffer[index]))

        if buffer[index][1] < sendwindow_start:
            # Too old, just remove and ignore it
            print("!! removing word !! '" + buffer[index][3] + "'")
            buffer.pop(index)

        elif buffer[index][0] <= sendwindow_stop:
            # In the window, move to return list
            retVal.append(buffer.pop(index))
            
        index -= 1

    return retVal    


def send_words(words_in_window, queue):
    #pairs = get_pairs(words_in_window)
    print("finish this (send_words)")


def log_window(words_in_window):
    #json.dump({'grouped_items': grouped_results}, log_file, indent=4)
    print("finish this (log_window)")