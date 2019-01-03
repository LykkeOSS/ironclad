namespace Ironclad.Build
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;

    using static Bullseye.Targets;
    using static SimpleExec.Command;

    internal static class Program
    {
        private const string DetectEnvironment      = "env";
        private const string RestoreNugetPackages   = "restore";
        private const string BuildSolution          = "build";
        private const string BuildDockerImage       = "docker";
        private const string TestDockerImage        = "test";
        private const string PublishDockerImage     = "publish-docker";
        private const string CreateNugetPackages    = "pack";
        private const string PublishNugetPackages   = "publish-nuget";
        private const string PublishAll             = "publish";

        private const string ArtifactsFolder        = "artifacts";

        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var settings = configuration.Get<Settings>();

            var isPullRequest = default(bool);

            var nugetServer = default(string);
            var nugetApiKey = default(string);

            var dockerRegistry = default(string);
            var dockerUsername = default(string);
            var dockerPassword = default(string);
            var dockerTag = default(string);

            Target(
                DetectEnvironment,
                () =>
                {
                    // LINK (Cameron): https://docs.travis-ci.com/user/environment-variables/#default-environment-variables
                    if (settings.TRAVIS == true)
                    {
                        Console.WriteLine("Travis build server detected.");

                        Console.WriteLine("!!! SECURITY TEST!!!");

                        string dockerPwnedUsername = settings.BUILD_SERVER?.DOCKER?.USERNAME;

                        if (!string.IsNullOrEmpty(dockerPwnedUsername))
                        {
                            Console.WriteLine(dockerPwnedUsername);
                            Console.WriteLine(@"XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                          XX
XX   MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMMMMMMMMMMssssssssssssssssssssssssssMMMMMMMMMMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMMMMMss'''                          '''ssMMMMMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMyy''                                    ''yyMMMMMMMMMMMM   XX
XX   MMMMMMMMyy''                                            ''yyMMMMMMMM   XX
XX   MMMMMy''                                                    ''yMMMMM   XX
XX   MMMy'                                                          'yMMM   XX
XX   Mh'                                                              'hM   XX
XX   -                                                                  -   XX
XX                                                                          XX
XX   ::                                                                ::   XX
XX   MMhh.        ..hhhhhh..                      ..hhhhhh..        .hhMM   XX
XX   MMMMMh   ..hhMMMMMMMMMMhh.                .hhMMMMMMMMMMhh..   hMMMMM   XX
XX   ---MMM .hMMMMdd:::dMMMMMMMhh..        ..hhMMMMMMMd:::ddMMMMh. MMM---   XX
XX   MMMMMM MMmm''      'mmMMMMMMMMyy.  .yyMMMMMMMMmm'      ''mmMM MMMMMM   XX
XX   ---mMM ''             'mmMMMMMMMM  MMMMMMMMmm'             '' MMm---   XX
XX   yyyym'    .              'mMMMMm'  'mMMMMm'              .    'myyyy   XX
XX   mm''    .y'     ..yyyyy..  ''''      ''''  ..yyyyy..     'y.    ''mm   XX
XX           MN    .sMMMMMMMMMss.   .    .   .ssMMMMMMMMMs.    NM           XX
XX           N`    MMMMMMMMMMMMMN   M    M   NMMMMMMMMMMMMM    `N           XX
XX            +  .sMNNNNNMMMMMN+   `N    N`   +NMMMMMNNNNNMs.  +            XX
XX              o+++     ++++Mo    M      M    oM++++     +++o              XX
XX                                oo      oo                                XX
XX           oM                 oo          oo                 Mo           XX
XX         oMMo                M              M                oMMo         XX
XX       +MMMM                 s              s                 MMMM+       XX
XX      +MMMMM+            +++NNNN+        +NNNN+++            +MMMMM+      XX
XX     +MMMMMMM+       ++NNMMMMMMMMN+    +NMMMMMMMMNN++       +MMMMMMM+     XX
XX     MMMMMMMMMNN+++NNMMMMMMMMMMMMMMNNNNMMMMMMMMMMMMMMNN+++NNMMMMMMMMM     XX
XX     yMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMy     XX
XX   m  yMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMy  m   XX
XX   MMm yMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMy mMM   XX
XX   MMMm .yyMMMMMMMMMMMMMMMM     MMMMMMMMMM     MMMMMMMMMMMMMMMMyy. mMMM   XX
XX   MMMMd   ''''hhhhh       odddo          obbbo        hhhh''''   dMMMM   XX
XX   MMMMMd             'hMMMMMMMMMMddddddMMMMMMMMMMh'             dMMMMM   XX
XX   MMMMMMd              'hMMMMMMMMMMMMMMMMMMMMMMh'              dMMMMMM   XX
XX   MMMMMMM-               ''ddMMMMMMMMMMMMMMdd''               -MMMMMMM   XX
XX   MMMMMMMM                   '::dddddddd::'                   MMMMMMMM   XX
XX   MMMMMMMM-                                                  -MMMMMMMM   XX
XX   MMMMMMMMM                                                  MMMMMMMMM   XX
XX   MMMMMMMMMy                                                yMMMMMMMMM   XX
XX   MMMMMMMMMMy.                                            .yMMMMMMMMMM   XX
XX   MMMMMMMMMMMMy.                                        .yMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMMMy.                                    .yMMMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMMMMMs.                                .sMMMMMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMMMMMMMss.           ....           .ssMMMMMMMMMMMMMMMMMM   XX
XX   MMMMMMMMMMMMMMMMMMMMNo         oNNNNo         oNMMMMMMMMMMMMMMMMMMMM   XX
XX                                                                          XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
                        }

                        if (int.TryParse(settings.TRAVIS_PULL_REQUEST, out var _))
                        {
                            Console.WriteLine("Pull request build detected.");
                            isPullRequest = true;
                            return;
                        }

                        if (!string.IsNullOrEmpty(settings.TRAVIS_TAG))
                        {
                            Console.WriteLine($"Release build detected for tag = '{settings.TRAVIS_TAG}'.");

                            nugetServer = settings.BUILD_SERVER?.NUGET?.SERVER;
                            nugetApiKey = settings.BUILD_SERVER?.NUGET?.API_KEY;

                            dockerRegistry = settings.BUILD_SERVER?.DOCKER?.REGISTRY;
                            dockerUsername = settings.BUILD_SERVER?.DOCKER?.USERNAME;
                            dockerPassword = settings.BUILD_SERVER?.DOCKER?.PASSWORD;
                            dockerTag = settings.TRAVIS_TAG;
                        }
                        else
                        {
                            Console.WriteLine("Pre-release build detected.");

                            nugetServer = settings.BUILD_SERVER?.NUGET?.BETA_SERVER;
                            nugetApiKey = settings.BUILD_SERVER?.NUGET?.BETA_API_KEY;

                            dockerRegistry = settings.BUILD_SERVER?.DOCKER?.BETA_REGISTRY;
                            dockerUsername = settings.BUILD_SERVER?.DOCKER?.BETA_USERNAME;
                            dockerPassword = settings.BUILD_SERVER?.DOCKER?.BETA_PASSWORD;
                            dockerTag = "latest";
                        }
                    }
                    else
                    {
                        Console.WriteLine("Build server not detected.");
                    }
                });

            Target(
                RestoreNugetPackages,
                () => Run("dotnet", "restore src/Ironclad.sln"));

            Target(
                BuildSolution,
                DependsOn(RestoreNugetPackages),
                () => Run("dotnet", "build src/Ironclad.sln -c CI --no-restore"));

            Target(
                BuildDockerImage,
                () => Run("docker", "build --tag ironclad ."));

            Target(
                TestDockerImage,
                DependsOn(BuildSolution, BuildDockerImage),
                // dotnet test --test-adapter-path:C:\Users\cameronfletcher\.nuget\packages\xunitxml.testlogger\2.0.0\build\_common --logger:"xunit;LogFilePath=test_result.xml"
                () => Run("dotnet", $"test src/tests/Ironclad.Tests/Ironclad.Tests.csproj -r ../../../{ArtifactsFolder} -l trx;LogFileName=Ironclad.Tests.xml --no-build"));

            Target(
                CreateNugetPackages,
                DependsOn(BuildSolution),
                ForEach(
                    "src/Ironclad.Client/Ironclad.Client.csproj", 
                    "src/Ironclad.Console/Ironclad.Console.csproj", 
                    "src/tests/Ironclad.Tests.Sdk/Ironclad.Tests.Sdk.csproj"),
                project => Run("dotnet", $"pack {project} -c Release -o ../../{(project.StartsWith("src/tests") ? "../" : "") + ArtifactsFolder} --no-build"));

            Target(
                PublishNugetPackages,
                DependsOn(DetectEnvironment, CreateNugetPackages, TestDockerImage),
                () =>
                {
                    var packagesToPublish = Directory.GetFiles(ArtifactsFolder, "*.nupkg", SearchOption.TopDirectoryOnly);
                    Console.WriteLine($"Found packages to publish: {string.Join("; ", packagesToPublish)}");

                    if (isPullRequest)
                    {
                        Console.WriteLine("Build is pull request. Packages will not be published.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(nugetServer) || string.IsNullOrWhiteSpace(nugetApiKey))
                    {
                        Console.WriteLine("NuGet settings not specified. Packages will not be published.");
                        return;
                    }

                    foreach (var packageToPublish in packagesToPublish)
                    {
                        Run("dotnet", $"nuget push {packageToPublish} -s {nugetServer} -k {nugetApiKey}", noEcho: true);
                    }
                });

            Target(
                PublishDockerImage,
                DependsOn(DetectEnvironment, TestDockerImage),
                () =>
                {
                    if (isPullRequest)
                    {
                        Console.WriteLine("Build is pull request. Docker images will not be published.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(dockerRegistry) || string.IsNullOrWhiteSpace(dockerUsername) || string.IsNullOrWhiteSpace(dockerPassword))
                    {
                        Console.WriteLine("Docker settings not specified. Docker images will not be published.");
                        return;
                    }

                    Run("docker", $"login {dockerRegistry} -u {dockerUsername} -p {dockerPassword}");
                    Run("docker", $"tag ironclad:latest {dockerRegistry}/ironclad:{dockerTag}");
                    Run("docker", $"push {dockerRegistry}/ironclad:{dockerTag}");
                });

            Target(
                    PublishAll,
                    DependsOn(PublishNugetPackages, PublishDockerImage));

            Target(
                    "default",
                    DependsOn(PublishNugetPackages, PublishDockerImage));

            RunTargets(args);
        }

        private class Settings
        {
            // travis specific
            public bool? TRAVIS { get; set; }
            public string TRAVIS_PULL_REQUEST { get; set; }
            public string TRAVIS_TAG { get; set; }

            public BuildSettings BUILD_SERVER { get; set; }

            public class BuildSettings
            {
                public NugetSettings NUGET { get; set; }
                public DockerSettings DOCKER { get; set; }

                public class NugetSettings
                {
                    public string BETA_SERVER { get; set; }
                    public string BETA_API_KEY { get; set; }
                    public string SERVER { get; set; }
                    public string API_KEY { get; set; }
                }

                public class DockerSettings
                {
                    public string BETA_REGISTRY { get; set; }
                    public string BETA_USERNAME { get; set; }
                    public string BETA_PASSWORD { get; set; }
                    public string REGISTRY { get; set; }
                    public string USERNAME { get; set; }
                    public string PASSWORD { get; set; }
                }
            }
        }
    }
}
