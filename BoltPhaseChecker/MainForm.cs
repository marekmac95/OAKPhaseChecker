using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;

namespace BoltPhaseChecker
{
    public partial class MainForm : Form
    {
        private readonly BoltPhaseCheckerLogic _logic;
        private List<int> _invalidPartIds;
        private List<int> _orderBoltIds;
        private List<int> _startNumberErrorIds;

        private List<int> _buildSequence = new List<int>();
        private Dictionary<int, int> _seqIndex = new Dictionary<int, int>();
        private readonly OpenFileDialog _ofdCsv = new OpenFileDialog() { Filter = "CSV|*.csv" };
        private List<int> _boltOrderErrorIds = new List<int>();
        private enum CheckMode { None, Bolt, Part }
        private CheckMode _currentMode = CheckMode.None;
        List<int> _prelimErrorIds;


        public MainForm()
        {
            InitializeComponent();
            _logic = new BoltPhaseCheckerLogic();
            _invalidPartIds = new List<int>();
            _orderBoltIds = new List<int>();
            Log("App started.");
            if (_logic.CheckModelConnection(out string msg))
            {
                Log("Tekla connected.");
            }
            else
            {
                Log("Tekla not connected.");
                MessageBox.Show("❌ Tekla Structures is not connected.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }


            // For mass checkboxing
            this.grdOrder.CurrentCellDirtyStateChanged += new System.EventHandler(this.grdOrder_CurrentCellDirtyStateChanged);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Handles form resizing
            int gridTop = btnHighlightSelected.Bottom + 12;
            grdOrder.Top = gridTop;
            txtLog.Top = grdOrder.Bottom + 10;

            Resize += (s, ea) =>
            {
                grdOrder.Width = ClientSize.Width - 24;
                txtLog.Width = ClientSize.Width - 24;
                grdOrder.Height = ClientSize.Height - txtLog.Height - gridTop - 40;
                txtLog.Top = grdOrder.Bottom + 10;
            };
        }

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        // --- Load build-sequence CSV ---
        private void btnLoadSeq_Click(object sender, EventArgs e)
        {
            if (_ofdCsv.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                _buildSequence.Clear();
                foreach (var line in File.ReadAllLines(_ofdCsv.FileName))
                {
                    var tokens = line
                        .Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var t in tokens)
                        if (int.TryParse(t, out int ph))
                            _buildSequence.Add(ph);
                }
                
                _buildSequence = _buildSequence
                .Where(x => x >= 0)
                .Distinct()
                .ToList(); // usuwa duplikaty, ale zachowuje kolejność wystąpienia
                
                _seqIndex = _buildSequence
                    .Select((phase, idx) => new { phase, idx })
                    .ToDictionary(x => x.phase, x => x.idx);

                Log($"Build sequence loaded: {string.Join(", ", _buildSequence)}");
            }
            catch (Exception ex)
            {
                Log($"Error reading CSV: {ex.Message}");
            }
        }



        // --- Part phase checking & fixing ---
        private void CheckPartPhases()
        {
            Log("Checking selected parts against assembly phases...");
            

            var issues = _logic.GetPartOrderIssues();

            foreach (var issue in issues)
            {
                bool needsFix = issue.NeedsFix;
                string err = needsFix
                    ? $"Part Phase: {issue.PartPhase}, Assembly Phase: {issue.AssemblyPhase}"
                    : string.Empty;

                string fix = needsFix
                    ? $"Set phase to {issue.AssemblyPhase}"
                    : string.Empty;

                grdOrder.Rows.Add(needsFix, issue.PartId, err, fix);
            }

            _invalidPartIds = issues
                .Where(x => x.NeedsFix)
                .Select(x => x.PartId)
                .ToList();

            Log($"{_invalidPartIds.Count} parts need fixing.");
            _currentMode = CheckMode.Part;
        }

        private void FixPartPhases()
        {
            if (_invalidPartIds.Count == 0)
            {
                Log("No part errors to fix.");
                return;
            }

            Log("Fixing part phases to match assemblies...");
            foreach (int id in _invalidPartIds)
            {
                bool ok = _logic.FixPartPhase(id, out string msg);
                Log(ok
                    ? $"✔ Part {id}: {msg}"
                    : $"✘ Part {id}: {msg}");
            }
            Log("Finished updating part phases.");
        }

        // --- Part start numbers checking & fixing ---
        private void CheckStartNumbers()
        {
            Log("Checking part start numbers...");
            

            var issues = _logic.GetStartNumberIssues();

            foreach (var issue in issues)
            {
                bool needsFix = issue.NeedsFix;

                string err = needsFix
                    ? $"Phase: {issue.Phase}, Actual Start: {issue.StartNumber}, Expected Start: {issue.ExpectedStartNumber}"
                    : string.Empty;

                string fix = needsFix
                    ? $"Set start number to {issue.ExpectedStartNumber}"
                    : string.Empty;

                grdOrder.Rows.Add(needsFix, issue.PartId, err, fix);
            }

            _startNumberErrorIds = issues
                .Where(i => i.NeedsFix)
                .Select(i => i.PartId)
                .ToList();

            Log($"Found {_startNumberErrorIds.Count} parts with incorrect start numbers.");
            _currentMode = CheckMode.Part;
        }

        private void FixStartNumbers()
        {
            if (_startNumberErrorIds.Count == 0)
            {
                Log("No start number issues to fix.");
                return;
            }

            Log("Fixing start numbers...");
            foreach (int id in _startNumberErrorIds)
            {
                bool ok = _logic.FixStartNumber(id, out string msg);
                Log(ok
                    ? $"✔ Part {id}: {msg}"
                    : $"✘ Part {id}: {msg}");
            }
            Log("Done fixing start numbers.");
        }

        // --- Bolting order checking & fixing ---
        private bool CheckBoltOrder()
        {
            if (_seqIndex.Count == 0)
            {
                Log("⚠ Load a build-sequence CSV first.");
                MessageBox.Show("⚠ Load a build-sequence CSV first.");
                return false;  
            }

            Log("Checking bolting order against build sequence...");

            var issues = _logic.GetBoltOrderIssues(_seqIndex);

            foreach (var issue in issues)
            {
                bool needsFix = issue.NeedsFix;

                string err = needsFix
                    ? $"Bolt phase: {issue.BoltPhase}, Main Part phase: {issue.PhaseTo},  Secondary part phase: {issue.PhaseWith}"
                    : string.Empty;

                string fix = string.Empty;

                if (!needsFix)
                {
                    fix = "";
                }
                else
                {
                    bool toHasIndex = _seqIndex.ContainsKey(issue.PhaseTo);
                    bool withHasIndex = _seqIndex.ContainsKey(issue.PhaseWith);

                    if (!toHasIndex || !withHasIndex)
                    {
                        fix = "⚠ Phase not in CSV — cannot determine order, check part phases.";
                    }
                    else
                    {
                        int idxTo = _seqIndex[issue.PhaseTo];
                        int idxWith = _seqIndex[issue.PhaseWith];

                        bool orderCorrect = idxTo < idxWith;
                        bool phaseMatches = issue.BoltPhase == issue.PhaseWith;

                        if (!orderCorrect && !phaseMatches)
                            fix = "Swap bolt order and set bolt phase";
                        else if (!orderCorrect)
                            fix = "Swap bolt order";
                        else if (!phaseMatches)
                            fix = $"Set bolt phase to {issue.PhaseWith}";
                    }
                }

                grdOrder.Rows.Add(needsFix, issue.BoltId, err, fix);
            }

            _orderBoltIds = issues
                .Where(x => x.NeedsFix)
                .Select(x => x.BoltId)
                .ToList();

            Log($"{_orderBoltIds.Count} bolt groups need fixing (pre-ticked).");
            _currentMode = CheckMode.Bolt;

            return true; // <- operacja zakończona poprawnie
        }


        private void FixBoltOrder()
        {
            if (_orderBoltIds.Count == 0)
            {
                Log("No bolt-order issues to fix.");
                return;
            }

            int fixedCount = 0;

            foreach (DataGridViewRow row in grdOrder.Rows)
            {
                if (row.Cells[0].Value is bool chk && chk)
                {
                    int id = Convert.ToInt32(row.Cells[1].Value);

                    // Sprawdzenie, czy to ID jest na liście śrub do poprawy
                    if (_orderBoltIds.Contains(id))
                    {
                        if (_logic.SwapBoltParts(id, _seqIndex))
                            fixedCount++;
                    }
                }
            }

            Log($"Bolting order fixed on {fixedCount} bolt groups.");
        }

        // --- Check prelims ---



        private void CheckPrelims()
        {
            Log("Checking PRELIM assignments...");

            var issues = _logic.GetPrelimIssues();

            foreach (var issue in issues)
            {
                string err = issue.NeedsFix
                    ? $"Profile: {issue.Profile}, PRELIM: <empty>"
                    : string.Empty;

                string fix = issue.NeedsFix
                    ? "Set PRELIM = YES"
                    : string.Empty;

                grdOrder.Rows.Add(issue.NeedsFix, issue.PartId, err, fix);
            }

            _prelimErrorIds = issues
                .Where(i => i.NeedsFix)
                .Select(i => i.PartId)
                .ToList();

            Log($"Found {_prelimErrorIds.Count} parts missing PRELIM assignment.");
            _currentMode = CheckMode.Part;
        }





        // --- Highlight Selected Bolts and parts ---
        private void btnHighlightSelected_Click(object sender, EventArgs e)
        {
            var selector = new Tekla.Structures.Model.UI.ModelObjectSelector();
            var list = new ArrayList();

            foreach (DataGridViewRow row in grdOrder.Rows)
            {
                bool marked = row.Cells[0].Value is bool b && b;
                if (!marked) continue;

                int id = Convert.ToInt32(row.Cells[1].Value);

                // Próbujemy pobrać jako część
                var part = _logic.GetPartById(id);
                if (part != null)
                {
                    list.Add(part);
                    continue;
                }

                // Próbujemy pobrać jako śrubę
                var bolt = _logic.GetBoltById(id);
                if (bolt != null)
                {
                    list.Add(bolt);
                }
            }

            if (list.Count == 0)
            {
                Log("Nothing ticked to highlight.");
                return;
            }

            selector.Select(list);
            Log($"{list.Count} model objects highlighted.");
        }
        // --- Functions for buttons ---
        private void btnUnifiedCheck_Click(object sender, EventArgs e)
        {
            grdOrder.Rows.Clear();
            SetupUnifiedGrid();


            if (chkPartPhases.Checked) CheckPartPhases();
            if (chkStartNumbers.Checked) CheckStartNumbers();
            // Jeśli checkbox od bolta jest zaznaczony, ale nie wczytano sekwencji — nie kontynuujemy
            if (chkBoltOrder.Checked && !CheckBoltOrder())
                return;

        }

        private void btnUnifiedFix_Click(object sender, EventArgs e)
        {
            if (chkPartPhases.Checked) FixPartPhases();
            if (chkStartNumbers.Checked) FixStartNumbers();
            if (chkBoltOrder.Checked) FixBoltOrder();

        }



        // --- Mass checkbox for selected rows ---
        private void grdOrder_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grdOrder.CurrentCell is DataGridViewCheckBoxCell && grdOrder.IsCurrentCellDirty)
            {
                grdOrder.CommitEdit(DataGridViewDataErrorContexts.Commit);
                var newValue = grdOrder.CurrentCell.Value is bool b && b;
                foreach (DataGridViewRow row in grdOrder.SelectedRows)
                {
                    if (row.Cells[0] is DataGridViewCheckBoxCell)
                        row.Cells[0].Value = newValue;
                }
            }
        }


    }
}
