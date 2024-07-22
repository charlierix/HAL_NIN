from faster_whisper import WhisperModel

def transcribe_soundclip(clip, config):
    config_translate = config['translate']
    language = config_translate['language']
    if language == '':
        language = None

    model = WhisperModel(config_translate['model_size'], device=config_translate['device'], compute_type=config_translate['compute_type'])
    words = transcribe_clip(model, clip, language)
    return ''.join(o.word for o in words).strip()

def transcribe_clip(model, clip, language):
    segments, _ = model.transcribe(clip, language=language, word_timestamps=True)

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

class TranscribedWord:
    def __init__(self, probability, word):
        self.probability = probability
        self.word = word