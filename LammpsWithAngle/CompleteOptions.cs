using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LammpsWithAngle
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class CompleteOptions
    {
        public string WaterModel { get; init; } = "SPC";
        public bool Large27 { get; init; }
        public bool FixInvalidAxis { get; set; }
        public bool RemoveToLittleDistanceAtoms { get; set; }
        public double MinO_H { get; set; } = 0.9;
        public double MaxO_H { get; set; } = 1.1;
        public double MinC_H { get; set; } = 0.9;
        public double MaxC_H { get; set; } = 1.2;
        public double ErrorO_O { get; set; } = 2.5;
        public double ErrorC_O { get; set; } = 3.5;
        public double ErrorC_C { get; set; } = 5;
        public double ErrorC_H { get; set; } = 1.5;
        public double ErrorO_H_CH4 { get; set; } = 2.6;
        public double ErrorH_H_CH4 { get; set; } = 2.3;
        public double ErrorH_H { get; set; } = 1;
        public double ErrorH_CH4_H_CH4 { get; set; } = 1;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{nameof(WaterModel)} is {WaterModel}.");
            builder.AppendLine($"{nameof(Large27)} is {Large27}.");
            builder.AppendLine($"{nameof(FixInvalidAxis)} is {FixInvalidAxis}.");
            builder.AppendLine($"{nameof(RemoveToLittleDistanceAtoms)} is {RemoveToLittleDistanceAtoms}.");
            builder.AppendLine($"Search O-H distance range is from {MinO_H} to {MaxO_H}.");
            builder.AppendLine($"Search C-H distance range is from {MinC_H} to {MaxC_H}.");
            builder.AppendLine($"Allow O-O min distance is from {ErrorO_O}.");
            builder.AppendLine($"Allow C-O min distance is from {ErrorC_O}.");
            builder.AppendLine($"Allow C-C min distance is from {ErrorC_C}.");
            builder.AppendLine($"Allow C-H min distance is from {ErrorC_H}.");
            builder.AppendLine($"Allow O-H_CH4 min distance is from {ErrorO_H_CH4}.");
            builder.AppendLine($"Allow H-H_CH4 min distance is from {ErrorH_H_CH4}.");
            builder.AppendLine($"Allow H-H min distance is from {ErrorH_H}.");
            builder.AppendLine($"Allow H_CH4-H_CH4 min distance is from {ErrorH_CH4_H_CH4}.");
            return builder.ToString();
        }
    }
}