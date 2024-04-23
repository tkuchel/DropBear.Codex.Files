using System.Reflection;
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
                CompareLists(containers1, containers2, property.Name, CompareContentContainers);
            else if (value1 is IList<FileVersion> versions1 && value2 is IList<FileVersion> versions2)
                CompareLists(versions1, versions2, property.Name, CompareVersions);
            else if (value1 is IDictionary<string, string> dict1 && value2 is IDictionary<string, string> dict2)
                CompareDictionaries(dict1, dict2, property.Name);
            else if (value1 is byte[] bytes1 && value2 is byte[] bytes2)
                CompareByteArrays(bytes1, bytes2, property.Name);
            else if (!Equals(value1, value2))
                Console.WriteLine($"Difference found in property '{property.Name}': '{value1}' != '{value2}'");
        }
    }

    private static void CompareContentContainers(ContentContainer container1, ContentContainer container2, int index)
    {
        if (container1 == null || container2 == null)
        {
            Console.WriteLine($"ContentContainer at index {index} is null.");
            return;
        }

        Console.WriteLine($"Comparing ContentContainers at index {index}:");
        CompareProperties(container1, container2, typeof(ContentContainer));
    }

    private static void CompareProperties<T>(T object1, T object2, Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value1 = property.GetValue(object1);
            var value2 = property.GetValue(object2);

            if (property.PropertyType == typeof(byte[]))
            {
                var bytes1 = value1 as byte[] ?? new byte[0];
                var bytes2 = value2 as byte[] ?? new byte[0];
                CompareByteArrays(bytes1, bytes2, property.Name);
            }
            else if (!Equals(value1, value2))
            {
                Console.WriteLine($"   - {property.Name}: '{value1}' != '{value2}'");
            }
        }
    }

    private static void CompareByteArrays(byte[] bytes1, byte[] bytes2, string propertyName)
    {
        if (bytes1.Length != bytes2.Length)
        {
            Console.WriteLine(
                $"Difference in {propertyName}: Array lengths differ ({bytes1.Length} vs {bytes2.Length}).");
            return;
        }

        for (var i = 0; i < bytes1.Length; i++)
            if (bytes1[i] != bytes2[i])
            {
                Console.WriteLine($"Difference in {propertyName} at byte index {i}: {bytes1[i]} != {bytes2[i]}");
                // To limit verbosity, you might choose to report only the first few differences
                if (i >= 10)
                {
                    Console.WriteLine("... and more differences.");
                    break;
                }
            }
    }


    private static void CompareLists<T>(IList<T> list1, IList<T> list2, string propertyName,
        Action<T, T, int> compareFunction)
    {
        if (list1.Count != list2.Count)
        {
            Console.WriteLine($"Difference in {propertyName}: Count differs ({list1.Count} vs {list2.Count}).");
            return;
        }

        for (var i = 0; i < list1.Count; i++) compareFunction(list1[i], list2[i], i);
    }


    private static void CompareVersions(FileVersion version1, FileVersion version2, int index)
    {
        if (!version1.Equals(version2))
        {
            Console.WriteLine($"Difference in Versions at index {index}:");
            CompareProperties(version1, version2, typeof(FileVersion));
        }
    }


    private static void CompareDictionaries(IDictionary<string, string> dict1, IDictionary<string, string> dict2,
        string propertyName)
    {
        if (dict1.Count != dict2.Count)
        {
            Console.WriteLine(
                $"Difference in {propertyName}: Dictionary size differs ({dict1.Count} vs {dict2.Count}).");
            return;
        }

        foreach (var key in dict1.Keys.Union(dict2.Keys))
            if (dict1.ContainsKey(key) && dict2.ContainsKey(key))
            {
                if (dict1[key] != dict2[key])
                    Console.WriteLine(
                        $"Difference in {propertyName} for key '{key}': '{dict1[key]}' != '{dict2[key]}'");
            }
            else if (!dict2.ContainsKey(key))
            {
                Console.WriteLine($"Difference in {propertyName}: Key '{key}' missing in second file.");
            }
            else
            {
                Console.WriteLine($"Difference in {propertyName}: Key '{key}' missing in first file.");
            }
    }
}