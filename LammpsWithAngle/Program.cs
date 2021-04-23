using System;
using System.IO;
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

        [Argument(0, Description = "The source lammps data file.")]
        public string SourceFile { get; set; } = "source.lmp";

        [Argument(1, Description = "The lammps data file with angle dat.")]
        public string TargetFile { get; set; } = "result.lmp";

        [Option("-l|--large27", Description = "Determine whether large 27 times.")]
        public bool Large27 { get; set; } = false;

        [Option("-m|--mode", Description = "Determine atom export mode.")]
        public string Mode { get; set; } = Config.Mode;

        private async Task OnExecuteAsync()
        {
            Config.Mode = Mode;
            Log.Logger.Information($"Will read from {SourceFile}, and write to {TargetFile}.");
            Log.Logger.Information($"Atom export mode is {Config.Mode}.");
            LammpsData lammpsData = await LammpsDataSerializer.DeserializeFromFileAsync(SourceFile);
            lammpsData = lammpsData.CompleteBondAndAngle(Large27);
            Log.Logger.Information("End to complete.");
            await LammpsDataSerializer.SerializeToFileAsync(lammpsData, TargetFile);
        }
    }
}
