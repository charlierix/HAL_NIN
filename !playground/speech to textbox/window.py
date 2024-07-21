import json5        # json5 supports json with comments
import tkinter as tk
import pyperclip
import queue
import threading
import time

from workers.mic_listener import mic_to_soundclips
from workers.transcriber import transcribe_soundclip
from workers.window_builder import get_window

# ------------------------------ Init -----------------------------

COMMAND_START = 'start'     # value placed on the queue to the mic listener
COMMAND_STOP = 'stop'

with open('config.json', 'r') as f:
    config = json5.load(f)

mic_commands = queue.Queue()
mic_result = queue.Queue()

# Start the mic listener thread
threading.Thread(target=mic_to_soundclips, args=(mic_commands, mic_result, COMMAND_START, COMMAND_STOP, config), daemon=True).start()

# ------------------------- Create Window -------------------------

window = get_window(config)

def start_recording(event):
    mic_commands.put(COMMAND_START)

def stop_recording_and_translate(event):
    mic_commands.put(COMMAND_STOP)

    # Wait for the mic_result queue to be populated, then pop it off and translate the audio
    # NOTE: It would make the UI feel better if the transcription were done in the other thread, but then extra logic
    # would be needed to ignore button pushes while transcribing, or record again, but the other thread is blocked
    # from recording, so a different thread would be needed for transcribing.  For now, just block the UI thread until
    # the transcription finished
    while True:
        if mic_result.empty():
            time.sleep(0.15)

        else:
            clip = mic_result.get()
            text = transcribe_soundclip(clip, config)
            window.textbox.delete("1.0", "end")
            window.textbox.insert("1.0", text)
            break

    if window.checkbox_value.get():
        copy_to_clipboard(event)

def copy_to_clipboard(event):
    print('Copying to clipboard')
    pyperclip.copy(window.textbox.get('1.0', tk.END))

# Bind the button press and release events to the start and stop recording functions...
window.record_button.bind('<ButtonPress-1>', start_recording)
window.record_button.bind('<ButtonRelease-1>', stop_recording_and_translate)
window.copy_button.bind('<ButtonPress-1>', copy_to_clipboard)

window.root.mainloop()