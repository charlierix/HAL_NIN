# https://github.com/SYSTRAN/faster-whisper

from datetime import datetime, timezone, timedelta
import time
from faster_whisper import WhisperModel
from .models import SoundClip, TranscribedWord

# Pops sound clips off queue_sound, translates, pushes sets of words onto queue_text
# queue_sound items:
#   models.SoundClip
#
# queue_text items:
#   [models.TranscribedWord]
def soundclips_to_text(queue_sound, queue_text, queue_cancel, config):
    config_translate = config['translate']

    model = WhisperModel(config_translate['model_size'], device=config_translate['device'], compute_type=config_translate['compute_type'])

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            break

        # Wait for another sound clip to pop off the queue
        if queue_sound.empty():
            print('........................transcriber is sleeping')
            time.sleep(0.333)
        else:
            clip = queue_sound.get()

            start = datetime.now(timezone.utc)
            words = transcribe_clip(model, clip.start_time, clip.stop_time, clip.clip)
            stop = datetime.now(timezone.utc)

            clip_len = (clip.stop_time - clip.start_time).total_seconds()
            trans_len = (stop - start).total_seconds()

            if trans_len > clip_len:
                print('Translation took longer than clip length.  Either use gpu or increase number of transcriber workers.  clip len: %.2fs, translation len: %.2fs' % (clip_len, trans_len))

            if len(words) > 0:
                queue_text.put(words)

def transcribe_clip(model, clip_time_start, clip_time_stop, clip):
    # NOTE: segments is a generator, so need to iterate to get the translation (can't set breakpoint and see anything until after iterating)

    # condition_on_previous_text: If True, the previous output of the model is provided
    #   as a prompt for the next window; disabling may make the text inconsistent across
    #   windows, but the model becomes less prone to getting stuck in a failure loop,
    #   such as repetition looping or timestamps going out of sync.
    #
    # this defaults to true, but trying with false to hopefully get cleaner translations

    start = datetime.now(timezone.utc)

    segments, _ = model.transcribe(clip, language='en', word_timestamps=True, condition_on_previous_text=False)

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

            retVal.append(TranscribedWord(
                clip_time_start,
                clip_time_stop,
                start,
                datetime.now(timezone.utc),
                clip_time_start + timedelta(seconds=word.start),
                clip_time_start + timedelta(seconds=word.end),
                word.probability,
                word.word))
            
    return retVal