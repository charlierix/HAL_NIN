using MAFTesters_Core.Tools;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Windows;
using System.Windows.Controls;

namespace MAFTesters_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                cboModel.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // this is from the first example on the page.  there are other good ones on that page
        // https://medium.com/@venya-brodetskiy/getting-started-with-microsoft-agent-framework-61a1112220f8
        private async void WeatherExample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = GetOllamaValues();
                if (settings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Convert the function to a tool
                var weatherFunction = AIFunctionFactory.Create(WeatherTool.GetWeather);

                // Create the agent
                var client = new OllamaApiClient(settings.Value.url, settings.Value.model);


                // NOTE: it doesn't look like tools can be dynamically added.  if a new tool gets created, a new agent instance would be needed
                // NOTE: added the line to not make up an answer because it would just return a random temperature if the tool returned a bad response
                var agent = client.CreateAIAgent(
                        instructions: "say 'just a second' before answering question.  if there is an error calling the tool, please don't try to make up a response",
                        tools: [weatherFunction],
                        name: "myagent");



                /*
                Thread Management: GetNewThread() creates a conversation context that maintains message history. Each thread is isolated, allowing you to manage multiple conversations independently.
                */

                // Create a thread for conversation
                var thread = agent.GetNewThread();


                // Run the agent with streaming
                // NOTE: it wan't calling the weather tool unless the prompt called for temperature
                // NOTE: it asked for a location when one wasn't provided
                var streamingResponse = agent.RunStreamingAsync(txtPrompt.Text, thread);

                txtLog.Text = "";

                await foreach (var chunk in streamingResponse)
                {
                    txtLog.Text += chunk;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string url, string model)? GetOllamaValues()
        {
            var model_item = cboModel.SelectedItem as ComboBoxItem;
            string model = model_item?.Content?.ToString();

            if (string.IsNullOrEmpty(txtOllamaURL.Text) || string.IsNullOrEmpty(model))
                return null;

            return (txtOllamaURL.Text, model);
        }
    }
}