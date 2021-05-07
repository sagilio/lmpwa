using System;
using LammpsWithAngle.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Serilog;
using System.Threading;
using System.Collections.Immutable;
using System.IO;

namespace LammpsWithAngle
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class LammpsDataExtensions
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static LammpsData CompleteBondAndAngle(this LammpsData lammpsData, CompleteOptions completeOptions)
        {
            var charges = Charges.GetCharges(completeOptions.WaterModel);
            lammpsData.Bonds.Clear();
            lammpsData.Angles.Clear();

            var atoms = new List<Atom>();
            var anotherAtoms = completeOptions.Large27 
                ? lammpsData.Atoms.Large27(lammpsData) 
                : lammpsData.Atoms;

            var atomOs = lammpsData.Atoms
                .Where(a => a.Type == (int) AtomType.O).ToImmutableArray();
            var atomCs = lammpsData.Atoms
                .Where(a => a.Type == (int) AtomType.C).ToImmutableArray();
            var anotherAtomHs = anotherAtoms
                .Where(a => a.Type == (int) AtomType.H).ToImmutableArray();

            var nowAtomId = 0;
            var nowChainId = 0;
            foreach (Atom atomO in atomOs)
            {
                var waterAtoms = new List<Atom>();
                int waterAtomId = nowAtomId;
                int waterChainId = nowChainId;

                waterAtomId++;
                waterChainId++;
                waterAtoms.Add(new Atom
                {
                    Id = waterAtomId,
                    Chain = waterChainId,
                    Type = (int)AtomType.O,
                    Charge = charges.O,
                    X = atomO.X,
                    Y = atomO.Y,
                    Z = atomO.Z
                });

                foreach (var atomH in anotherAtomHs)
                {
                    double distanceOH = atomO.Distance(atomH);

                    if (distanceOH < completeOptions.MinO_H || distanceOH > completeOptions.MaxO_H)
                    {
                        continue;
                    }

                    waterAtomId++;
                    waterAtoms.Add(new Atom
                    {
                        Id = waterAtomId,
                        Chain = waterChainId,
                        Type = (int)AtomType.H,
                        Charge = charges.H,
                        X = atomH.X,
                        Y = atomH.Y,
                        Z = atomH.Z
                    });
                }

                if (waterAtoms.Count is not 3)
                {
                    Log.Logger.Debug("Remove error water atoms of ids: {0}.",
                            string.Join(", ", waterAtoms.Select(a => a.Id)));
                    continue;
                }

                atoms.AddRange(waterAtoms);
                nowAtomId = waterAtomId;
                nowChainId = waterChainId;

                if (nowAtomId % 200 is 0)
                {
                    Log.Logger.Information("Now atom id is {0}", nowAtomId);
                }
            }

            foreach (Atom atomC in atomCs)
            {
                var methaneAtoms = new List<Atom>();
                int methaneAtomId = nowAtomId;
                int methaneChainId = nowChainId;

                methaneAtomId++;
                methaneChainId++;
                methaneAtoms.Add(new Atom
                {
                    Id = methaneAtomId,
                    Chain = methaneChainId,
                    Type = (int)AtomType.C,
                    Charge = charges.C,
                    X = atomC.X,
                    Y = atomC.Y,
                    Z = atomC.Z
                });

                foreach (var atomH in anotherAtomHs)
                {
                    double distanceCH = atomC.Distance(atomH);

                    if (distanceCH < completeOptions.MinC_H || distanceCH > completeOptions.MaxC_H)
                    {
                        continue;
                    }

                    methaneAtomId++;
                    methaneAtoms.Add(new Atom
                    {
                        Id = methaneAtomId,
                        Chain = methaneChainId,
                        Type = (int)AtomType.H_CH4,
                        Charge = charges.H_CH4,
                        X = atomH.X,
                        Y = atomH.Y,
                        Z = atomH.Z
                    });
                }

                if (methaneAtoms.Count is not 5)
                {
                    Log.Logger.Debug("Remove error methane atoms of ids: {0}.",
                        string.Join(", ", methaneAtoms.Select(a => a.Id)));
                    continue;
                }

                atoms.AddRange(methaneAtoms);
                nowAtomId = methaneAtomId;
                nowChainId = methaneChainId;

                if (nowAtomId % 200 is 0)
                {
                    Log.Logger.Information("Now atom id is {0}", nowAtomId);
                }
            }

            if (completeOptions.FixInvalidAxis)
            {
                foreach (var atom in atoms)
                {
                    atom.FixInvalidAxis(lammpsData);
                }
            }

            if (completeOptions.RemoveToLittleDistanceAtoms)
            {
                atoms = atoms.RemoveToLittleDistance(lammpsData, completeOptions);
                lammpsData.Atoms = atoms;
                if (completeOptions.WaterModel is "SPC")
                {
                    lammpsData = lammpsData.Arrange().RemoveInvalidAngle();
                }
            }
            else
            {
                lammpsData.Atoms = atoms;
            }

            lammpsData.ChainsCount = nowChainId;
            lammpsData.AtomTypeCount = 4;
            lammpsData.BondTypeCount = 2;
            lammpsData.AngleTypeCount = 2;
            return lammpsData.Arrange();
        }

        private static double Distance(this Atom atom, Atom anotherAtom)
        {
            return Math.Sqrt(
                (atom.X - anotherAtom.X) * (atom.X - anotherAtom.X) +
                (atom.Y - anotherAtom.Y) * (atom.Y - anotherAtom.Y) +
                (atom.Z - anotherAtom.Z) * (atom.Z - anotherAtom.Z));
        }

        private static List<Atom> Large27(this ICollection<Atom> sourceAtoms, LammpsData lammpsData)
        {
            var atoms = new List<Atom>();
            var nowAtomId = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        foreach (var atom in sourceAtoms)
                        {
                            atoms.Add(new Atom
                            {
                                Id = nowAtomId,
                                Type = atom.Type,
                                X = atom.X + x * (lammpsData.Xhi - lammpsData.Xlo),
                                Y = atom.Y + y * (lammpsData.Yhi - lammpsData.Ylo),
                                Z = atom.Z + z * (lammpsData.Zhi - lammpsData.Zlo)
                            });
                            nowAtomId++;
                        }
                    }
                }
            }

            return atoms;
        }

        private static Atom FixInvalidAxis(this Atom atom, LammpsData lammpsData)
        {
            if (atom.X < 0)
            {
                double oldX = atom.X;
                atom.X += (lammpsData.Xhi - lammpsData.Xlo);
                Log.Logger.Information("Fixed invalid X {0} to {1}",
                    oldX, atom.X);
            }
            if (atom.Y < 0)
            {
                double oldY = atom.Y;
                atom.Y += (lammpsData.Yhi - lammpsData.Ylo);
                Log.Logger.Information("Fixed invalid Y {0} to {1}",
                    oldY, atom.Y);
            }
            if (atom.Z < 0)
            {
                double oldZ = atom.Z;
                atom.Z += (lammpsData.Zhi - lammpsData.Zlo);
                Log.Logger.Information("Fixed invalid Z {0} to {1}",
                    oldZ, atom.Z);
            }
            return atom;
        }

        private static List<Atom> RemoveToLittleDistance(this List<Atom> atoms, LammpsData lammpsData, CompleteOptions completeOptions)
        {
            var brokenChainIds = new List<int>();
            var waterAtom = atoms.Where(a => a.Type == (int)AtomType.O).ToImmutableArray();
            var methaneAtom = atoms.Where(a => a.Type == (int)AtomType.C).ToImmutableArray();
            var waterAtomH = atoms.Where(a => a.Type == (int)AtomType.H).ToImmutableArray();
            var methaneAtomH = atoms.Where(a => a.Type == (int)AtomType.H_CH4).ToImmutableArray();

            int waterCount = waterAtom.Length;
            int methaneCount = methaneAtom.Length;
            int waterHCount = waterAtomH.Length;
            int methaneHCount = methaneAtomH.Length;
            for (var atomIndex = 0; atomIndex < waterCount; atomIndex++)
            {
                // O-O
                for (var anotherAtomIndex = 0; anotherAtomIndex < waterCount; anotherAtomIndex++)
                {
                    if (atomIndex == anotherAtomIndex)
                    {
                        continue;
                    }

                    Atom atomO1 = waterAtom[atomIndex];
                    Atom atomO2 = waterAtom[anotherAtomIndex];
                    double distance = atomO1.Distance(atomO2);

                    if (distance < completeOptions.ErrorO_O)
                    {
                        if (brokenChainIds.Contains(atomO1.Chain) || brokenChainIds.Contains(atomO2.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom O {2} and O {3} is too little, but has removed the another chain {4}.",
                                distance, completeOptions.ErrorO_O, atomO1.Id, atomO2.Id, atomO2.Chain);
                            continue;
                        }
                        Log.Logger.Information("the distance {0} < {1} with atom O {2} and O {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorO_O, atomO1.Id, atomO2.Id, atomO1.Chain);
                        brokenChainIds.Add(atomO1.Chain);
                    }
                }

                // O-C
                for (var methaneAtomIndex = 0; methaneAtomIndex < methaneCount; methaneAtomIndex++)
                {
                    Atom atomO = waterAtom[atomIndex];
                    Atom atomC = methaneAtom[methaneAtomIndex];
                    double distance = atomO.Distance(atomC);

                    if (distance < completeOptions.ErrorC_O)
                    {
                        if (brokenChainIds.Contains(atomO.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom O {2} and C {3} is too little, but has been removed the chain {4}.",
                                distance, completeOptions.ErrorC_O, atomO.Id, atomC.Id, atomO.Chain);
                            continue;
                        }

                        Log.Logger.Information("the distance {0} < {1} with atom O {2} and C {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorC_O, atomO.Id, atomC.Id, atomO.Chain);
                        brokenChainIds.Add(atomO.Chain);
                    }
                }

                // O-H_CH4
                for (var methaneAtomIndex = 0; methaneAtomIndex < methaneCount; methaneAtomIndex++)
                {
                    Atom atomO = waterAtom[atomIndex];
                    Atom atomH_CH4 = methaneAtomH[methaneAtomIndex];
                    double distance = atomO.Distance(atomH_CH4);

                    if (distance < completeOptions.ErrorO_H_CH4)
                    {
                        if (brokenChainIds.Contains(atomO.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom O {2} and H_CH4 {3} is too little, but has been removed the chain {4}.",
                                distance, completeOptions.ErrorO_H_CH4, atomO.Id, atomH_CH4.Id, atomO.Chain);
                            continue;
                        }

                        Log.Logger.Information("the distance {0} < {1} with atom O {2} and H_CH4 {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorO_H_CH4, atomO.Id, atomH_CH4.Id, atomO.Chain);
                        brokenChainIds.Add(atomO.Chain);
                    }
                }
            }

            for (var methaneAtomIndex = 0; methaneAtomIndex < methaneCount; methaneAtomIndex++)
            {
                // C-C
                for (var anotherMethaneAtomIndex = 0; anotherMethaneAtomIndex < methaneCount; anotherMethaneAtomIndex++)
                {
                    if (methaneAtomIndex == anotherMethaneAtomIndex)
                    {
                        continue;
                    }

                    Atom atomC1 = methaneAtom[methaneAtomIndex];
                    Atom atomC2 = methaneAtom[anotherMethaneAtomIndex];
                    double distance = atomC1.Distance(atomC2);

                    if (distance < completeOptions.ErrorC_C)
                    {
                        if (brokenChainIds.Contains(atomC1.Chain) || brokenChainIds.Contains(atomC2.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom C {2} and C {3} is too little, but has removed the another chain {4}.",
                                distance, completeOptions.ErrorC_C, atomC1.Id, atomC2.Id, atomC2.Chain);
                            continue;
                        }
                        Log.Logger.Information("the distance {0} < {1} with atom C {2} and C {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorC_C, atomC1.Id, atomC2.Id, atomC1.Chain);
                        brokenChainIds.Add(atomC1.Chain);
                    }
                }

                // C-H
                for (var waterAtomHIndex = 0; waterAtomHIndex < waterHCount; waterAtomHIndex++)
                {
                    Atom atomH = waterAtomH[waterAtomHIndex];
                    Atom atomC = methaneAtom[methaneAtomIndex];
                    double distance = atomH.Distance(atomC);

                    if (distance < completeOptions.ErrorC_H)
                    {
                        if (brokenChainIds.Contains(atomH.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom H {2} and C {3} is too little, but has removed the chain {4}.",
                                distance, completeOptions.ErrorC_H, atomH.Id, atomC.Id, atomH.Chain);
                            continue;
                        }
                        Log.Logger.Information("the distance {0} < {1} with atom H {2} and C {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorC_H, atomH.Id, atomC.Id, atomH.Chain);
                        brokenChainIds.Add(atomH.Chain);
                    }
                }
            }

            for (var waterAtomHIndex = 0; waterAtomHIndex < waterHCount; waterAtomHIndex++)
            {
                // H-H
                for (var anotherWaterAtomHIndex = 0; anotherWaterAtomHIndex < waterHCount; anotherWaterAtomHIndex++)
                {
                    if (waterAtomHIndex == anotherWaterAtomHIndex)
                    {
                        continue;
                    }

                    Atom atomH1 = waterAtomH[waterAtomHIndex];
                    Atom atomH2 = waterAtomH[anotherWaterAtomHIndex];
                    double distance = atomH1.Distance(atomH2);

                    if (distance < completeOptions.ErrorH_H)
                    {
                        if (brokenChainIds.Contains(atomH1.Chain) || brokenChainIds.Contains(atomH2.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom H {2} and H {3} is too little, but has removed the chain {4}.",
                                distance, completeOptions.ErrorH_H, atomH1.Id, atomH2.Id, atomH1.Chain);
                            continue;
                        }
                        Log.Logger.Information("the distance {0} < {1} with atom H {2} and H {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorH_H, atomH1.Id, atomH2.Id, atomH1.Chain);
                        brokenChainIds.Add(atomH1.Chain);
                    }
                }

                // H-H_CH4
                for (var methaneAtomHIndex = 0; methaneAtomHIndex < methaneHCount; methaneAtomHIndex++)
                {
                    Atom atomH1 = waterAtomH[waterAtomHIndex];
                    Atom atomH2 = methaneAtomH[methaneAtomHIndex];
                    double distance = atomH1.Distance(atomH2);

                    if (distance < completeOptions.ErrorH_H_CH4)
                    {
                        if (brokenChainIds.Contains(atomH1.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom H {2} and H_CH4 {3} is too little, but has removed the chain {4}.",
                                distance, completeOptions.ErrorH_H_CH4, atomH1.Id, atomH2.Id, atomH1.Chain);
                            continue;
                        }
                        Log.Logger.Information("the distance {0} < {1} with atom H {2} and H_CH4 {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorH_H_CH4, atomH1.Id, atomH2.Id, atomH1.Chain);
                        brokenChainIds.Add(atomH1.Chain);
                    }
                }
            }

            
            for (var methaneAtomHIndex = 0; methaneAtomHIndex < methaneHCount; methaneAtomHIndex++)
            {
                // H_CH4-H_CH4
                for (var anotherMethaneAtomHIndex = 0; anotherMethaneAtomHIndex < methaneHCount; anotherMethaneAtomHIndex++)
                {
                    if (methaneAtomHIndex == anotherMethaneAtomHIndex)
                    {
                        continue;
                    }

                    Atom atomH1 = methaneAtomH[methaneAtomHIndex];
                    Atom atomH2 = methaneAtomH[anotherMethaneAtomHIndex];
                    double distance = atomH1.Distance(atomH2);

                    if (distance < completeOptions.ErrorH_CH4_H_CH4)
                    {
                        if (brokenChainIds.Contains(atomH1.Chain) || brokenChainIds.Contains(atomH2.Chain))
                        {
                            Log.Logger.Information("the distance {0} < {1} with atom H_CH4 {2} and H_CH4 {3} is too little, but has removed the chain {4}.",
                                distance, completeOptions.ErrorH_CH4_H_CH4, atomH1.Id, atomH2.Id, atomH1.Chain);
                            continue;
                        }
                        Log.Logger.Information("the distance {0} < {1} with atom H_CH4 {2} and H_CH4 {3} is too little, will remove the chain {4}.",
                            distance, completeOptions.ErrorH_CH4_H_CH4, atomH1.Id, atomH2.Id, atomH1.Chain);
                        brokenChainIds.Add(atomH1.Chain);
                    }
                }
            }

            lammpsData.ChainsCount -= brokenChainIds.Count;
            return atoms.RemoveChains(brokenChainIds);
        }

        private static LammpsData RemoveInvalidAngle(this LammpsData lammpsData)
        {
            var invalidAngleChainIds = new List<int>();
            foreach (var angle in lammpsData.Angles.Where(a => a.Type is 1))
            {
                Atom atom1 = lammpsData.Atoms[angle.AtomId1 - 1];
                Atom atom2 = lammpsData.Atoms[angle.AtomId2 - 1];
                Atom atom3 = lammpsData.Atoms[angle.AtomId3 - 1];
                double AB = atom1.Distance(atom2);
                double BC = atom2.Distance(atom3);
                double AC = atom1.Distance(atom3);

                double cosAtoms = (AB * AB + BC * BC - AC * AC) / (2 * AB * BC);
                double angleNumber = 180 * Math.Acos(cosAtoms) / Math.PI;
                if (angleNumber is <= 104 or >= 112)
                {
                    invalidAngleChainIds.Add(atom1.Chain);
                    Log.Logger.Information($"{angleNumber} is not in range 104 to 112, will remove chain {atom1.Chain}");
                }
            }
            lammpsData.Atoms.RemoveChains(invalidAngleChainIds);
            return lammpsData;
        }

        private static List<Atom> RemoveChains(this List<Atom> atoms, ICollection<int> chainIds)
        {
            var removeAtoms = atoms.Where(a => chainIds.Contains(a.Chain)).ToList();
            foreach (var atom in removeAtoms)
            {
                atoms.Remove(atom);
                Log.Logger.Debug("Deleted atom {0} {1} and relate bounds and angles.", (AtomType)atom.Type, atom.Id);
            }
            return atoms;
        }

        private static LammpsData Arrange(this LammpsData lammpsData)
        {
            lammpsData.Bonds.Clear();
            lammpsData.Angles.Clear();
            lammpsData.WaterCount = 0;
            lammpsData.MethaneCount = 0;

            var atomId = 1;
            var chainId = 1;
            var bondId = 1;
            var angleId = 1;

            var nowAtomOId = 0;
            var nowAtomCId = 0;
            var lastWaterAtomId = 0;

            foreach (var atom in lammpsData.Atoms)
            {
                switch (atom.Type)
                {
                    case (int)AtomType.O:
                        nowAtomOId = atomId;
                        atom.Id = atomId;
                        atom.Chain = chainId;
                        atomId++;
                        break;
                    case (int)AtomType.H:
                        atom.Id = atomId;
                        atom.Chain = chainId;
                        // add O-H bond
                        SetWaterBond();
                        // add H-O-H angle
                        if (atomId % 3 == 0)
                        {
                            lastWaterAtomId = atomId;
                            SetWaterAngle();
                            atom.Chain = chainId++;
                            lammpsData.WaterCount++;
                        }
                        atomId++;
                        break;
                    case (int)AtomType.C:
                        nowAtomCId = atomId;
                        atom.Id = atomId;
                        atom.Chain = chainId;
                        atomId++;
                        break;
                    case (int)AtomType.H_CH4:
                        atom.Id = atomId;
                        atom.Chain = chainId;
                        // add C-H bond
                        SetMethaneBond();
                        // add H-C-H angle
                        if ((atomId - lastWaterAtomId) % 5 == 0)
                        {
                            SetMethaneAngle();
                            atom.Chain = chainId++;
                            lammpsData.MethaneCount++;
                        }
                        atomId++;
                        break;
                }
            }

            void SetWaterBond()
            {
                Bond bond = new()
                {
                    Id = bondId++,
                    Type = 1,
                    AtomId1 = nowAtomOId,
                    AtomId2 = atomId
                };
                lammpsData.Bonds.Add(bond);
            }

            void SetMethaneBond()
            {
                Bond bond = new()
                {
                    Id = bondId++,
                    Type = 2,
                    AtomId1 = nowAtomCId,
                    AtomId2 = atomId
                };
                lammpsData.Bonds.Add(bond);
            }

            void SetWaterAngle()
            {
                Angle angle = new()
                {
                    Id = angleId++,
                    Type = 1,
                    AtomId1 = atomId - 1,
                    AtomId2 = nowAtomOId,
                    AtomId3 = atomId
                };
                lammpsData.Angles.Add(angle);
            }

            void SetMethaneAngle()
            {
                Angle angle = new()
                {
                    Id = angleId++,
                    Type = 2,
                    AtomId1 = atomId - 1,
                    AtomId2 = nowAtomCId,
                    AtomId3 = atomId - 0
                };
                lammpsData.Angles.Add(angle);

                angle = new Angle
                {
                    Id = angleId++,
                    Type = 2,
                    AtomId1 = atomId - 2,
                    AtomId2 = nowAtomCId,
                    AtomId3 = atomId - 0
                };
                lammpsData.Angles.Add(angle);

                angle = new Angle
                {
                    Id = angleId++,
                    Type = 2,
                    AtomId1 = atomId - 3,
                    AtomId2 = nowAtomCId,
                    AtomId3 = atomId - 0
                };
                lammpsData.Angles.Add(angle);

                angle = new Angle
                {
                    Id = angleId++,
                    Type = 2,
                    AtomId1 = atomId - 3,
                    AtomId2 = nowAtomCId,
                    AtomId3 = atomId - 1
                };
                lammpsData.Angles.Add(angle);

                angle = new Angle
                {
                    Id = angleId++,
                    Type = 2,
                    AtomId1 = atomId - 2,
                    AtomId2 = nowAtomCId,
                    AtomId3 = atomId - 1
                };
                lammpsData.Angles.Add(angle);

                angle = new Angle
                {
                    Id = angleId++,
                    Type = 2,
                    AtomId1 = atomId - 2,
                    AtomId2 = nowAtomCId,
                    AtomId3 = atomId - 3
                };
                lammpsData.Angles.Add(angle);
            }

            return lammpsData;
        }
    }
}