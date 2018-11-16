using System;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace Ironclad.Build
{
    class Program
    {
        private const string ArtifactsDir = "artifacts";
        private const string Build = "build";
        private const string Test = "test";
        private const string Pack = "pack";
        private const string Publish = "publish";

        static void Main(string[] args)
        {
            var travisBuildNumber = Environment.GetEnvironmentVariable("TRAVIS_BUILD_NUMBER");
            var buildNumber = travisBuildNumber ?? "0.0.1-developer";

            Target(Build, () => Run("dotnet", "build Ironclad.sln -c Release"));

            Target(
                Test,
                DependsOn(Build),
                () => Run("dotnet", "test tests/Ironclad.Tests/Ironclad.Tests.csproj -c Release -l trx;LogFileName=Ironclad.Tests.xml --verbosity=normal"));

            if(Environment.GetEnvironmentVariable("CI") == null)
            {
                Target(Publish, () => Run("dotnet", "publish Ironclad.sln -c Release -r linux-x64")); //-o /app
            }
            else
            {
                Target(Publish, () => Run("dotnet", $"publish Ironclad.sln -c Release -r linux-x64 -o ./build /p:ShowLinkerSizeComparison=true /p:Version={buildNumber}"));
            }
            
            
            Target("default", DependsOn(Test, Publish));

            RunTargets(args);
        }
    }
}
