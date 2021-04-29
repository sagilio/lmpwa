using LammpsWithAngle.Data;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Collections.Generic;

namespace LammpsWithAngle
{
    public static class LammpsDataSerializer
    {
        public static Task SerializeToFileAsync(LammpsData lammpsData, string path)
        {
            return SerializeToFileAsync(lammpsData, path, new LammpsDataSerializeOptions());
        }

        public static async Task SerializeToFileAsync(LammpsData lammpsData, string path, LammpsDataSerializeOptions options)
        {
            Log.Logger.Debug("Start to serialize to file.");
            await using FileStream fileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            await using var writer = new StreamWriter(fileStream);
            string header = SerializeHeader(lammpsData, options);
            await writer.WriteAsync(header);
            Log.Logger.Information("Serialized Header.");
            if (lammpsData.Masses.Count > 0)
            {
                //'原来元素
                //' O   H   C
                //' 1   2   3
                //'定义的元素
                //'Ow  Hw  Cm  Hm
                //'1   2   3   4
                lammpsData.Masses.Add(4, lammpsData.Masses[2]);
                await WriteDataAsync(writer, "Masses", lammpsData.Masses.Select(mass => $"{mass.Key}   {mass.Value}    # {(AtomType) mass.Key}"), options);
                Log.Logger.Debug("Serialize Masses.");
            }
            await WriteDataAsync(writer, $"Atoms  # {options.Mode}", lammpsData.Atoms, options);
            Log.Logger.Debug("Serialized Atoms, count is {0}", lammpsData.AtomCount);
            await WriteDataAsync(writer, "Bonds", lammpsData.Bonds, options);
            Log.Logger.Debug("Serialized Bonds, count is {0}", lammpsData.BondCount);
            await WriteDataAsync(writer, "Angles", lammpsData.Angles, options);
            Log.Logger.Debug("Serialized Angles, count is {0}", lammpsData.AngleCount);
            Log.Logger.Information("Serialized Success, Header is: {1}{0}", header, options.NewLine);
        }

        public static Task<LammpsData> DeserializeFromFileAsync(string path)
        {
            return DeserializeFromFileAsync(path, new LammpsDataDeserializeOptions());
        }

        public static async Task<LammpsData> DeserializeFromFileAsync(string path, LammpsDataDeserializeOptions options)
        {
            if (File.Exists(path) is false)
            {
                throw new InvalidOperationException($"Can find the file {path}");
            }

            LammpsData lammpsData = new();
            VerityData verityData = new();
            string dataType = "Header";
            await using FileStream? fileStream = File.OpenRead(path);
            using var reader = new StreamReader(fileStream);
            while (reader.EndOfStream is false)
            {
                string? line = await reader.ReadLineAsync();
                if (line is null)
                {
                    continue;
                }

                line = RemoveComments(line);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] group = line.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (group.Length is 1)
                {
                    dataType = group.First();
                    continue;
                }

                if (dataType is "Header")
                {
                    lammpsData = DeserializeHeader(lammpsData, line, group, verityData);
                }

                lammpsData = DeserializeData(lammpsData, line, group, dataType, options);
            }

            bool verityResult = Verity(lammpsData, verityData);
            if (verityResult is false)
            {
                throw new InvalidOperationException("Source data format verity error.");
            }

            Log.Logger.Information("Success deserialize, Header is: {1}{0}", 
                SerializeHeader(lammpsData, new LammpsDataSerializeOptions()), Environment.NewLine);
            return lammpsData;
        }

        private static string SerializeHeader(LammpsData lammpsData, LammpsDataSerializeOptions options)
        {
            string newLine = options.NewLine;
            var builder = new StringBuilder();
            #region Comment
            builder.Append("# File created by lmpwa, made by Tang Xianhe.");
            builder.Append(newLine);
            builder.AppendFormat("# {0} water, {1} methene, {2} total", lammpsData.WaterCount, lammpsData.MethaneCount, lammpsData.TotalCount);
            builder.Append(newLine);
            #endregion

            #region Count
            // 4806 atoms
            // 3348 bonds
            // 2538 angles
            // 0 dihedrals
            // 0 impropers
            builder.Append(newLine);
            builder.AppendFormat("{0} atoms", lammpsData.AtomCount);
            builder.Append(newLine);
            builder.AppendFormat("{0} bonds", lammpsData.BondCount);
            builder.Append(newLine);
            builder.AppendFormat("{0} angles", lammpsData.AngleCount);
            builder.Append(newLine);
            builder.AppendFormat("{0} dihedrals", lammpsData.Dihedrals);
            builder.Append(newLine);
            builder.AppendFormat("{0} impropers", lammpsData.Impropers);
            builder.Append(newLine);
            #endregion

            #region Type Count
            // 4 atom types
            // 2 bond types
            // 2 angle types
            builder.Append(newLine);
            builder.AppendFormat("{0} atom types", lammpsData.AtomTypeCount);
            builder.Append(newLine);
            builder.AppendFormat("{0} bond types", lammpsData.BondTypeCount);
            builder.Append(newLine);
            builder.AppendFormat("{0} angle types", lammpsData.AngleTypeCount);
            builder.Append(newLine);
            #endregion

            builder.Append(newLine);

            #region Xlo Xhi
            builder.AppendFormat("{0} {1} xlo xhi", lammpsData.Xlo, lammpsData.Xhi);
            builder.Append(newLine);
            builder.AppendFormat("{0} {1} ylo yhi", lammpsData.Ylo, lammpsData.Yhi);
            builder.Append(newLine);
            builder.AppendFormat("{0} {1} zlo zhi", lammpsData.Zlo, lammpsData.Zhi);
            builder.Append(newLine);
            #endregion

            #region XY XZ YZ
            if (options.RemoveSurfaceAngles is false)
            {
                builder.Append(newLine);
                builder.AppendFormat("{0} {1} {2} xy xz yz", lammpsData.Xy, lammpsData.Xz, lammpsData.Yz);
                builder.Append(newLine);
            }
            #endregion

            return builder.ToString();
        }

