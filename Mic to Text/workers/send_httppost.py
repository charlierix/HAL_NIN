import requests
import json

# The URL to which we want to make the POST request
url = "http://localhost:5030/VisualizeWords"

# Data that we want to send in our HTTP POST request
data = [
  {
    "speakerName": "microphone",
    "start": "2024-07-10T04:39:46.610Z",
    "stop": "2024-07-10T04:39:46.610Z",
    "probability": 1,
    "text": "this is badass"
  }
]

# Convert Python dictionary object (data) into JSON string using json.dumps() method
json_data = json.dumps(data)

# Set headers to indicate that we are sending a JSON payload
headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
}

try:
    # Make the HTTP POST request using requests.post() method
    response = requests.post(url, data=json_data, headers=headers)
    
    # Check if the request was successful (status code 200 OK) and print the result
    if response.ok:
        print("Request Successful")
        print("Response JSON Data:", response.json())
    else:
        print(f"Error occurred with status code {response.status_code}")
        
except requests.exceptions.RequestException as e:
    # Handle any exceptions that may occur while making the request
    print("An error occurred during the HTTP POST request.")
    print(e)