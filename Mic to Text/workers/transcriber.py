# https://github.com/SYSTRAN/faster-whisper

import datetime
import time
from faster_whisper import WhisperModel

def soundclips_to_text(queue_sound, queue_text, queue_cancel, config):
    config_translate = config["translate"]

    model = WhisperModel(config_translate["model_size"], device=config_translate["device"], compute_type=config_translate["compute_type"])

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        # Wait for another sound clip to pop off the queue
        if queue_sound.empty():
            time.sleep(1)
        else:
            clip = queue_sound.get()
            words = transcribe_chunk(model, clip[0], clip[1])

            if words.count > 0:
                queue_text.put(words)


def transcribe_chunk(model, clip_time, clip):
    # NOTE: segments is a generator, so need to iterate to get the translation (can't set breakpoint and see anything until after iterating)

    # condition_on_previous_text: If True, the previous output of the model is provided
    #   as a prompt for the next window; disabling may make the text inconsistent across
    #   windows, but the model becomes less prone to getting stuck in a failure loop,
    #   such as repetition looping or timestamps going out of sync.
    #
    # this defaults to true, but trying with false to hopefully get cleaner translations

    segments, _ = model.transcribe(clip, language="en", word_timestamps=True, condition_on_previous_text=False)

    retVal = []

    for segment in segments:
        # print("-- segment --")
        # print("[%.2fs -> %.2fs] (avg_logprob: %.3f, no_speech_prob: %.3f) %s" % (segment.start, segment.end, segment.avg_logprob, segment.no_speech_prob, segment.text))

        # NOTE: this needs param: word_timestamps=True
        for word in segment.words:
            # print("-- word --")
            # print("[%.2fs -> %.2fs] (prob: %.3f) %s" % (word.start, word.end, word.probability, word.word))

            # It looks like .8 is a good threshold for keeping, maybe as low as .5
            # May also want to ignore a strong probability word if garbage word(s) are in front of it, only keep if there's more strong
            # probability word(s) following
            #
            # Leaving that decision for the function that has results from both streams

            retVal.append((
                clip_time + datetime.timedelta(seconds=word.start),
                clip_time + datetime.timedelta(seconds=word.end),
                word.probability,
                word.word))
            
    return retVal