using System;
using System.Collections.Generic;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;

namespace BoltPhaseChecker
{
    // === MODELE BŁĘDÓW ===

    public abstract class IssueBase
    {
        public int PartId { get; set; }
        public bool NeedsFix { get; set; }
    }

    public class BoltOrderIssue : IssueBase
    {
        public int BoltPhase { get; set; }
        public int PhaseTo { get; set; }
        public int PhaseWith { get; set; }

        public int BoltId => PartId;
    }

    public class PartPhaseIssue : IssueBase
    {
        public int PartPhase { get; set; }
        public int AssemblyPhase { get; set; }
    }

    public class StartNumberIssue : IssueBase
    {
        public int Phase { get; set; }
        public int StartNumber { get; set; }
        public int ExpectedStartNumber { get; set; }
    }

    public class PrelimIssue
    {
        public int PartId { get; set; }
        public string Profile { get; set; }
        public string PrelimMark { get; set; }
        public bool NeedsFix { get; set; }
    }




    // === LOGIKA ===

    public class BoltPhaseCheckerLogic
    {
        private readonly Model _model = new Model();

        public bool CheckModelConnection(out string message)
        {
            if (_model.GetConnectionStatus())
            {
                message = "Tekla connected.";
                return true;
            }
            else
            {
                message = "Tekla not connected.";
                return false;
            }
        }

        public Part GetPartById(int id)
        {
            return _model.SelectModelObject(new Identifier(id)) as Part;
        }

        public BoltGroup GetBoltById(int id)
        {
            return _model.SelectModelObject(new Identifier(id)) as BoltGroup;
        }

        public List<PartPhaseIssue> GetPartOrderIssues()
        {
            var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var partsEnum = selector.GetSelectedObjects();
            var list = new List<PartPhaseIssue>();

            while (partsEnum.MoveNext())
            {
                if (!(partsEnum.Current is Part part)) continue;
                if (!part.GetPhase(out Phase partPhase)) continue;

                var asm = part.GetAssembly() as Assembly;
                if (asm == null || !asm.GetPhase(out Phase asmPhase)) continue;

                bool needsFix = partPhase.PhaseNumber != asmPhase.PhaseNumber;

                list.Add(new PartPhaseIssue
                {
                    PartId = part.Identifier.ID,
                    PartPhase = partPhase.PhaseNumber,
                    AssemblyPhase = asmPhase.PhaseNumber,
                    NeedsFix = needsFix
                });
            }

            return list;
        }

        public bool FixPartPhase(int partId, out string message)
        {
            var part = GetPartById(partId);
            if (part == null)
            {
                message = "Could not retrieve Part.";
                return false;
            }

            if (!part.GetPhase(out Phase partPhase))
            {
                message = "Could not read part phase.";
                return false;
            }

            var asm = part.GetAssembly() as Assembly;
            if (asm == null || !asm.GetPhase(out Phase asmPhase))
            {
                message = "Could not read assembly phase.";
                return false;
            }

            part.SetPhase(asmPhase);
            if (!part.Modify())
            {
                message = "Modify() failed.";
                return false;
            }

            message = $"Phase set to {asmPhase.PhaseNumber}.";
            return true;
        }

        public List<StartNumberIssue> GetStartNumberIssues()
        {
            var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var partsEnum = selector.GetSelectedObjects();
            var list = new List<StartNumberIssue>();

            while (partsEnum.MoveNext())
            {
                if (!(partsEnum.Current is Part part)) continue;
                if (!part.GetPhase(out Phase phase)) continue;

                int expected = phase.PhaseNumber * 1000 + 1;
                int found = part.PartNumber.StartNumber;

                bool needsFix = found != expected;

                list.Add(new StartNumberIssue
                {
                    PartId = part.Identifier.ID,
                    Phase = phase.PhaseNumber,
                    StartNumber = found,
                    ExpectedStartNumber = expected,
                    NeedsFix = needsFix
                });
            }

            return list;
        }

