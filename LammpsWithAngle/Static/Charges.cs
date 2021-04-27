// ReSharper disable InconsistentNaming
using System;

namespace LammpsWithAngle.Static
{
    public class Charges
    {
        public double H { get; set; } = 0.52;
        public double O { get; set; } = -1.04;
        public double C { get; set; } = -0.3744;
        public double H_CH4 { get; set; } = 0.0936;

        public static Charges TiP4P = new()
        {
            H = 0.52,
            O = 1.04,
            C = -0.3744,
            H_CH4 = 0.0936
        };

        public static Charges SPC = new()
        {
            O = -0.82,
            H = 0.41,
            C = -0.3744,
            H_CH4 = 0.0936
        };

        public static Charges SPC_E = new()
        {
            O = -0.8476,
            H = 0.4238,
            C = -0.3744,
            H_CH4 = 0.0936
        };

        public static Charges GetCharges(string modelName)
        {
            return modelName switch
            {
                nameof(TiP4P) => TiP4P,
                nameof(SPC) => SPC,
                nameof(SPC_E) => SPC_E,
                _ => throw new InvalidOperationException($"Water model {modelName} is unsupported.")
            };
        }
    }
}