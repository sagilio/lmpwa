using System;
using System.Collections.Generic;
using System.Linq;

namespace LammpsWithAngle.Data
{
    public class Angle
    {
        public Angle()
        {
        }

        private Angle(IEnumerable<double> data)
        {
            _data = data as double[] ?? data.ToArray();
        }

        private readonly double[] _data = new double[5];
        public double[] Data => _data;

        public double Id
        {
            get => _data[0];
            set => _data[0] = value;
        }

        public double Type
        {
            get => _data[1];
            set => _data[1] = value;
        }

        public double LastAtomId
        {
            get => _data[2];
            set => _data[2] = value;
        }

        public double OxygenAtomId
        {
            get => _data[3];
            set => _data[3] = value;
        }

        public double AtomId
        {
            get => _data[4];
            set => _data[4] = value;
        }

        public override string ToString()
        {
            return string.Join("       ", _data);
        }

        public static Angle Parse(string line)
        {
            string[] data = line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new Angle(data.Select(double.Parse));
        }

        public static Angle Parse(IEnumerable<string> data)
        {
            return new Angle(data.Select(double.Parse));
        }
    }
}