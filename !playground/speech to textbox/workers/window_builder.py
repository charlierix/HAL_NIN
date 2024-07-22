import tkinter as tk

def get_window(config):
    CHECK_TEXT = 'Auto Copy'
    RECORD_TEXT = 'Hold to Record'
    COPY_TEXT = 'Copy to Clipboard'

    root = tk.Tk()
    root.title('Mic to Text')
    frame = tk.Frame(root)

    textbox = None
    checkbox_value = tk.BooleanVar(value=config['AutoCopy'])
    checkbox = None
    record_button = None
    copy_button = None

    if config['DarkMode']:
        root.configure(bg='#242424')
        frame.configure(bg='#242424')
        textbox = tk.Text(frame, bg='#303030', fg='white')
        checkbox = tk.Checkbutton(frame, text=CHECK_TEXT, variable=checkbox_value, bg='#242424', fg='#ADB017')       # can't use fg='white' because the little box is white and there doesn't seem to be a way to change it (fg affects text and the checkmark)
        record_button = tk.Button(frame, text=RECORD_TEXT, bg='#5C5A3F', fg='white')
        copy_button = tk.Button(frame, text=COPY_TEXT, bg='#555555', fg='white')

    else:
        textbox = tk.Text(frame)
        checkbox_value = tk.BooleanVar(value=config['AutoCopy'])
        checkbox = tk.Checkbutton(frame, text=CHECK_TEXT, variable=checkbox_value)
        record_button = tk.Button(frame, text=RECORD_TEXT, bg='#F5F3D7')
        copy_button = tk.Button(frame, text=COPY_TEXT)

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

    return WidowResult(root, frame, textbox, checkbox, checkbox_value, record_button, copy_button)

class WidowResult:
    def __init__(self, root, frame, textbox, checkbox, checkbox_value, record_button, copy_button):
        self.root = root
        self.frame = frame
        self.textbox = textbox
        self.checkbox = checkbox
        self.checkbox_value = checkbox_value
        self.record_button = record_button
        self.copy_button = copy_button