using System;
using System.Windows.Forms;

namespace BoltPhaseChecker
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Button btnLoadSeq;
        private System.Windows.Forms.Button btnHighlightSelected;
        private System.Windows.Forms.DataGridView grdOrder;
        private System.Windows.Forms.TextBox txtLog;

        private CheckBox chkBoltOrder;
        private CheckBox chkPartPhases;
        private CheckBox chkStartNumbers;
        private CheckBox chkPrelims;

        private Button btnUnifiedCheck;
        private Button btnUnifiedFix;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void SetupUnifiedGrid()
        {
            grdOrder.Columns.Clear();

            // Kolumna checkbox z sortowaniem
            var chkCol = new DataGridViewCheckBoxColumn
            {
                HeaderText = "✔",
                Name = "colCheck",
                Width = 60,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            grdOrder.Columns.Add(chkCol);

            // Kolumna ID
            grdOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "Part/Bolt ID",
                Width = 100,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            // Kolumna z błędami
            grdOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colError",
                HeaderText = "Error Details",
                Width = 380,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            // Kolumna z propozycją poprawki
            grdOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colFix",
                HeaderText = "Suggested Fix",
                Width = 250,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            // Opcjonalnie: włączenie sortowania na całej siatce
            grdOrder.AllowUserToOrderColumns = true;
            grdOrder.AllowUserToResizeColumns = true;
            grdOrder.AllowUserToResizeRows = false;
        }

        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = ((CheckBox)sender).Checked;

            chkBoltOrder.Checked = isChecked;
            chkPartPhases.Checked = isChecked;
            chkStartNumbers.Checked = isChecked;
            chkPrelims.Checked = isChecked;
        }

        private void InitializeComponent()
        {
            this.chkPrelims = new CheckBox
            {
                Text = "Check Prelims",
                Left = 280,
                Top = 10,
                Width = 160,
            };
            this.Controls.Add(chkPrelims);

            /// 
            CheckBox chkSelectAll = new CheckBox
            {
                Text = "Select all",
                Left = 480,
                Top = 10,
                Width = 160
            };
            chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;
            this.Controls.Add(chkSelectAll);
            ///

            this.MinimumSize = new System.Drawing.Size(800, 600);
            // === Inicjalizacja komponentów ===
            int checkboxWidth = 160;

            this.chkPartPhases = new CheckBox { Text = "Check part phases", Left = 10, Top = 10, Width = checkboxWidth };
            this.chkBoltOrder = new CheckBox { Text = "Check bolt order", Left = 10, Top = 35, Width = checkboxWidth };
            this.chkStartNumbers = new CheckBox { Text = "Check start numbers", Left = 10, Top = 60, Width = checkboxWidth };

            this.btnUnifiedCheck = new Button { Text = "Check" };
            this.btnUnifiedFix = new Button { Text = "Fix" };
            this.btnLoadSeq = new Button { Text = "Load Sequence" };
            this.btnHighlightSelected = new Button { Text = "Highlight Selected" };

            this.grdOrder = new DataGridView();
            this.txtLog = new TextBox();

            // === Layout ===
            int marginLeft = 10;
            int top = 10;
            int checkboxSpacing = 25;

            // Checkboxy (kolumna)
            chkPartPhases.Location = new System.Drawing.Point(marginLeft, top);
            chkStartNumbers.Location = new System.Drawing.Point(marginLeft, top + checkboxSpacing);
            chkBoltOrder.Location = new System.Drawing.Point(marginLeft, top + 2 * checkboxSpacing);


            // Przyciskowy rząd
            int buttonTop = top + 3 * checkboxSpacing + 10;
            int buttonSpacing = 140;

            btnUnifiedCheck.Location = new System.Drawing.Point(marginLeft + buttonSpacing * 0, buttonTop);
            btnUnifiedFix.Location = new System.Drawing.Point(marginLeft + buttonSpacing * 1, buttonTop);
            btnLoadSeq.Location = new System.Drawing.Point(marginLeft + buttonSpacing * 2, buttonTop);
            btnHighlightSelected.Location = new System.Drawing.Point(marginLeft + buttonSpacing * 3, buttonTop);

            btnUnifiedCheck.Size = btnUnifiedFix.Size = btnLoadSeq.Size = btnHighlightSelected.Size = new System.Drawing.Size(130, 28);

            // === Grid ===
            int gridTop = buttonTop + 40;
            grdOrder.AllowUserToAddRows = false;
            grdOrder.AllowUserToDeleteRows = false;
            grdOrder.RowHeadersVisible = false;
            grdOrder.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grdOrder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right ;
            grdOrder.Location = new System.Drawing.Point(12, gridTop);
            grdOrder.Name = "grdOrder";
            grdOrder.Height = 300;
            grdOrder.Width = this.ClientSize.Width - 24;
            //grdOrder.Size = new System.Drawing.Size(200, 100);
            grdOrder.CurrentCellDirtyStateChanged += new System.EventHandler(this.grdOrder_CurrentCellDirtyStateChanged);

            // === Log ===
            int logTop = gridTop + 310;


            txtLog.Dock = DockStyle.Bottom;
            txtLog.Height = 120;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Name = "txtLog";

            // === Zdarzenia ===
            btnUnifiedCheck.Click += btnUnifiedCheck_Click;
            btnUnifiedFix.Click += btnUnifiedFix_Click;
            btnLoadSeq.Click += btnLoadSeq_Click;
            btnHighlightSelected.Click += btnHighlightSelected_Click;

            // === Dodanie kontrolek ===
            this.Controls.Add(chkPartPhases);
            this.Controls.Add(chkStartNumbers);
            this.Controls.Add(chkBoltOrder);

            this.Controls.Add(btnUnifiedCheck);
            this.Controls.Add(btnUnifiedFix);
            this.Controls.Add(btnLoadSeq);
            this.Controls.Add(btnHighlightSelected);

            this.Controls.Add(grdOrder);
            this.Controls.Add(txtLog);

            // === MainForm ===
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.ClientSize = new System.Drawing.Size(824, logTop + 140);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Name = "MainForm";
            this.Text = "BoltPhaseChecker";
            this.Load += MainForm_Load;
        }
    }
}
