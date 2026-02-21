using Microsoft.Extensions.AI;
using OllamaSharp;

namespace MAFTesters_Agents
{
    public class ClientSettings
    {
        public string Ollama_URL { get; set; }
        public string Ollama_Model { get; set; }

        // TODO: take a param for type of model (small, large, require image, etc)
        public IChatClient CreateClient()
        {
            //// Set up the Azure OpenAI client
            //var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            //var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
            //var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Using ollama
            return new OllamaApiClient(Ollama_URL, Ollama_Model);
        }
    }
}
