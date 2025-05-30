using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;

namespace BoltPhaseChecker
{
    public class BoltOrderIssue
    {
        public int BoltId { get; set; }
        public int BoltPhase { get; set; } 
        public int PhaseTo { get; set; }
        public int PhaseWith { get; set; }
        public bool NeedsFix { get; set; }

        public int PartId { get; set; }
        public int PartPhase { get; set; } 
        public int AssemblyPhase { get; set; }
        public bool PartNeedsFix { get; set; }


        public int Phase { get; set; }
        public int StartNumber { get; set; }

        public int Expected { get; set; }
        public int Found { get; set; }




        public BoltOrderIssue(int id, int boltPhase, int phaseTo, int phaseWith, bool needsFix)
        {

            BoltId = id;
            BoltPhase = boltPhase;
            PhaseTo = phaseTo;
            PhaseWith = phaseWith;
            NeedsFix = needsFix;

            PartId = id;
            PartPhase = boltPhase;
            AssemblyPhase = phaseTo;
            PartNeedsFix = needsFix;

            PartId = id;
            Phase = boltPhase;
            StartNumber = phaseTo;
            NeedsFix = needsFix;



        }



    }

    public class BoltPhaseCheckerLogic
    {
        private readonly Model _model;

        public BoltPhaseCheckerLogic()
        {
            _model = new Model();
        }
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
            var selObj = _model.SelectModelObject(new Identifier(id));
            return selObj as Part;
        }
        public List<BoltOrderIssue> GetPartOrderIssues()
        {
            var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var partsEnum = selector.GetSelectedObjects();
            var list = new List<BoltOrderIssue>();

            while (partsEnum.MoveNext())
            {
                if (!(partsEnum.Current is Part part)) continue;
                if (!part.GetPhase(out Phase partPhase)) continue;

                var asm = part.GetAssembly() as Assembly;
                if (asm == null) continue;
                if (!asm.GetPhase(out Phase asmPhase)) continue;

                bool needsFix = partPhase.PhaseNumber != asmPhase.PhaseNumber;

                list.Add(new BoltOrderIssue(
                    id: part.Identifier.ID,
                    boltPhase: partPhase.PhaseNumber,
                    phaseTo: asmPhase.PhaseNumber,
                    phaseWith: 0,           // unused for parts
                    needsFix: needsFix
                ));
            }

            return list;
        }
        public bool FixPartPhase(int partId, out string message)
        {
            var selObj = _model.SelectModelObject(new Identifier(partId));
            if (!(selObj is Part part))
            {
                message = "Could not retrieve Part.";
                return false;
            }

            if (!part.GetPhase(out Phase partPhase))
            {
                message = "Could not read part phase.";
                return false;
            }

            var asmObj = part.GetAssembly();
            if (!(asmObj is Assembly asm))
            {
                message = "Part not in an assembly.";
                return false;
            }

            if (!asm.GetPhase(out Phase asmPhase))
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

        public List<BoltOrderIssue> GetStartNumberIssues()
        {
            var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var partsEnum = selector.GetSelectedObjects();
            var list = new List<BoltOrderIssue>();

            while (partsEnum.MoveNext())
            {
                if (!(partsEnum.Current is Part part)) continue;
                if (!part.GetPhase(out Phase phase)) continue;

                int expectedStartNumber = phase.PhaseNumber * 1000 + 1;
                int actualStartNumber = part.PartNumber.StartNumber;

                bool needsFix = actualStartNumber != expectedStartNumber;

                list.Add(new BoltOrderIssue(
                    id: part.Identifier.ID,
                    boltPhase: actualStartNumber,      // przechowuje tu aktualny StartNumber
                    phaseTo: expectedStartNumber,      // oczekiwany StartNumber
                    phaseWith: phase.PhaseNumber,      // faza
                    needsFix: needsFix
                ));
            }

            return list;
        }
        public bool FixStartNumber(int partId, out string message)
        {
            var selObj = _model.SelectModelObject(new Identifier(partId));
            if (!(selObj is Part part))
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


        // --- Bolting order/phase logic ---
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

                bool needsFix;

                if (phasesEqual)
                {
                    // Everything is correct, do not flag
                    needsFix = false;
                }
                else
                {
                    // Old logic: flag if order wrong, or order ok but phase not matching
                    needsFix = (!orderOK) || (orderOK && boltPh != withPh);
                }

                list.Add(new BoltOrderIssue(id, boltPh, toPh, withPh, needsFix));
            }
            return list;
        }


        public BoltGroup GetBoltById(int id)
        {
            return _model.SelectModelObject(new Identifier(id)) as BoltGroup;
        }

        public bool SwapBoltParts(int boltId, Dictionary<int, int> seqIdx)
        {
            var bolt = _model.SelectModelObject(new Identifier(boltId)) as BoltGroup;
            if (bolt == null) return false;

            var a = bolt.PartToBoltTo;
            var b = bolt.PartToBeBolted;
            if (a == null || b == null) return false;

            int phA = GetPhase(a);
            int phB = GetPhase(b);
            int boltPh = GetPhase(bolt);

            bool orderOK = seqIdx.ContainsKey(phA) && seqIdx.ContainsKey(phB) && seqIdx[phA] < seqIdx[phB];
            bool phaseOK = (boltPh == phB);

            if (!orderOK)
            {
                // Swap and set phase to the later phase
                bolt.PartToBoltTo = b;
                bolt.PartToBeBolted = a;

                int laterPhase = seqIdx[phA] > seqIdx[phB] ? phA : phB;
                bolt.SetPhase(new Phase { PhaseNumber = laterPhase });
            }
            else if (!phaseOK)
            {
                // Set bolt phase to "withPh"
                bolt.SetPhase(new Phase { PhaseNumber = phB });
            }
            else
            {
                return true; // Nothing to do
            }

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
    }
}
