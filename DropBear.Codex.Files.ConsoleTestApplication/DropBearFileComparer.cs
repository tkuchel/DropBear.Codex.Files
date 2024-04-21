using DropBear.Codex.Files.Models;

namespace DropBear.Codex.Files.ConsoleTestApplication;

public static class DropBearFileComparer
{
    public static void CompareDropBearFiles(DropBearFile? file1, DropBearFile? file2)
    {
        if (file1 == null || file2 == null)
        {
            Console.WriteLine("One of the files is null.");
            return;
        }

        Console.WriteLine("Comparing DropBearFiles...");
        var type = typeof(DropBearFile);
        foreach (var property in type.GetProperties())
        {
            var value1 = property.GetValue(file1);
            var value2 = property.GetValue(file2);

            if (value1 is IList<ContentContainer> containers1 && value2 is IList<ContentContainer> containers2)
            {
                if (!CompareContentContainers(containers1, containers2))
                    Console.WriteLine($"Difference found in property '{property.Name}': Content containers differ.");
            }
            else if (value1 is IDictionary<string, string> dict1 && value2 is IDictionary<string, string> dict2)
            {
                if (!CompareDictionaries(dict1, dict2))
                    Console.WriteLine($"Difference found in property '{property.Name}': Dictionaries differ.");
            }
            else if (!Equals(value1, value2))
            {
                Console.WriteLine($"Difference found in property '{property.Name}': '{value1}' != '{value2}'");
            }
        }
    }

    private static bool CompareContentContainers(IList<ContentContainer> containers1, IList<ContentContainer> containers2)
    {
        if (containers1.Count != containers2.Count)
        {
            Console.WriteLine($"Content containers count differs: {containers1.Count} != {containers2.Count}");
            return false;
        }

        for (int i = 0; i < containers1.Count; i++)
        {
            if (!containers1[i].Equals(containers2[i]))
            {
                Console.WriteLine($"Content containers at index {i} differ.");
                return false;
            }
        }
        return true;
    }


    private static bool CompareDictionaries(IDictionary<string, string> dict1, IDictionary<string, string> dict2)
    {
        if (dict1.Count != dict2.Count) return false;

        foreach (var key in dict1.Keys)
            if (!dict2.ContainsKey(key) || dict1[key] != dict2[key])
            {
                Console.WriteLine($"Metadata key-value pair differs for key '{key}'.");
                return false;
            }

        return true;
    }
}