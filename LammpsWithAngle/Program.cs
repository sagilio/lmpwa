using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global
namespace LammpsWithAngle
{
    [Command(Name = "lmpwa", Description = "Add angle info to lmp file.")]
    [HelpOption("-h|--help")]
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await CommandLineApplication.ExecuteAsync<Program>(args);
        }

        [Argument(0, Description = "The source lammps data file. (source.lmp)")]
        public string SourceFile { get; set; } = "source.lmp";

        [Argument(1, Description = "The lammps data file with angle dat. (result.lmp)")]
        public string TargetFile { get; set; } = "result.lmp";

        [Option("--read-mode", Description = "Determine read source file mode. (atomic)")]
        public string ReadMode { get; set; } = "atomic";

        [Option("-rsa|--remove-surface-angle", Description = "Determine whether remove surface angle. (false)")]
        public bool RemoveSurfaceAngle { get; set; } = false;

        [Option("-wm|--water-model", Description = "Water model name will be used. (SPC)")]
        public string WaterModel { get; set; } = "SPC";

        [Option("-l|--large27", Description = "Determine whether large 27 times. (false)")]
        public bool Large27 { get; set; } = false;

        [Option("-fia|--fix-invalid-axis", Description = "Determine whether fix invalid axis. (false)")]
        public bool FixInvalidAxis { get; set; } = false;

        // ReSharper disable once StringLiteralTypo
        [Option("-rtla|--remove-to-little-atoms", Description = "Determine whether delete to little distance atoms. (false)")]
        public bool RemoveToLittleDistanceAtoms { get; set; } = false;

        [Option("--min-O-H", Description = "Search O-H min distance. (0.9)")]
        public double MinO_H { get; set; } = 0.9;

        [Option("--max-O-H", Description = "Search O-H max distance. (1.1)")]
        public double MaxO_H { get; set; } = 1.1;
        
        [Option("--min-C-H", Description = "Search C-H min distance. (0.9)")]
        public double MinC_H { get; set; } = 0.9;
        
        [Option("--max-C-H", Description = "Search C-H max distance. (1.2)")]
        public double MaxC_H { get; set; } = 1.2;
        
        [Option("--error-O-O", Description = "Allow O-O min distance. (2.5)")]
        public double ErrorO_O { get; set; } = 2.5;
        
        [Option("--error-C-O", Description = "Allow C-O min distance. (3.5)")]
        public double ErrorC_O { get; set; } = 3.5;
        
        [Option("--error-C-C", Description = "Allow C-C min distance. (5.0)")]
        public double ErrorC_C { get; set; } = 5;

        [Option("--error-C-H", Description = "Allow C-H min distance. (1.5)")]
        public double ErrorC_H { get; set; } = 1.5;

        [Option("--error-O-H_CH4", Description = "Allow O-C_CH4 min distance. (2.6)")]
        public double ErrorO_H_CH4 { get; set; } = 2.6;

        [Option("--error-H-H_CH4", Description = "Allow H-H_CH4 min distance. (2.3)")]
        public double ErrorH_H_CH4 { get; set; } = 2.3;

        [Option("--error-H-H", Description = "Allow H-H min distance. (1.0)")]
        public double ErrorH_H { get; set; } = 1;

        [Option("--error-H_CH4-H_CH4", Description = "Allow H_CH4-H_CH4 min distance. (1.0)")]
        public double ErrorH_CH4_H_CH4 { get; set; } = 1;

        [Option("-v|--verbose", Description = "Set logger minimum level to verbose. (false)")]
        public bool Verbose { get; set; } = false;

        private async Task<int> OnExecuteAsync()
        {
            var logger = new LoggerConfiguration();

            if (Verbose)
            {
                logger.MinimumLevel.Verbose();
            }
            else
            {
                logger.MinimumLevel.Information();
            }
            
            Log.Logger = logger
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

            var appOptions = new AppOptions
            {
                SourceFile = SourceFile,
                TargetFile = TargetFile,
                ReadMode = ReadMode,
                RemoveSurfaceAngles = RemoveSurfaceAngle,
                CompleteOptions = new CompleteOptions
                {
                    WaterModel = WaterModel,
                    Large27 = Large27,
                    FixInvalidAxis = FixInvalidAxis,
                    RemoveToLittleDistanceAtoms = RemoveToLittleDistanceAtoms,
                    MinO_H = MinO_H, MaxO_H = MaxO_H,
                    MinC_H = MinC_H, MaxC_H = MaxC_H,
                    ErrorO_O = ErrorO_O, ErrorC_O = ErrorC_O, 
                    ErrorC_C = ErrorC_C, ErrorC_H = ErrorC_H,
                    ErrorO_H_CH4 = ErrorO_H_CH4, ErrorH_H_CH4 = ErrorH_H_CH4,
                    ErrorH_H = ErrorH_H, ErrorH_CH4_H_CH4 = ErrorH_CH4_H_CH4
                }
            };

            Log.Logger.Information(appOptions.ToString());

            try
            {
                string? targetDir = Path.GetDirectoryName(TargetFile);
                if (targetDir is not null && Directory.Exists(targetDir) is false)
                {
                    Directory.CreateDirectory(targetDir);
                }

                LammpsData lammpsData = await LammpsDataSerializer.DeserializeFromFileAsync(appOptions.SourceFile, new LammpsDataDeserializeOptions
                {
                    Mode = appOptions.ReadMode
                });

                DateTimeOffset startTome = DateTimeOffset.Now;
                Log.Logger.Information("Start to complete.");
                lammpsData = lammpsData.CompleteBondAndAngle(appOptions.CompleteOptions);
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
            public bool RemoveSurfaceAngles { get; set; }
            public CompleteOptions CompleteOptions { get; set; } = new();

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Will read from {SourceFile}, and write to {TargetFile}.");
                builder.AppendLine($"Read mode is {ReadMode}.");
                builder.AppendLine($"Remove surface angles is {RemoveSurfaceAngles}.");
                builder.AppendLine("CompleteOptions is: ");
                builder.Append(CompleteOptions);
                return builder.ToString();
            }
        }
    }
}
