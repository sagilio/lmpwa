using LammpsWithAngle.Static;
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
            get => (int) _data[0];
            set => _data[0] = value;
        }

        public double Chain
        {
            get => _data[1];
            set => _data[1] = value;
        }

        public int Type
        {
            get => (int) _data[2];
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
            if (Config.Mode is "atomic")
            {
                return string.Join("       ", new []{Id, Type, X, Y, Z});
            }
            return string.Join("       ", _data);
        }

        public static Atom Parse(string line)
        {
            string[] data = line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new Atom(data.Select(double.Parse));
        }

        public static Atom Parse(IEnumerable<string> data)
        {
            return new (data.Select(double.Parse));
        }
    }
}