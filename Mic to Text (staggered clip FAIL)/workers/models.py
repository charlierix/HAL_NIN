class SoundClip:
    def __init__(self, start_time, stop_time, clip):
        self.start_time = start_time
        self.stop_time = stop_time
        # numpy arr of sound data
        self.clip = clip

class TranscribedWord:
    def __init__(self, clip_time_start, clip_time_stop, transcribe_start, transcribe_stop, word_start, word_stop, word_probability, word):
        self.clip_time_start = clip_time_start
        self.clip_time_stop = clip_time_stop
        self.transcribe_start = transcribe_start
        self.transcribe_stop = transcribe_stop
        self.word_start = word_start
        self.word_stop = word_stop
        self.word_probability = word_probability
        self.word = word