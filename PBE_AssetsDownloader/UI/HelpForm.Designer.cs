namespace PBE_AssetsDownloader.UI
{
    partial class HelpForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabAbout;
        private System.Windows.Forms.TabPage tabChangelogs;
        private System.Windows.Forms.TabPage tabBugsReport;
        private System.Windows.Forms.RichTextBox richTextBoxAbout;
        private System.Windows.Forms.RichTextBox richTextBoxChangelogs;

        private System.Windows.Forms.GroupBox groupBoxBugReport;
        private System.Windows.Forms.Label labelBugReport;
        private System.Windows.Forms.Button buttonReportBug;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabAbout = new System.Windows.Forms.TabPage();
            this.tabChangelogs = new System.Windows.Forms.TabPage();
            this.tabBugsReport = new System.Windows.Forms.TabPage();

            this.groupBoxBugReport = new System.Windows.Forms.GroupBox(); // <- Correcto aquí
            this.labelBugReport = new System.Windows.Forms.Label();
            this.buttonReportBug = new System.Windows.Forms.Button();

            this.richTextBoxAbout = new System.Windows.Forms.RichTextBox();
            this.richTextBoxChangelogs = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();

            // tabControl
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.tabControl.Controls.Add(this.tabAbout);
            this.tabControl.Controls.Add(this.tabChangelogs);
            this.tabControl.Controls.Add(this.tabBugsReport);
            this.tabControl.Location = new System.Drawing.Point(20, 20);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(580, 420);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);

            // tabAbout
            this.tabAbout.Controls.Add(this.richTextBoxAbout);
            this.tabAbout.Location = new System.Drawing.Point(4, 25);
            this.tabAbout.Name = "tabAbout";
            this.tabAbout.Size = new System.Drawing.Size(572, 375);
            this.tabAbout.TabIndex = 0;
            this.tabAbout.Text = "About";
            this.tabAbout.UseVisualStyleBackColor = true;

            // tabChangelogs
            this.tabChangelogs.Controls.Add(this.richTextBoxChangelogs);
            this.tabChangelogs.Location = new System.Drawing.Point(4, 25);
            this.tabChangelogs.Name = "tabChangelogs";
            this.tabChangelogs.Size = new System.Drawing.Size(572, 375);
            this.tabChangelogs.TabIndex = 1;
            this.tabChangelogs.Text = "Changelogs";
            this.tabChangelogs.UseVisualStyleBackColor = true;

            // tabBugsReport
            this.tabBugsReport.Controls.Add(this.groupBoxBugReport);
            this.tabBugsReport.Location = new System.Drawing.Point(4, 25);
            this.tabBugsReport.Name = "tabBugsReport";
            this.tabBugsReport.Size = new System.Drawing.Size(572, 375);
            this.tabBugsReport.TabIndex = 2;
            this.tabBugsReport.Text = "Bugs Report";
            this.tabBugsReport.UseVisualStyleBackColor = true;

            // groupBoxBugReport
            this.groupBoxBugReport.Text = "Reportar errores";
            this.groupBoxBugReport.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupBoxBugReport.Location = new System.Drawing.Point(20, 20);
            this.groupBoxBugReport.Size = new System.Drawing.Size(530, 120);
            this.groupBoxBugReport.Controls.Add(this.labelBugReport);
            this.groupBoxBugReport.Controls.Add(this.buttonReportBug);

            // labelBugReport
            this.labelBugReport.Text = "¿Has encontrado un error? Haz clic abajo para reportarlo.";
            this.labelBugReport.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelBugReport.Location = new System.Drawing.Point(20, 30);
            this.labelBugReport.Size = new System.Drawing.Size(480, 20);

            // buttonReportBug
            this.buttonReportBug.Text = "Abrir formulario de reporte";
            this.buttonReportBug.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.buttonReportBug.Size = new System.Drawing.Size(220, 30);
            this.buttonReportBug.Location = new System.Drawing.Point(20, 60);
            this.buttonReportBug.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonReportBug.BackColor = System.Drawing.Color.LightSteelBlue;
            this.buttonReportBug.Click += new System.EventHandler(this.buttonReportBug_Click);

            // richTextBoxAbout
            this.richTextBoxAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.richTextBoxAbout.Location = new System.Drawing.Point(6, 6);
            this.richTextBoxAbout.Name = "richTextBoxAbout";
            this.richTextBoxAbout.Size = new System.Drawing.Size(560, 360);
            this.richTextBoxAbout.TabIndex = 2;
            this.richTextBoxAbout.ReadOnly = true;
            this.richTextBoxAbout.SelectionIndent = 4;
            this.richTextBoxAbout.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxAbout.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.richTextBoxAbout.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxAbout.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;

            // richTextBoxChangelogs
            this.richTextBoxChangelogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.richTextBoxChangelogs.Location = new System.Drawing.Point(6, 6);
            this.richTextBoxChangelogs.Name = "richTextBoxChangelogs";
            this.richTextBoxChangelogs.Size = new System.Drawing.Size(560, 360);
            this.richTextBoxChangelogs.TabIndex = 3;
            this.richTextBoxChangelogs.ReadOnly = true;
            this.richTextBoxChangelogs.SelectionIndent = 4;
            this.richTextBoxChangelogs.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxChangelogs.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.richTextBoxChangelogs.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxChangelogs.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;

            // HelpForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(620, 460);
            this.Controls.Add(this.tabControl);
            this.Name = "HelpForm";
            this.Text = "Help"; 
            this.ResumeLayout(false);
        }
    }
}