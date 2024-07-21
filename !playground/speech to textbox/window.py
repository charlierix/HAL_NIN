import json5        # json5 supports json with comments
import tkinter as tk
import pyperclip
import queue
import threading
import time

from workers.mic_listener import mic_to_soundclips
from workers.transcriber import transcribe_soundclip

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


# TODO: put a lot of this in a helper class


root = tk.Tk()
root.title('Mic to Text')
frame = tk.Frame(root)


# TODO: support option for dark mode
#textbox = tk.Text(frame, bg='#333333', fg='white')

textbox = tk.Text(frame)


checkbox_value = tk.BooleanVar(value=config['AutoCopy'])
checkbox = tk.Checkbutton(frame, text='Auto Copy', variable=checkbox_value)

# TODO: make the record button stand out

record_button = tk.Button(frame, text='Hold to Record')


copy_button = tk.Button(frame, text='Copy to Clipboard')

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
            textbox.delete("1.0", "end")
            textbox.insert("1.0", text)
            break

    if checkbox_value.get():
        copy_to_clipboard(event)

def copy_to_clipboard(event):
    print('Copying to clipboard')
    pyperclip.copy(textbox.get('1.0', tk.END))

# Bind the button press and release events to the start and stop recording functions...
record_button.bind('<ButtonPress-1>', start_recording)
record_button.bind('<ButtonRelease-1>', stop_recording_and_translate)
copy_button.bind('<ButtonPress-1>', copy_to_clipboard)

# Use grid to manage layout, placing widgets in the same row but different columns...
frame.grid(column=0, row=0, sticky='nesw', padx=4, pady=4)
textbox.grid(column=0, row=0, columnspan=3, sticky='nsew', pady=(0, 8))  # Textbox spans all columns and expands vertically
checkbox.grid(column=0, row=1, sticky='nesw')
record_button.grid(column=1, row=1, sticky='nesw', padx=8)
copy_button.grid(column=2, row=1, sticky='nesw')

# Make the frame resize when the root (window) resizes
root.columnconfigure(0, weight=1)
root.rowconfigure(0, weight=1)

# Configure the grid weights so that widgets can expand properly when window is resized...
frame.columnconfigure(0, weight=0)
frame.columnconfigure(1, weight=1)
frame.columnconfigure(2, weight=0)

frame.rowconfigure(0, weight=1)
frame.rowconfigure(1, weight=0)

root.mainloop()