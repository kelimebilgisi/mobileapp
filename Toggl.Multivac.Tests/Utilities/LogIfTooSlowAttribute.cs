using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

public sealed class LogIfTooSlowAttribute : BeforeAfterTestAttribute
{
    private readonly Stopwatch stopwatch = new Stopwatch();

    private static readonly TimeSpan fast = TimeSpan.FromSeconds(0.5f);
    private static readonly TimeSpan moderate = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan slow = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan critical = TimeSpan.FromSeconds(5);

    private TestTooLongDetector testStuckDetector;

    public override void Before(MethodInfo methodUnderTest)
    {
        testStuckDetector = TestTooLongDetector.StartDetection(
            reportAfter: critical * 2,
            callback: () => reportStuckTest(methodUnderTest));

        stopwatch.Start();
    }

    public override void After(MethodInfo methodUnderTest)
    {
        stopwatch.Stop();

        testStuckDetector.CancelDetection();

        if (stopwatch.Elapsed <= fast) return;

        var formattedTestName = getNormalizedFormattedTestName(methodUnderTest);

        Console.ForegroundColor = getColorForTime(stopwatch.Elapsed);
        Console.WriteLine($"{stopwatch.Elapsed} - {formattedTestName}");
    }

    private void reportStuckTest(MethodInfo methodUnderTest)
    {
        var formattedTestName = getNormalizedFormattedTestName(methodUnderTest);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[! TEST STUCK !] - {formattedTestName}");
    }

    private string getNormalizedFormattedTestName(MethodInfo methodUnderTest)
        => getFormattedName(methodUnderTest.ReflectedType, toSentenceCase(methodUnderTest.Name)).Replace("`1", "");

    private ConsoleColor getColorForTime(TimeSpan elapsed)
    {
        if (elapsed <= moderate) return ConsoleColor.DarkMagenta;
        if (elapsed <= slow) return ConsoleColor.DarkYellow;
        if (elapsed <= critical) return ConsoleColor.Yellow;
        return ConsoleColor.Red;
    }

    private string getFormattedName(Type typeInfo, string accumulator)
    {
        if (typeInfo.DeclaringType == null)
            return $"{typeInfo.Name}: {accumulator[0]}{accumulator.Substring(1).ToLower()}";

        return getFormattedName(typeInfo.DeclaringType, $"{toSentenceCase(typeInfo.Name)} {accumulator}");
    }

    private static string toSentenceCase(string value)
        => Regex.Replace(value, "[a-z][A-Z]", x => $"{x.Value[0]} {char.ToLower(x.Value[1])}");
}
