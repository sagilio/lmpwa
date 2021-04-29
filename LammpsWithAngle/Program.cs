﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using Serilog.Events;
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
                .MinimumLevel.Information()
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

        [Option("--read-mode", Description = "Determine read source file mode. (atomic)")]
        public string ReadMode { get; set; } = "atomic";
        
        [Option("-wm|--water-model", Description = "Water model name will be used. (SPC)")]
        public string WaterModel { get; set; } = "SPC";

        [Option("-l|--large27", Description = "Determine whether large 27 times. (false)")]
        public bool Large27 { get; set; } = false;

        [Option("-rsa|--remove-surface-angle", Description = "Determine whether remove surface angle. (false)")]
        public bool RemoveSurfaceAngle { get; set; } = false;

        [Option("-fia|--fix-invalid-axis", Description = "Determine whether fix invalid axis. (false)")]
        public bool FixInvalidAxis { get; set; } = false;

        private async Task<int> OnExecuteAsync()
        {
            var appOptions = new AppOptions
            {
                SourceFile = SourceFile,
                TargetFile = TargetFile,
                ReadMode = ReadMode,
                WaterModel = WaterModel,
                Large27 = Large27,
                RemoveSurfaceAngles = RemoveSurfaceAngle,
                FixInvalidAxis = FixInvalidAxis
            };

            Log.Logger.Information(appOptions.ToString());

            try
            {
                LammpsData lammpsData = await LammpsDataSerializer.DeserializeFromFileAsync(appOptions.SourceFile, new LammpsDataDeserializeOptions
                {
                    Mode = appOptions.ReadMode
                });

                Log.Logger.Information("Start to complete.");
                DateTimeOffset startTome = DateTimeOffset.Now;
                lammpsData = lammpsData.CompleteBondAndAngle(appOptions.WaterModel, appOptions.Large27, appOptions.FixInvalidAxis);
                Log.Logger.Information($"End to complete. Cost {DateTimeOffset.Now - startTome}.");

                await LammpsDataSerializer.SerializeToFileAsync(lammpsData, appOptions.TargetFile, new LammpsDataSerializeOptions
                {
                    RemoveSurfaceAngles = appOptions.RemoveSurfaceAngles
                });
            }
            catch (Exception e)
            {
                Log.Logger.Error($"Error: {e}");
                return 1;
            }
            return 0;
        }

        public class AppOptions
        {
            public string SourceFile { get; set; } = "source.lmp";
            public string TargetFile { get; set; } = "result.lmp";
            public string ReadMode { get; set; } = "atomic";
            public string WaterModel { get; set; } = "SPC";
            public bool Large27 { get; set; }
            public bool RemoveSurfaceAngles { get; set; }
            public bool FixInvalidAxis { get; set; }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Will read from {SourceFile}, and write to {TargetFile}.");
                builder.AppendLine($"Read mode is {ReadMode}.");
                builder.AppendLine($"Water model is {WaterModel}.");
                builder.AppendLine($"Large 27 times is {Large27}.");
                builder.AppendLine($"Remove surface angles is {RemoveSurfaceAngles}.");
                builder.Append($"Fix invalid axis is {FixInvalidAxis}.");
                return builder.ToString();
            }
        }
    }
}
