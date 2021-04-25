using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LammpsWithAngle.Static;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

// ReSharper disable ClassNeverInstantiated.Global

namespace LammpsWithAngle
{
    [Command(Name = "lmpwa", Description = "Add angle info to lmp file.")]
    [HelpOption("-h|--help")]
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: Path.Combine("logs", @"log.txt"),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 1_000_000,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(10))
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        [Argument(0, Description = "The source lammps data file. (source.lmp)")]
        public string SourceFile { get; set; } = "source.lmp";

        [Argument(1, Description = "The lammps data file with angle dat. (result.lmp)")]
        public string TargetFile { get; set; } = "result.lmp";

        [Option("-l|--large27", Description = "Determine whether large 27 times. (false)")]
        public bool Large27 { get; set; } = false;

        [Option("-rsa|--remove-surface-angle", Description = "Determine whether remove surface angle. (false)")]
        public bool RemoveSurfaceAngle { get; set; } = false;

        private async Task OnExecuteAsync()
        {
            var appOptions = new AppOptions
            {
                Large27 = Large27,
                RemoveSurfaceAngles = RemoveSurfaceAngle,
                SourceFile = SourceFile,
                TargetFile = TargetFile
            };

            Log.Logger.Information(appOptions.ToString());

            LammpsData lammpsData = await LammpsDataSerializer.DeserializeFromFileAsync(appOptions.SourceFile);
            lammpsData = lammpsData.CompleteBondAndAngle(appOptions.Large27);
            Log.Logger.Information("End to complete.");

            await LammpsDataSerializer.SerializeToFileAsync(lammpsData, appOptions.TargetFile, new SerializeOptions
            {
                RemoveSurfaceAngles = appOptions.RemoveSurfaceAngles
            });
        }

        public class AppOptions
        {
            public bool Large27 { get; set; }
            public bool RemoveSurfaceAngles { get; set; }
            public string SourceFile { get; set; } = "source.lmp";
            public string TargetFile { get; set; } = "result.lmp";

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Will read from {SourceFile}, and write to {TargetFile}.");
                builder.AppendLine($"Large 27 times is {Large27}.");
                builder.AppendLine($"Remove surface angles is {RemoveSurfaceAngles}.");
                return builder.ToString();
            }
        }
    }
}
