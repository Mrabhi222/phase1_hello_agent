#pragma warning disable SKEXP0110 // Experimental Agent Framework

using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Phase1HelloAgent
{
    public class MultiAgentDemo
    {
        public static async Task RunAsync(Kernel kernel)
        {
            Console.WriteLine("\n=== MULTI-AGENT COLLABORATION ===");

            // 1. Define the Writer Agent
            ChatCompletionAgent writerAgent = new()
            {
                Name = "Copywriter",
                Instructions = "You write catchy, professional marketing emails. Keep them under 3 sentences.",
                Kernel = kernel
            };

            // 2. Define the Reviewer Agent
            ChatCompletionAgent reviewerAgent = new()
            {
                Name = "Editor",
                Instructions = "You review marketing emails. Give brief suggestions on how to make it more exciting.",
                Kernel = kernel
            };

            // 3. Create a shared chat group
            AgentGroupChat chat = new(writerAgent, reviewerAgent);

            // 4. Start the conversation
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Write a marketing email for a new AI coding assistant for .NET."));
            Console.WriteLine("\n👤 User: Write a marketing email for a new AI coding assistant for .NET.");

            // 5. Let the agents talk to each other in sequence
            // First, the copywriter writes it
            await foreach (var message in chat.InvokeAsync(writerAgent))
            {
                Console.WriteLine($"\n✍️ {message.AuthorName}: {message.Content}");
            }

            // Next, the editor reviews it
            await foreach (var message in chat.InvokeAsync(reviewerAgent))
            {
                Console.WriteLine($"\n🕵️ {message.AuthorName}: {message.Content}");
            }
        }
    }
}