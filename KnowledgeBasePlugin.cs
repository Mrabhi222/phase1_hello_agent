using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;



namespace Phase1HelloAgent
{
    public  class KnowledgeBasePlugin 
    {
       // A mock list of company policies (our "Database")
      private readonly List<string> _companyPolicies = new()
      {
         "Vacation Policy: Employees get 25 days of paid time off per calendar year.",
         "Dress Code: The office dress code is business casual. No flip-flops allowed.",
         "Remote Work: Employees can work remotely up to 2 days per week with manager approval.",
         "Expense Policy: Meals during business travel can be expensed up to $50 per day."
      };

      [KernelFunction]
        [Description("Searches the company policy database for relevant information.")]
        public string SearchPolicies(    [Description("The search query or keyword (e.g., 'vacation', 'dress code', 'remote')")] string query)
        {
              Console.WriteLine($"\n[SYSTEM: KnowledgeBase search executed for query: '{query}']");
          
         // Perform a simple keyword match (mock vector search)
            var results = _companyPolicies
                .Where(p => p.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (results.Any())
            {
                return string.Join("\n", results);
            }
            return "No matching company policies found.";
        }

    } 
}