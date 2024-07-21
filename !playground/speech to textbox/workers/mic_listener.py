import time

# This runs in its own thread, looking for 'start' and 'stop' commands to come through on the command queue
# While in 'start' state, it streams from the mic
# Once told to stop, it puts the audio on the result queue
def mic_to_soundclips(queue_commands, queue_result, command_start, command_stop, config):
    while True:
        if queue_commands.empty():
            time.sleep(0.333)

        else:
            command = queue_commands.get()

            if command == command_start:
                print('Start Recording')
                # Add your start recording code here...

            elif command == command_stop:
                print('Stop Recording and Translate')
                # Add your stop recording and translation code here...        