        public bool FixStartNumber(int partId, out string message)
        {
            var part = GetPartById(partId);
            if (part == null)
            {
                message = "Could not retrieve Part.";
                return false;
            }

            if (!part.GetPhase(out Phase phase))
            {
                message = "Could not retrieve phase.";
                return false;
            }

            int newStart = phase.PhaseNumber * 1000 + 1;
            part.PartNumber.StartNumber = newStart;

            if (!part.Modify())
            {
                message = "Modify() failed.";
                return false;
            }

            message = $"Set Start Number to {newStart}.";
            return true;
        }

        public List<BoltOrderIssue> GetBoltOrderIssues(Dictionary<int, int> seqIdx)
        {
            var sel = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var it = sel.GetSelectedObjects();
            var list = new List<BoltOrderIssue>();

            while (it.MoveNext())
            {
                if (!(it.Current is BoltGroup bolt)) continue;

                int id = bolt.Identifier.ID;
                int boltPh = GetPhase(bolt);
                int toPh = GetPhase(bolt.PartToBoltTo);
                int withPh = GetPhase(bolt.PartToBeBolted);

                bool inSeq = seqIdx.ContainsKey(toPh) && seqIdx.ContainsKey(withPh);
                bool orderOK = inSeq && seqIdx[toPh] < seqIdx[withPh];
                bool phasesEqual = (toPh == withPh) && (boltPh == withPh);

                bool needsFix = !phasesEqual && (!orderOK || (orderOK && boltPh != withPh));

                list.Add(new BoltOrderIssue
                {
                    PartId = id,
                    BoltPhase = boltPh,
                    PhaseTo = toPh,
                    PhaseWith = withPh,
                    NeedsFix = needsFix
                });
            }
            return list;
        }

        public bool SwapBoltParts(int boltId, Dictionary<int, int> seqIdx)
        {
            var bolt = GetBoltById(boltId);
            if (bolt == null) return false;

            var a = bolt.PartToBoltTo;
            var b = bolt.PartToBeBolted;
            if (a == null || b == null) return false;

            int phA = GetPhase(a);
            int phB = GetPhase(b);
            int boltPh = GetPhase(bolt);

            if (!seqIdx.ContainsKey(phA) || !seqIdx.ContainsKey(phB)) return false;

            bool orderOK = seqIdx[phA] < seqIdx[phB];
            bool phaseOK = boltPh == phB;

            if (!orderOK)
            {
                bolt.PartToBoltTo = b;
                bolt.PartToBeBolted = a;

                int later = seqIdx[phA] > seqIdx[phB] ? phA : phB;
                bolt.SetPhase(new Phase { PhaseNumber = later });
            }
            else if (!phaseOK)
            {
                bolt.SetPhase(new Phase { PhaseNumber = phB });
            }
            else return true;

            return bolt.Modify();
        }

        private int GetPhase(ModelObject o)
        {
            return o != null && o.GetPhase(out Phase p) ? p.PhaseNumber : -1;
        }

        private int GetPhase(BoltGroup b)
        {
            return b != null && b.GetPhase(out Phase p) ? p.PhaseNumber : -1;
        }

        public List<PrelimIssue> GetPrelimIssues()
        {
            var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var partsEnum = selector.GetSelectedObjects();
            var list = new List<PrelimIssue>();

            while (partsEnum.MoveNext())
            {
                if (!(partsEnum.Current is Part part)) continue;

                string profile = string.Empty;
                part.GetReportProperty("PROFILE", ref profile);

                // Sprawdź, czy to profilowana stal (dopasuj swoją funkcję)
                if (!IsColdFormedProfile(profile)) continue;

                string prelimMark = string.Empty;
                part.GetUserProperty("PRELIM_MARK", ref prelimMark);

                bool needsFix = string.IsNullOrWhiteSpace(prelimMark);

                list.Add(new PrelimIssue
                {
                    PartId = part.Identifier.ID,
                    Profile = profile,
                    PrelimMark = prelimMark,
                    NeedsFix = needsFix
                });
            }

            return list;
        }

        private bool IsColdFormedProfile(string profile)
        {
            if (string.IsNullOrWhiteSpace(profile)) return false;

            profile = profile.ToUpperInvariant();

            return profile.StartsWith("C") || profile.StartsWith("Z") ||
                   profile.StartsWith("HAT") || profile.Contains("CFS");
        }


    }
}