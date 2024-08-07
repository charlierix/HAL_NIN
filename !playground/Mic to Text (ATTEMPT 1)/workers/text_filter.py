from datetime import datetime, timezone, timedelta
import time
from .models import TranscribedWord


# TODO: this is sending too frequently, dupes are in different windows



def filter_text_streams(queue_text1, queue_text2, queue_cancel, config):
    config_filter = config['text_filter']

    buffer = []     # list of models.TranscribedWord (record_start, transcribe_elapsed, word_start, word_stop, probability, word)

    log = open('log.json', 'w')
    log.write('{"actions": [')

    # TODO: May also want a small history of probabilities.  when in silence, there are some low probability halucinations, maybe one word
    # with high probability.  So when in that state, there should be a stronger set of high probabilities before going to just blindly trusting
    #
    # It might be good enough to be in silence or active state

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            log.write(']}')
            log.close()
            break

        # Pop any words from the queue into the buffer list
        while not queue_text1.empty():
            buffer.extend(queue_text1.get())

        # Figure out times
        now = datetime.now(timezone.utc)
        sendwindow_start = now - timedelta(seconds=config_filter['send_delay_max'])
        sendwindow_stop = now - timedelta(seconds=config_filter['send_delay_min'])

        words_in_window = get_words_in_window(buffer, sendwindow_start, sendwindow_stop, now, log)
        #send_words(words_in_window, queue_text2)

        log_window(log, words_in_window, sendwindow_start, sendwindow_stop, now)

        time.sleep(0.333)

# Scans buffer for words that are in or before the window
# If the word is completely before the time window (too far in the past), it is simply removed
# If the word is in the window, it is removed from the buffer and put in the return list
# TODO: may want to sort the return list
def get_words_in_window(buffer, sendwindow_start, sendwindow_stop, now, log):
    retVal = []

    index = len(buffer) - 1     # it looks like len() needs to iterate to get the number, also pop needs to shift things left (I could be mistaken).  So work right to left

    while index >= 0:
        #print('examining word: ' + buffer[index].word)

        if buffer[index].word_stop < sendwindow_start:
            # Too old, just remove and ignore it
            seconds = (sendwindow_start - buffer[index].word_stop).total_seconds()
            #print("too old (%.2fs).  word stop: %s | window start: %s" % (seconds, str(buffer[index].word_stop), str(sendwindow_start)))
            print('too old (%.2fs) | %s' % (seconds, buffer[index].word))

            log_removal(log, buffer[index], sendwindow_start, sendwindow_stop, now)
            buffer.pop(index)

        elif buffer[index].word_start <= sendwindow_stop:
            # In the window, move to return list
            word_display = buffer[index].word.upper() if buffer[index].word_probability >= 0.75 else buffer[index].word.lower()
            print('%s          (word in window | %s)' % (word_display, buffer[index].transcriber_name))
            retVal.append(buffer.pop(index))
            
        index -= 1

    return retVal

def send_words(words_in_window, queue):
    #pairs = get_pairs(words_in_window)
    print('finish this (send_words)')

def log_removal(log, word, sendwindow_start, sendwindow_stop, now):
    log.write('{"remove": {')

    log.write('"now": "' + str(now) + '",')
    log.write('"sendwindow_start": "' + str(sendwindow_start) + '",')
    log.write('"sendwindow_stop": "' + str(sendwindow_stop) + '",')

    add_word_to_log(log, word)

    log.write('},')

def log_window(log, words_in_window, sendwindow_start, sendwindow_stop, now):
    if len(words_in_window) == 0:
        return False
    
    log.write('{"window": [')

    for word in words_in_window:
        log.write('{')
        log.write('"now": "' + str(now) + '",')
        log.write('"sendwindow_start": "' + str(sendwindow_start) + '",')
        log.write('"sendwindow_stop": "' + str(sendwindow_stop) + '",')

        add_word_to_log(log, word)

        log.write('},')
    
    log.write(']},')

def add_word_to_log(log, word):
    log.write('"clip_time_start": "' + str(word.clip_time_start) + '",')
    log.write('"clip_time_stop": "' + str(word.clip_time_stop) + '",')
    log.write('"transcriber_name": "' + word.transcriber_name + '"')
    log.write('"transcribe_start": "' + str(word.transcribe_start) + '",')
    log.write('"transcribe_stop": "' + str(word.transcribe_stop) + '",')
    log.write('"word_start": "' + str(word.word_start) + '",')
    log.write('"word_stop": "' + str(word.word_stop) + '",')
    log.write('"word_probability": ' + str(word.word_probability) + ',')
    log.write('"word": "' + word.word + '"')