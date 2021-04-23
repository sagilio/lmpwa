using System;
using LammpsWithAngle.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LammpsWithAngle.Static;

namespace LammpsWithAngle
{
    public static class LammpsDataExtensions
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static LammpsData CompleteBondAndAngle(this LammpsData lammpsData, bool large27)
        {
            lammpsData.Bonds.Clear();
            lammpsData.Angles.Clear();

            var atoms = new List<Atom>();
            var anotherAtoms = large27 ? lammpsData.Atoms.Large27(lammpsData) : lammpsData.Atoms;

            var nowAngleId = 0;
            var nowAtomId = 0;
            var nowChainId= 0;
            var nowBondId = 0;
            foreach (Atom atomO in lammpsData.Atoms.Where(a => a.Type == (int) AtomType.O))
            {
                nowAtomId++;
                nowChainId++;
                int nowAtomOId = nowAtomId;
                atoms.Add(new Atom
                {
                    Id = nowAtomId,
                    Chain = nowChainId,
                    Type = (int) AtomType.O,
                    Charge = Potential.O,
                    X = atomO.X,
                    Y = atomO.Y,
                    Z = atomO.Z
                });

                foreach (var atomH in anotherAtoms.Where(a => a.Type == (int) AtomType.H))
                {
                    double distanceOH = Math.Sqrt(
                        (atomO.X - atomH.X) * (atomO.X - atomH.X) +
                        (atomO.Y - atomH.Y) * (atomO.Y - atomH.Y) +
                        (atomO.Z - atomH.Z) * (atomO.Z - atomH.Z));

                    if (distanceOH is <= 0.5 or >= 1.05)
                    {
                        continue;
                    }

                    nowAtomId++;
                    atoms.Add(new Atom
                    {
                        Id = nowAtomId,
                        Chain = nowChainId,
                        Type = (int) AtomType.H,
                        Charge = Potential.H,
                        X = atomH.X,
                        Y = atomH.Y,
                        Z = atomH.Z
                    });

                    nowBondId++;
                    lammpsData.Bonds.Add(new Bond
                    {
                        Id = nowBondId,
                        Type = (int) AtomType.O,
                        LinkedAtomId = nowAtomOId,
                        AtomId = nowAtomId
                    });
                }

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.O,
                    LastAtomId = nowAtomId - 1,
                    OxygenAtomId = nowAtomOId,
                    AtomId = nowAtomId
                });
                lammpsData.WaterCount++;
            }

            foreach (Atom atomC in lammpsData.Atoms.Where(a => a.Type == (int) AtomType.C))
            {
                nowAtomId++;
                nowChainId++;
                int nowAtomCId = nowAtomId;
                atoms.Add(new Atom
                {
                    Id = nowAtomId,
                    Chain = nowChainId,
                    Type = (int) AtomType.C,
                    Charge = Potential.C,
                    X = atomC.X,
                    Y = atomC.Y,
                    Z = atomC.Z
                });

                foreach (var atomH in anotherAtoms.Where(a => a.Type == (int) AtomType.H))
                {
                    double distanceCH = Math.Sqrt(
                        (atomC.X - atomH.X) * (atomC.X - atomH.X) +
                        (atomC.Y - atomH.Y) * (atomC.Y - atomH.Y) +
                        (atomC.Z - atomH.Z) * (atomC.Z - atomH.Z));

                    if (distanceCH is <= 0.5 or >= 1.2)
                    {
                        continue;
                    }

                    nowAtomId++;
                    atoms.Add(new Atom
                    {
                        Id = nowAtomId,
                        Chain = nowChainId,
                        Type = (int) AtomType.H_CH4,
                        Charge = Potential.H_CH4,
                        X = atomH.X,
                        Y = atomH.Y,
                        Z = atomH.Z
                    });

                    nowBondId++;
                    lammpsData.Bonds.Add(new Bond
                    {
                        Id = nowBondId,
                        Type = (int) AtomType.H,
                        LinkedAtomId = nowAtomCId,
                        AtomId = nowAtomId
                    });
                }

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.H,
                    LastAtomId = nowAtomId - 1,
                    OxygenAtomId = nowAtomCId,
                    AtomId = nowAtomId - 0
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.H,
                    LastAtomId = nowAtomId - 2,
                    OxygenAtomId = nowAtomCId,
                    AtomId = nowAtomId - 0
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.H,
                    LastAtomId = nowAtomId - 3,
                    OxygenAtomId = nowAtomCId,
                    AtomId = nowAtomId - 0
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.H,
                    LastAtomId = nowAtomId - 3,
                    OxygenAtomId = nowAtomCId,
                    AtomId = nowAtomId - 1
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.H,
                    LastAtomId = nowAtomId - 2,
                    OxygenAtomId = nowAtomCId,
                    AtomId = nowAtomId - 1
                });

                nowAngleId++;
                lammpsData.Angles.Add(new Angle
                {
                    Id = nowAngleId,
                    Type = (int) AtomType.H,
                    LastAtomId = nowAtomId - 2,
                    OxygenAtomId = nowAtomCId,
                    AtomId = nowAtomId - 3
                });

                lammpsData.MethaneCount++;
            }

            lammpsData.Atoms = atoms;
            lammpsData.AtomTypeCount = 4;
            lammpsData.BondTypeCount = 2;
            lammpsData.AngleTypeCount = 2;
            return lammpsData;
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
    }
}