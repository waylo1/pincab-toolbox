using System.Reflection;

namespace PincabToolbox.Repair.Tests;

/// <summary>Micro test framework: every public static void method starting with "Test_" is a test.</summary>
public static class TestRunner
{
    public static int Main(string[] args)
    {
        var filter = args.FirstOrDefault(a => !a.StartsWith('-'));
        var methods = typeof(TestRunner).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name.StartsWith("Test_") && m.GetParameters().Length == 0)
            .OrderBy(m => m.DeclaringType!.Name).ThenBy(m => m.Name)
            .ToList();

        int passed = 0, failed = 0;
        foreach (var m in methods)
        {
            var name = $"{m.DeclaringType!.Name}.{m.Name}";
            if (filter is not null && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                m.Invoke(null, null);
                passed++;
                Console.WriteLine($"  ok  {name}");
            }
            catch (Exception ex)
            {
                failed++;
                var inner = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                Console.WriteLine($"FAIL  {name}");
                Console.WriteLine($"      {inner.GetType().Name}: {inner.Message}");
                var line = inner.StackTrace?.Split('\n').FirstOrDefault(l => l.Contains("Tests"));
                if (line is not null) Console.WriteLine($"      {line.Trim()}");
            }
        }

        Console.WriteLine($"\n{passed} passed, {failed} failed, {passed + failed} total");
        return failed == 0 ? 0 : 1;
    }
}

public static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition) throw new Exception(message ?? "Expected true.");
    }

    public static void False(bool condition, string? message = null) => True(!condition, message ?? "Expected false.");

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception(message ?? $"Expected: {expected}\n      Actual:   {actual}");
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value is null) throw new Exception(message ?? "Expected non-null.");
    }

    public static void Contains(string needle, string? haystack, string? message = null)
    {
        if (haystack is null || !haystack.Contains(needle, StringComparison.Ordinal))
            throw new Exception(message ?? $"Expected to find \"{needle}\" in:\n      {haystack?[..Math.Min(200, haystack.Length)]}");
    }
}
