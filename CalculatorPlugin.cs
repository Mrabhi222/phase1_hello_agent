using System.ComponentModel;
using Microsoft.SemanticKernel;



public class CalculatorPlugin{
    [KernelFunction]
    [Description("Multiplies two numbers together.")]
    public double Multiply(
        [Description("The first number")] double number1,
        [Description("The second number")] double number2)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n[SYSTEM: C# Method 'Multiply' was executed with args: {number1}, {number2}]");
        Console.ResetColor();
        
        return number1 * number2;
    }
}