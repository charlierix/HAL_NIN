import tkinter as tk
import pyperclip
import threading

root = tk.Tk()
root.title('Mic to Text')
frame = tk.Frame(root)
textbox = tk.Text(frame)
checkbox_value = tk.BooleanVar(value=True)
checkbox = tk.Checkbutton(frame, text='Auto Copy', variable=checkbox_value)
record_button = tk.Button(frame, text='Hold to Record')
copy_button = tk.Button(frame, text='Copy to Clipboard')

def start_recording(event):
    print('start_recording')

def stop_recording_and_translate(event):
    print('stop_recording_and_translate')

    if checkbox_value.get():
        print('also copy to clipboard')
        copy_to_clipboard(event)

def copy_to_clipboard(event):
    print('copy_to_clipboard')
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