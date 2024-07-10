import requests
import json

def send_http(words, config):
    config_send = config['send_results']

    if config_send['should_send_marquee']:
        send_marquee(words, config_send)
    
def send_marquee(words, config_send):
  # The URL to which we want to make the POST request
  url = config_send['url_marquee']

  # Data that we want to send in our HTTP POST request
  data = get_data_marquee(words, config_send)

  # Convert Python dictionary object (data) into JSON string
  json_data = json.dumps(data)

  # Set headers to indicate that we are sending a JSON payload
  headers = get_headers()

  send_it(url, json_data, headers)

# Input model: models.TranscribedWord
# Output model: CentralListeners.WebApi.Models.Word
def get_data_marquee(words, config_send):
    FORMAT = '%Y-%m-%dT%H:%M:%S.%fZ'
    speaker_name = config_send['sendername_marquee']

    data = []

    for word in words:
        data.append({
            'speakerName': speaker_name,
            'start': word.word_start.strftime(FORMAT),
            'stop': word.word_stop.strftime(FORMAT),
            'probability': word.word_probability,
            'text': word.word})
      
    return data

def get_headers():
    return {
      'Content-Type': 'application/json',
      'Accept': 'application/json'}

def send_it(url, json_data, headers):
  try:
      # Make the HTTP POST request using requests.post() method
      response = requests.post(url, data=json_data, headers=headers)
      
      # Check if the request was successful (status code 200 OK) and print the result
      if not response.ok:
          print(f"Error occurred with status code {response.status_code}")
          
  except requests.exceptions.RequestException as ex:
      # Handle any exceptions that may occur while making the request
      print("An error occurred during the HTTP POST request")
      print(ex)