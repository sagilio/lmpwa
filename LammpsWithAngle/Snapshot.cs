using LammpsWithAngle.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LammpsWithAngle
{
    public class Snapshot
    {
        public int AtomId { get; set; }
        public int BondId { get; set; }
        public int AngleId { get; set; }
        public List<Atom> Atoms { get; set; } = new();
        public List<Bond> Bonds { get; set; } = new();
        public List<Angle> Angles { get; set; } = new();

        public static Snapshot SyncSnapshotStatus(Snapshot snapshot, Snapshot snapshotFrom)
        {
            snapshot.AtomId = snapshotFrom.AtomId;
            snapshot.BondId = snapshotFrom.BondId;
            snapshot.AngleId = snapshotFrom.AngleId;
            snapshot.Atoms.AddRange(snapshotFrom.Atoms);
            snapshot.Bonds.AddRange(snapshotFrom.Bonds);
            snapshot.Angles.AddRange(snapshotFrom.Angles);
            return snapshot;
        }
    }
}