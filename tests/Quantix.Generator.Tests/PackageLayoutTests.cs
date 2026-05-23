using System.Diagnostics;
using System.IO.Compression;
using Xunit;

namespace Quantix.Generator.Tests;

/// <summary>
/// Validates the layout of the produced Quantix NuGet package (plan L3-9): the generator must
/// ship under <c>analyzers/dotnet/cs/</c> and the abstractions under <c>lib/</c> for every
/// target framework. The test is self-sufficient — if no package has been produced yet it
/// packs one — so it validates the same thing whether or not <c>dotnet pack</c> ran first.
/// </summary>
public class PackageLayoutTests
{
    [Fact]
    public void Package_places_the_generator_and_abstractions_in_the_expected_folders()
    {
        string repoRoot = FindRepoRoot();
        string? package = FindQuantixPackage(repoRoot) ?? PackAndFind(repoRoot);

        Assert.True(
            package is not null,
            "No Quantix .nupkg was found and 'dotnet pack' did not produce one.");

        using ZipArchive archive = ZipFile.OpenRead(package!);
        var entries = new HashSet<string>(
            archive.Entries.Select(static entry => entry.FullName),
            StringComparer.Ordinal);

        Assert.Contains("analyzers/dotnet/cs/Quantix.Generator.dll", entries);
        Assert.Contains("lib/net8.0/Quantix.Abstractions.dll", entries);
        Assert.Contains("lib/net10.0/Quantix.Abstractions.dll", entries);
        Assert.Contains("README.md", entries);
    }

    /// <summary>Walks up from the test assembly to the directory holding <c>Quantix.slnx</c>.</summary>
    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Quantix.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (Quantix.slnx).");
    }

    /// <summary>Returns the most recently written Quantix <c>.nupkg</c>, or null when none exists.</summary>
    private static string? FindQuantixPackage(string repoRoot)
    {
        string packageRoot = Path.Combine(repoRoot, "artifacts", "package");
        if (!Directory.Exists(packageRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(packageRoot, "Quantix.*.nupkg", SearchOption.AllDirectories)
            .Where(static path => Path.GetExtension(path).Equals(".nupkg", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    /// <summary>Packs the Quantix project, then locates the resulting package.</summary>
    private static string? PackAndFind(string repoRoot)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(Path.Combine("src", "Quantix", "Quantix.csproj"));
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--nologo");

        using Process? process = Process.Start(startInfo);
        process?.WaitForExit();

        return FindQuantixPackage(repoRoot);
    }
}
