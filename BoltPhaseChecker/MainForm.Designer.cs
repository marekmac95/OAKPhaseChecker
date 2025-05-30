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

            grdOrder.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "✔", Width = 40 });
            grdOrder.Columns.Add("Id", "Part/Bolt ID");
            grdOrder.Columns.Add("ErrorInfo", "Error Details");
            grdOrder.Columns.Add("FixSuggestion", "Suggested Fix");

            grdOrder.Columns[0].Width = 40;
            grdOrder.Columns[1].Width = 100;
            grdOrder.Columns[2].Width = 400;
            grdOrder.Columns[3].Width = 250;
        }

        private void InitializeComponent()
        {

            this.btnUnifiedCheck = new Button { Text = "Check", Left = 10, Top = 100 };
            this.btnUnifiedFix = new Button { Text = "Fix", Left = 100, Top = 100 };

            this.Controls.Add(this.btnUnifiedCheck);
            this.Controls.Add(this.btnUnifiedFix);

            this.btnUnifiedCheck.Click += btnUnifiedCheck_Click;
            this.btnUnifiedFix.Click += btnUnifiedFix_Click;




            ///
            // Checkboxy
            this.chkBoltOrder = new CheckBox { Text = "Check bolt order", Left = 10, Top = 10 };
            this.chkPartPhases = new CheckBox { Text = "Check part phases", Left = 10, Top = 35 };
            this.chkStartNumbers = new CheckBox { Text = "Check start numbers", Left = 10, Top = 60 };

            // Nowe przyciski


            // Dodaj do formularza
            Controls.Add(this.chkBoltOrder);
            Controls.Add(this.chkPartPhases);
            Controls.Add(this.chkStartNumbers);
            Controls.Add(btnUnifiedCheck);
            Controls.Add(btnUnifiedFix);

            ///
            this.btnLoadSeq = new System.Windows.Forms.Button();
            this.btnHighlightSelected = new System.Windows.Forms.Button();
            this.grdOrder = new System.Windows.Forms.DataGridView();
            this.txtLog = new System.Windows.Forms.TextBox();

            var colChk = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            var colBolt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            var colBoltPh = new System.Windows.Forms.DataGridViewTextBoxColumn();
            var colFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            var colTo = new System.Windows.Forms.DataGridViewTextBoxColumn();



            ((System.ComponentModel.ISupportInitialize)(this.grdOrder)).BeginInit();
            this.SuspendLayout();


            ///// Button location
            int marginLeft = 220;
            int w = 130;  // szerokość przycisku (przykład)
            int pad = 8;  // odstęp między przyciskami
            int top = 12; // początkowa pozycja pionowa
            int h = 28;

            // 1. wiersz
            btnLoadSeq.Location = new System.Drawing.Point(marginLeft + (w + pad) * 0, top);

            // 2. wiersz
            top += 30 + pad;  // przesuwamy w dół o wysokość przycisku + odstęp


            // 3. wiersz
            top += 30 + pad;
            btnHighlightSelected.Location = new System.Drawing.Point(marginLeft + (w + pad) * 0, top);

            /////


            this.btnLoadSeq.Name = "btnLoadSeq";
            this.btnHighlightSelected.Name = "btnHighlightSelected";

            this.btnLoadSeq.Size = new System.Drawing.Size(130, 28);
            this.btnHighlightSelected.Size = new System.Drawing.Size(w, h);

            this.btnLoadSeq.Text = "Load Bolt Sequence";
            this.btnHighlightSelected.Text = "Highlight Selected";


            this.btnLoadSeq.Click += new System.EventHandler(this.btnLoadSeq_Click);
            this.btnHighlightSelected.Click += new System.EventHandler(this.btnHighlightSelected_Click);

            this.btnLoadSeq.UseVisualStyleBackColor = true;

            // ---------- grdOrder ----------
            int gridTop = top + h + 12;
            this.grdOrder.AllowUserToAddRows = false;
            this.grdOrder.AllowUserToDeleteRows = false;
            this.grdOrder.RowHeadersVisible = false;
            this.grdOrder.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdOrder.Anchor = (System.Windows.Forms.AnchorStyles.Top |
                                    System.Windows.Forms.AnchorStyles.Left |
                                    System.Windows.Forms.AnchorStyles.Right |
                                    System.Windows.Forms.AnchorStyles.Bottom);
            this.grdOrder.Location = new System.Drawing.Point(12, gridTop);
            this.grdOrder.Name = "grdOrder";
            this.grdOrder.Size = new System.Drawing.Size(800, 300);
            this.grdOrder.CurrentCellDirtyStateChanged += new System.EventHandler(this.grdOrder_CurrentCellDirtyStateChanged);


            // ---------- txtLog ----------
            int logTop = gridTop + 310;
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Anchor = (System.Windows.Forms.AnchorStyles.Left |
                                  System.Windows.Forms.AnchorStyles.Right |
                                  System.Windows.Forms.AnchorStyles.Bottom);
            this.txtLog.Location = new System.Drawing.Point(12, logTop);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(800, 120);

            // ---------- MainForm ----------
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.ClientSize = new System.Drawing.Size(824, logTop + 140);
            this.Controls.Add(this.btnLoadSeq);
            this.Controls.Add(this.btnHighlightSelected);


            this.Controls.Add(this.grdOrder);
            this.Controls.Add(this.txtLog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "MainForm";
            this.Text = "BoltPhaseChecker";
            this.Load += new System.EventHandler(this.MainForm_Load);

            ((System.ComponentModel.ISupportInitialize)(this.grdOrder)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

    }
}
