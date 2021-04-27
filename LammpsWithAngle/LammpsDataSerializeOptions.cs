namespace LammpsWithAngle
{
    public class LammpsDataSerializeOptions
    {
        public bool RemoveSurfaceAngles { get; set; }
        public string NewLine { get; set; } = "\n";
        public string Mode { get; set; } = "all";
    }
}