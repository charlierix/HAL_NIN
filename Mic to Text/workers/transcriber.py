# https://github.com/SYSTRAN/faster-whisper

from datetime import datetime, timezone, timedelta
import json
import time
from faster_whisper import WhisperModel
from .models import SoundClip, TranscribedWord
from .send_httppost import send_http

# Pops sound clips off queue_sound, translates, pushes sets of words onto queue_text
# queue_sound items:
#   models.SoundClip
#
# queue_text items:
#   [models.TranscribedWord]
def soundclips_to_text(queue_sound, queue_cancel, config, log_folder):
    config_translate = config['translate']
    language = config_translate['language']
    if language == '':
        language = None
    condition_on_previous_text = config_translate['condition_on_previous_text']
    should_log = config['should_log']

    model = WhisperModel(config_translate['model_size'], device=config_translate['device'], compute_type=config_translate['compute_type'])

    all_words = []

    while True:
        # See if the process should stop
        if not queue_cancel.empty():
            queue_cancel.get()
            break

        # Wait for another sound clip to pop off the queue
        if queue_sound.empty():
            time.sleep(0.333)
        else:
            clip = queue_sound.get()

            start = datetime.now(timezone.utc)
            words = transcribe_clip(model, clip.start_time, clip.stop_time, clip.clip, language, condition_on_previous_text)
            stop = datetime.now(timezone.utc)

            clip_len = (clip.stop_time - clip.start_time).total_seconds()
            trans_len = (stop - start).total_seconds()

            if trans_len > clip_len:
                print('Translation took longer than clip length.  Either use gpu or increase number of transcriber workers.  clip len: %.2fs, translation len: %.2fs' % (clip_len, trans_len))

            if len(words) > 0:
                send_http(words, config)

                if should_log:
                    all_words.extend(words)

    if should_log:
        write_log_file(all_words, log_folder)

def transcribe_clip(model, clip_time_start, clip_time_stop, clip, language, condition_on_previous_text):
    # NOTE: segments is a generator, so need to iterate to get the translation (can't set breakpoint and see anything until after iterating)

    start = datetime.now(timezone.utc)

    segments, _ = model.transcribe(clip, language=language, word_timestamps=True, condition_on_previous_text=condition_on_previous_text)

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
            
    retVal = filter_words(retVal)
    return retVal

def filter_words(words):
    # For some reason, every time it gets silence, it halucinates a low probability ' Thank' and high probability ' you.'
    if len(words) == 2:
        if words[0].probability < 0.8 and words[1].probability > 0.85:
            cleaned_0 = to_lower_nopunctuation(words[0].word)
            cleaned_1 = to_lower_nopunctuation(words[1].word)
            if cleaned_0 == 'thank' and cleaned_1 == 'you':
                return []
        
    return words
        
def to_lower_nopunctuation(input_string):
    # Convert input text to lowercase and remove whitespace and punctuations
    return ''.join([char for char in input_string.lower() if char.isalnum()])

def write_log_file(words, log_folder):
    # models.TranscribedWord isn't serializable.  Need to turn each one into a dictionary
    words_dicts = []
    for word in words:
        words_dicts.append({
            'clip_time_start': str(word.clip_time_start),
            'clip_time_stop': str(word.clip_time_stop),
            'transcribe_start': str(word.transcribe_start),
            'transcribe_stop': str(word.transcribe_stop),
            'word_start': str(word.word_start),
            'word_stop': str(word.word_stop),
            'word_probability': word.word_probability,
            'word': word.word })

    dict = { "words": words_dicts }
    json_obj = json.dumps(dict, indent=4)

    with open(log_folder + '/words.json', 'w') as outfile:
        outfile.write(json_obj)