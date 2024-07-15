from datetime import datetime, timezone, timedelta
from faster_whisper import WhisperModel
import json
import keyboard
import numpy as np
import os
import pyaudio
import wave

CLIP_LENGTH = 0.5
FORMAT = pyaudio.paInt16
RATE = 16000

key_pressed = False     # set by an event listener

class TranscribedWord:
    def __init__(self, start, stop, probability, word):
        self.start = start
        self.stop = stop
        self.probability = probability
        self.word = word

# ---------------------------------------------------------------------------------------

def get_base_folder():
    retVal = 'output/' + datetime.now().strftime('%Y-%m-%d %H-%M-%S')
    os.makedirs(retVal)
    return retVal

def on_key_press(event):        # event param is needed to conform to keyboard.on_press interface
    global key_pressed          # referencing the global so that it doesn't set a local variable with the same name
    key_pressed = True
    keyboard.unhook_all()

# ---------------------------------------------------------------------------------------

def record_sound_clips(audio):
    stream = audio.open(format=FORMAT, channels=1, rate=RATE, input=True, frames_per_buffer=1024)

    clips = []

    start = datetime.now(timezone.utc)
    last_print = start

    while True:
        if key_pressed:
            break

        now = datetime.now(timezone.utc)
        if (now - last_print).total_seconds() > 5:
            last_print = now
            print('%.1f seconds' % ((now - start).total_seconds()))

        clips.append(record_clip(stream))

    stream.stop_stream()
    stream.close()

    return clips

# Records clip_length seconds of audio into a list
def record_clip(stream):
    retVal = b''

    for _ in range(0, int(RATE / 1024 * CLIP_LENGTH)):
        retVal += stream.read(1024)

    return retVal

# ---------------------------------------------------------------------------------------

def transcribe(clips, base_folder, audio):
    power = 0

    while True:
        span = 2 ** power

        print('transcribing ' + str(span * CLIP_LENGTH) + ' second clips')

        transcribe_span(clips, span, base_folder, audio, False)

        # WARNING: execution is sometimes stopping after the first call to transcribe_span.  I've spent several hours
        # on this, adding print statements all over the place.  I thought maybe keyboard listener, but now I don't
        # think so
        #
        # I'm guessing the whisper model doesn't like that many instances created
        #
        # Also seems to be related to how long the recording is.  20 seconds is fine, 40+ has issues

        transcribe_span(clips, span, base_folder, audio, True)

        if span >= len(clips):
            break

        power += 1

def transcribe_span(clips, span, base_folder, audio, condition_on_previous_text):
    folder = base_folder + '/' + str(span) + ' ' + str(condition_on_previous_text)
    os.makedirs(folder)

    model = WhisperModel('distil-large-v3', device='cuda', compute_type='int8_float16')

    index = 0

    while index <= len(clips):
        clip = extract_clip(clips, index, span)
        words = transcribe_clip(clip[0], model, condition_on_previous_text)
        write_outputs(folder, words, clip[1], audio, index)

        index += span

def extract_clip(clips, index, span):
    data = b''
    data_list = []

    for i in range(index, min(index + span, len(clips) - 1)):
        data += clips[i]
        data_list.append(clips[i])

    return (np.frombuffer(data, np.int16).astype(np.float32) / 32768.0, data_list)

def transcribe_clip(clip, model, condition_on_previous_text):
    segments, _ = model.transcribe(clip, language='en', word_timestamps=True, condition_on_previous_text=condition_on_previous_text)
    retVal = []

    for segment in segments:
        # NOTE: this needs param: word_timestamps=True
        for word in segment.words:
            retVal.append(TranscribedWord(word.start, word.end, word.probability, word.word))

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

def write_outputs(folder, words, clip, audio, index):
    filename_base = folder + '/' + str(index)

    write_wav_file(filename_base + '.wav', audio, clip)
    write_json_file(filename_base + '.json', words)

def write_wav_file(filename, audio, full_sound):
    sound_file = wave.open(filename, 'wb')     # wb is write bytes mode
    sound_file.setnchannels(1)
    sound_file.setsampwidth(audio.get_sample_size(FORMAT))
    sound_file.setframerate(RATE)
    sound_file.writeframes(b''.join(full_sound))
    sound_file.close()

def write_json_file(filename, words):
    # TranscribedWord isn't serializable.  Need to turn each one into a dictionary
    words_dicts = []
    for word in words:
        words_dicts.append({
            'start': str(word.start),
            'stop': str(word.stop),
            'probability': word.probability,
            'word': word.word })

    dict = { "words": words_dicts }
    json_obj = json.dumps(dict, indent=4)

    with open(filename, 'w') as outfile:
        outfile.write(json_obj)

# ---------------------------------------------------------------------------------------

if __name__ == "__main__":
    try:
        base_folder = get_base_folder()

        print('Press any key to stop recording...')
        print('try to keep it around 20 seconds.  Long recordings have a good chance of the script just stopping silently')
        # NOTE: this sees any keypress by the os, not just when console has focus
        keyboard.on_press(on_key_press)

        audio = pyaudio.PyAudio()

        clips = record_sound_clips(audio)

        print('Transcribing - clip len: ' + str(len(clips)))
        transcribe(clips, base_folder, audio)

        print('after transcribing')

        audio.terminate()

        print('Finished')

    except Exception as e:
        print(f"An error occurred: {e}")