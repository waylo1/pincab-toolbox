using PincabToolbox.Core.Scanning;

namespace PincabToolbox.Core.Tests;

public static class SystemNoiseDirsTests
{
    public static void Test_Recycle_Bin_Is_Noise()
        => Assert.True(SystemNoiseDirs.IsNoise(@"C:\$Recycle.Bin"));

    public static void Test_Real_Folder_Is_Not_Noise()
        => Assert.False(SystemNoiseDirs.IsNoise(@"C:\Visual Pinball\Tables"));

    public static void Test_Is_Case_Insensitive()
        => Assert.True(SystemNoiseDirs.IsNoise(@"C:\WINDOWS"));
}