        private static LammpsData DeserializeHeader(LammpsData lammpsData, string line, string[] group, VerityData verityData)
        {
            // 0.0 35.0 zlo zhi
            if (group.Length is 4)
            {
                switch (group[2])
                {
                    case "xlo":
                        lammpsData.Xlo = double.Parse(@group[0]);
                        lammpsData.Xhi = double.Parse(@group[1]);
                        return lammpsData;
                    case "ylo":
                        lammpsData.Ylo = double.Parse(@group[0]);
                        lammpsData.Yhi = double.Parse(@group[1]);
                        return lammpsData;
                    case "zlo":
                        lammpsData.Zlo = double.Parse(@group[0]);
                        lammpsData.Zhi = double.Parse(@group[1]);
                        return lammpsData;
                }
            }
            // 0    0    0   xy xz yz
            if (group.Length is 6)
            {
                lammpsData.Xy = double.Parse(group[0]);
                lammpsData.Xz = double.Parse(group[1]);
                lammpsData.Yz = double.Parse(group[2]);
                return lammpsData;
            }
            // 4 atom types
            if (group.Length is 3)
            {
                group[1] = group[1] + " " + group[2];
                switch (group[1])
                {
                    case "atom types":
                        lammpsData.AtomTypeCount = int.Parse(@group[0]);
                        return lammpsData;
                    case "angle types":
                        lammpsData.AngleTypeCount = int.Parse(@group[0]);
                        return lammpsData;
                    case "bond types":
                        lammpsData.BondTypeCount = int.Parse(@group[0]);
                        return lammpsData;
                }
            }
            // 4806 atoms
            // 3348 bonds
            // 2538 angles
            // 0 dihedrals
            // 0 impropers
            if (group.Length is 2)
            {
                int count = int.Parse(group[0]);
                switch (group[1])
                {
                    case "atoms":
                        verityData.AtomCount = count;
                        return lammpsData; 
                    case "bonds":
                        verityData.BondCount = count;
                        return lammpsData; 
                    case "angles":
                        verityData.AngleCount = count;
                        return lammpsData; 
                    case "dihedrals":
                        lammpsData.Dihedrals = count;
                        return lammpsData; 
                    case "impropers":
                        lammpsData.Impropers = count;
                        return lammpsData; 
                }
            }
            return lammpsData;
        }

        private static LammpsData DeserializeData(LammpsData lammpsData, string line, string[] group, string dataType, LammpsDataDeserializeOptions options)
        {
            switch (dataType)
            {
                case "Atoms":
                    lammpsData.Atoms.Add(Atom.Parse(group, options.Mode));
                    break;
                case "Bonds":
                    lammpsData.Bonds.Add(Bond.Parse(group));
                    break;
                case "Angles":
                    lammpsData.Angles.Add(Angle.Parse(group));
                    break;
                case "Masses":
                    lammpsData.Masses.Add(int.Parse(group[0]), double.Parse(group[1]));
                    break;
            }
            return lammpsData;
        }

        private static async Task WriteDataAsync<T>(TextWriter writer, string dataType, IEnumerable<T> contents, LammpsDataSerializeOptions options)
        {
            string newLine = options.NewLine;
            await writer.WriteAsync(newLine);
            await writer.WriteAsync(dataType);
            await writer.WriteAsync(newLine);
            await writer.WriteAsync(newLine);
            foreach (T content in contents)
            {
                await writer.WriteAsync(content?.ToString());
                await writer.WriteAsync(newLine);
            }
        }

        private static bool Verity(LammpsData lammpsData, VerityData verityData)
        {
            if (lammpsData.AtomCount != verityData.AtomCount)
            {
                Log.Logger.Error($"Verity Error: Atom count except {verityData.AtomCount}, actual {lammpsData.AtomCount}");
                return false;
            }
            Log.Logger.Information($"Verity Success: Atom count {verityData.AtomCount}.");

            if (lammpsData.AngleCount != verityData.AngleCount)
            {
                Log.Logger.Error($"Verity Error: Angle count except {verityData.AngleCount}, actual {lammpsData.AngleCount}");
                return false;
            }
            Log.Logger.Information($"Verity Success: Angle count {verityData.AngleCount}.");

            if (lammpsData.BondCount != verityData.BondCount)
            {
                Log.Logger.Error($"Verity Error: Bond count except {verityData.BondCount}, actual {lammpsData.BondCount}");
                return false;
            }
            Log.Logger.Information($"Verity Success: Bond count {verityData.BondCount}.");
            return true;
        }

        private static string RemoveComments(string line)
        {
            int pos = line.IndexOf("#", StringComparison.Ordinal);
            return pos is -1 ? line : line[..pos].Trim();
        }

        private class VerityData
        {
            public int AtomCount { get; set; }
            public int BondCount { get; set; }
            public int AngleCount { get; set; }
        }
    }
}