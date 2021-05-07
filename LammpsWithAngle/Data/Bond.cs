using System;
using System.Collections.Generic;
using System.Linq;

namespace LammpsWithAngle.Data
{
    public class Bond
    {
        public Bond()
        {
        }

        private Bond(IEnumerable<double> data)
        {
            _data = data as double[] ?? data.ToArray();
        }

        private readonly double[] _data = new double[4];
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

        public int AtomId1
        {
            get => (int) _data[2];
            set => _data[2] = value;
        }

        public int AtomId2
        {
            get => (int) _data[3];
            set => _data[3] = value;
        }
        
        public override string ToString()
        {
            return string.Join("    ", _data);
        }

        public static Bond Parse(string line)
        {
            string[] data = line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new Bond(data.Select(double.Parse));
        }

        public static Bond Parse(IEnumerable<string> data)
        {
            return new (data.Select(double.Parse));
        }
    }
}