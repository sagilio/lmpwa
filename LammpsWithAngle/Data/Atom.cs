using System;
using System.Collections.Generic;
using System.Linq;

namespace LammpsWithAngle.Data
{
    public class Atom
    {
        public Atom()
        {
        }

        private Atom(IEnumerable<double> data)
        {
            _data = data as double[] ?? data.ToArray();
        }

        private readonly double[] _data = new double[7];
        public double[] Data => _data;

        public int Id
        {
            get => (int)_data[0];
            set => _data[0] = value;
        }

        public int Chain
        {
            get => (int)_data[1];
            set => _data[1] = value;
        }

        public int Type
        {
            get => (int)_data[2];
            set => _data[2] = value;
        }

        public double Charge
        {
            get => _data[3];
            set => _data[3] = value;
        }

        public double X
        {
            get => _data[4];
            set => _data[4] = value;
        }

        public double Y
        {
            get => _data[5];
            set => _data[5] = value;
        }

        public double Z
        {
            get => _data[6];
            set => _data[6] = value;
        }

        public override string ToString()
        {
            return string.Join("    ", Id.ToString(), Chain.ToString(), Type.ToString(),
                Charge.ToString("F8"), X.ToString("F8"), Y.ToString("F8"), Z.ToString("F8"));
        }

        public static Atom Parse(string line, string mode = "atomic")
        {
            string[] data = line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return Parse(data, mode);
        }

        public static Atom Parse(IEnumerable<string> data, string mode = "atomic")
        {
            if (mode is "full")
            {
                return new Atom(data.Select(double.Parse));
            }

            string[] dataArray = data as string[] ?? data.ToArray();
            if (dataArray.Length is not 5)
            {
                throw new InvalidOperationException("atomic mod need 5 data in one row.");
            }

            var atom = new Atom
            {
                Id = int.Parse(dataArray[0]),
                Type = int.Parse(dataArray[1]),
                X = double.Parse(dataArray[2]),
                Y = double.Parse(dataArray[3]),
                Z = double.Parse(dataArray[4])
            };
            return atom;
        }
    }
}