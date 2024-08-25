

using System.Collections.Immutable;
using System.Text.Json;

namespace RuleGenerator{

public readonly record struct Cluster {
    public ImmutableArray<string> Item1 {get; init;}
    public string Item2 {get; init;}
}

public class Program
{

    public static ImmutableArray<string> default_rule_priority = [
                ":",
                "l",
                "u",
                "c",
                "t",
                "TN",
                "zN",
                "ZN",
                "$X",
                "^X",
                "[",
                "]",
                "DN",
                "iNX",
                "oNX",
                "}",
                "{",
                "r",
                "sXY"
            ];
    
    
    public static string? FindApplicableRule(string base_, string other) {

    }

    public static List<string> GetHashcatRules (string base_, string other){
        List<string> generatedRules = new();
        var current_password = base_;
        if (current_password == other)
            generatedRules.Add(":");
        
        while (current_password != other){

        }

        return generatedRules;
    }

    public static int Main(string[] args)
    {
        
        
        var stdin = Console.OpenStandardInput();
        var clusters = JsonSerializer.Deserialize<ImmutableDictionary<int,Cluster>>(stdin);

        ImmutableArray<string> rule_priority = default_rule_priority;

        foreach (var cluster in clusters.Values) {
            // Skip the -1 outlier here

            string representative = cluster.Item2;
            foreach (string word in cluster.Item1){

            }
        }

        return 0;

    }

}

}