using System;
using System.IO;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Phase1HelloAgent
{
    public class FileAccessPlugin
    {
        [KernelFunction]
        [Description("Reads the contents of a local text file from disk.")]
        public string ReadTextFile(
            [Description("The path to the text file to read (e.g., 'secret.txt')")] string filePath)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n[SYSTEM: C# Method 'ReadTextFile' was executed for path: {filePath}]");
            Console.ResetColor();

            try
            {
                // We resolve the path relative to the application directory for safety
                string fullPath = Path.Combine(AppContext.BaseDirectory, filePath);
                
                // If it doesn't exist there, try the current working directory
                if (!File.Exists(fullPath))
                {
                    fullPath = filePath;
                }

                if (File.Exists(fullPath))
                {
                    return File.ReadAllText(fullPath);
                }
                else
                {
                    return $"Error: The file at '{filePath}' could not be found.";
                }
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }
    }
}