using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Phase1HelloAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables from .env file if it exists
            string envFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, ".env");
            if (!System.IO.File.Exists(envFilePath) && System.IO.File.Exists(".env"))
            {
                envFilePath = ".env";
            }

            if (System.IO.File.Exists(envFilePath))
            {
                foreach (var line in System.IO.File.ReadAllLines(envFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        // Remove optional surrounding quotes
                        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        else if (value.StartsWith("'") && value.EndsWith("'") && value.Length >= 2)
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
            }

            // 1. Choose your provider:
            // "nvidia" - NVIDIA NIM API (configured with Llama 3.1 8B Instruct)
            // "ollama" - Local Ollama (phi3, llama3, etc.)
            // "openai" - Cloud OpenAI (requires OPENAI_API_KEY env var)
            // "gemini" - Google Gemini (requires GEMINI_API_KEY env var)
            string provider = "nvidia"; 

            // 2. Initialize the Kernel Builder
            var builder = Kernel.CreateBuilder();

            if (provider.Equals("nvidia", StringComparison.OrdinalIgnoreCase))
            {
                string apiKey = Environment.GetEnvironmentVariable("NVIDIA_API_KEY")
                    ?? "";

                // Increase timeout for large models
                var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };

                builder.AddOpenAIChatCompletion(
                    modelId: "nvidia/nemotron-3-ultra-550b-a55b",
                    apiKey: apiKey,
                    endpoint: new Uri("https://integrate.api.nvidia.com/v1"),
                    httpClient: httpClient
                );

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🤖 Configured to use NVIDIA NIM (model: nvidia/nemotron-3-ultra-550b-a55b).");
                Console.ResetColor();
            }
            else if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            {
                // Ollama provides an OpenAI-compatible API on port 11434 by default.
                // We use AddOpenAIChatCompletion pointing to Ollama's local endpoint.
                builder.AddOpenAIChatCompletion(
                    modelId: "phi3", // The name of the model you ran with 'ollama run <model_name>'
                    apiKey: "ollama", // API Key is required by SDK but ignored by Ollama
                    endpoint: new Uri("http://localhost:11434/v1")
                );
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🤖 Configured to use Local Ollama (model: phi3).");
                Console.WriteLine("👉 Make sure you have Ollama running locally ('ollama run phi3') before chatting!");
                Console.ResetColor();
            }
            else if (provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
            {
                // To use OpenAI:
                // Set the OPENAI_API_KEY environment variable, or paste your API key directly below.
                string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Error: OPENAI_API_KEY environment variable is not set.");
                    Console.WriteLine("👉 Please set the environment variable, or edit Program.cs to pass your API key directly.");
                    Console.ResetColor();
                    return;
                }

                builder.AddOpenAIChatCompletion(
                    modelId: "gpt-4o-mini", // Cost-effective model for testing
                    apiKey: apiKey
                );
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🤖 Configured to use OpenAI (model: gpt-4o-mini).");
                Console.ResetColor();
            }
            else if (provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
            {
                string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Error: GEMINI_API_KEY environment variable is not set.");
                    Console.WriteLine("👉 Please set the environment variable, or edit .env to pass your API key directly.");
                    Console.ResetColor();
                    return;
                }

                builder.AddGoogleAIGeminiChatCompletion(
                    modelId: "gemini-1.5-flash",
                    apiKey: apiKey
                );
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🤖 Configured to use Google Gemini (model: gemini-1.5-flash).");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Unknown provider '{provider}'.");
                Console.ResetColor();
                return;
            }

            builder.Plugins.AddFromType<CalculatorPlugin>();
            builder.Plugins.AddFromType<FileAccessPlugin>();
            builder.Plugins.AddFromType<KnowledgeBasePlugin>();

            // Build the kernel container
            Kernel kernel = builder.Build();

            await MultiAgentDemo.RunAsync(kernel);
            return; // Stop here for now

            // 3. Retrieve the Chat Completion Service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Create a chat history object to maintain conversation context (short-term memory)
            var chatHistory = new ChatHistory(
                   "You are a helpfull assistant."
            );

            Console.WriteLine("\n=======================================================");
            Console.WriteLine("💬 Chat Session Started!");
            Console.WriteLine("Type your message and press Enter.");
            Console.WriteLine("Type 'exit' or 'quit' to end the session.");
            Console.WriteLine("=======================================================");

            while (true)
            {
                // Get input from user
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\n👤 You: ");
                Console.ResetColor();

                string? userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) || 
                    userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                // Append the user's message to the chat history
                chatHistory.AddUserMessage(userInput);

                // Print agent response header
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("🤖 Agent: ");
                Console.ResetColor();

                var responseBuilder = new StringBuilder();

                try
                {
                    // Setup execution settings (temperature and max tokens)
                    var executionSettings = new OpenAIPromptExecutionSettings
                    {
                        Temperature = 0.7,
                        MaxTokens = 1024,
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    };

                    // Call the chat service and stream the response content token-by-token
                    await foreach (var chunk in chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings: executionSettings, kernel: kernel))
                    {
                        Console.Write(chunk.Content);
                 
                        responseBuilder.Append(chunk.Content);
                    }
                    Console.WriteLine();

                    // Append the agent's full response to the chat history so the agent remembers it next turn
                    chatHistory.AddAssistantMessage(responseBuilder.ToString());
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n❌ Error communicating with LLM: {ex.Message}");
                    if (provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("💡 Tips for Ollama:");
                        Console.WriteLine("1. Verify Ollama is running in your taskbar.");
                        Console.WriteLine("2. Open cmd/powershell and run: ollama run phi3");
                        Console.WriteLine("3. Keep that window open while running this app.");
                    }
                    Console.ResetColor();
                }
            }
        }
    }
}
