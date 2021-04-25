using System.Collections.Generic;
using LammpsWithAngle.Data;

namespace LammpsWithAngle
{
    public class LammpsData
    {
        public int AtomCount => Atoms.Count;
        public int BondCount => Bonds.Count;
        public int AngleCount => Angles.Count;
        public int AtomTypeCount { get; set; }
        public int BondTypeCount { get; set; }
        public int AngleTypeCount { get; set; }

        public int Dihedrals { get; set; }
        public int Impropers { get; set; }

        public int ChainsCount { get; set; }

        public int WaterCount { get; set; }
        public int MethaneCount { get; set; }
        public int TotalCount => WaterCount + MethaneCount;

        public double Xlo { get; set; }
        public double Xhi { get; set; }
        public double Ylo { get; set; }
        public double Yhi { get; set; }
        public double Zlo { get; set; }
        public double Zhi { get; set; }
        public double Xy { get; set; }
        public double Xz { get; set; }
        public double Yz { get; set; }

        public IDictionary<int, double> Masses = new Dictionary<int, double>();

        public List<Atom> Atoms { get; set; } = new List<Atom>();
        public List<Bond> Bonds { get; set; } = new List<Bond>();
        public List<Angle> Angles { get; set; } = new List<Angle>();
    }
}