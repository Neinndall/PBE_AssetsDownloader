using System;
using System.Windows.Forms;
using System.Drawing;

namespace PBE_AssetsDownloader.UI
{
    partial class SettingsForm
    {
        private System.Windows.Forms.Button btnSave;

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabGeneralSettings;
        private System.Windows.Forms.TabPage tabHashesPath;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.TabPage tabLogs;

        private System.Windows.Forms.CheckBox checkBoxSyncHashes;
        private System.Windows.Forms.CheckBox checkBoxAutoCopy;
        private System.Windows.Forms.CheckBox CheckBoxCreateBackUp;
        private System.Windows.Forms.CheckBox checkBoxOnlyCheckDifferences;

        private System.Windows.Forms.Label lblSyncHashesInfo;
        private System.Windows.Forms.Label lblAutoCopyInfo;
        private System.Windows.Forms.Label lblCreateBackUpInfo;
        private System.Windows.Forms.Label lblOnlyCheckDifferencesInfo;

        private System.Windows.Forms.Panel panelSyncHashesInfo;
        private System.Windows.Forms.Panel panelAutoCopyInfo;
        private System.Windows.Forms.Panel panelCreateBackUpInfo;
        private System.Windows.Forms.Panel panelOnlyCheckDifferencesInfo;

        private System.Windows.Forms.GroupBox groupBoxHashesNew; 
        private System.Windows.Forms.GroupBox groupBoxHashesOld;
        
        private System.Windows.Forms.Button btnNewHashesPath;
        private System.Windows.Forms.Button btnOldHashesPath;

        private System.Windows.Forms.TextBox textBoxNewHashPath;
        private System.Windows.Forms.TextBox textBoxOldHashPath;
        
        private System.Windows.Forms.Label lblResetSettings;

        private System.Windows.Forms.RichTextBox richTextBoxLogs;

        private void InitializeComponent()
        {
            this.btnSave = new System.Windows.Forms.Button();

            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabGeneralSettings = new System.Windows.Forms.TabPage();
            this.tabHashesPath = new System.Windows.Forms.TabPage();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.tabLogs = new System.Windows.Forms.TabPage();

            this.checkBoxSyncHashes = new System.Windows.Forms.CheckBox();
            this.checkBoxAutoCopy = new System.Windows.Forms.CheckBox();
            this.CheckBoxCreateBackUp = new System.Windows.Forms.CheckBox();
            this.checkBoxOnlyCheckDifferences = new System.Windows.Forms.CheckBox();

            this.lblSyncHashesInfo = new System.Windows.Forms.Label();
            this.lblAutoCopyInfo = new System.Windows.Forms.Label();
            this.lblCreateBackUpInfo = new System.Windows.Forms.Label();
            this.lblOnlyCheckDifferencesInfo = new System.Windows.Forms.Label();

            this.panelSyncHashesInfo = new System.Windows.Forms.Panel();
            this.panelAutoCopyInfo = new System.Windows.Forms.Panel();
            this.panelCreateBackUpInfo = new System.Windows.Forms.Panel();
            this.panelOnlyCheckDifferencesInfo = new System.Windows.Forms.Panel();

            this.groupBoxHashesNew = new System.Windows.Forms.GroupBox();
            this.groupBoxHashesOld = new System.Windows.Forms.GroupBox();
                        
            this.textBoxNewHashPath = new System.Windows.Forms.TextBox();
            this.textBoxOldHashPath = new System.Windows.Forms.TextBox();
                                    
            this.btnNewHashesPath = new System.Windows.Forms.Button();
            this.btnOldHashesPath = new System.Windows.Forms.Button();

            this.lblResetSettings = new System.Windows.Forms.Label();

            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();

            // TabControl
            this.tabControl.Controls.Add(this.tabGeneralSettings);
            this.tabControl.Controls.Add(this.tabHashesPath);
            this.tabControl.Controls.Add(this.tabAdvanced);
            this.tabControl.Controls.Add(this.tabLogs);
            this.tabControl.Location = new System.Drawing.Point(20, 20);
            this.tabControl.Size = new System.Drawing.Size(340, 255); // Ajustado tamaño para ventana más pequeña
            this.tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;  // Añadir Anchor en el borde inferior

            // Tab General Settings
            this.tabGeneralSettings.Text = "General Settings"; // Nombre visible de la pestaña
            this.tabGeneralSettings.Controls.Add(this.checkBoxSyncHashes);
            this.tabGeneralSettings.Controls.Add(this.lblSyncHashesInfo);
            this.tabGeneralSettings.Controls.Add(this.panelSyncHashesInfo);
            this.tabGeneralSettings.Controls.Add(this.checkBoxAutoCopy);
            this.tabGeneralSettings.Controls.Add(this.lblAutoCopyInfo);
            this.tabGeneralSettings.Controls.Add(this.panelAutoCopyInfo);
            this.tabGeneralSettings.Controls.Add(this.CheckBoxCreateBackUp);
            this.tabGeneralSettings.Controls.Add(this.lblCreateBackUpInfo);
            this.tabGeneralSettings.Controls.Add(this.panelCreateBackUpInfo);
            this.tabGeneralSettings.Controls.Add(this.checkBoxOnlyCheckDifferences);
            this.tabGeneralSettings.Controls.Add(this.lblOnlyCheckDifferencesInfo);
            this.tabGeneralSettings.Controls.Add(this.panelOnlyCheckDifferencesInfo);

            // Tab Hashes Path
            this.tabHashesPath.Text = "Hashes Path";
            this.tabHashesPath.Controls.Add(this.btnOldHashesPath);
            this.tabHashesPath.Controls.Add(this.groupBoxHashesNew);
            this.tabHashesPath.Controls.Add(this.groupBoxHashesOld);
            
            // groupBoxHashesNew
            this.groupBoxHashesNew.Text = "Set the default directory for new hashes";
            this.groupBoxHashesNew.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.groupBoxHashesNew.Location = new System.Drawing.Point(15, 20);
            this.groupBoxHashesNew.Size = new System.Drawing.Size(300, 90);
            this.groupBoxHashesNew.Controls.Add(this.textBoxNewHashPath);
            this.groupBoxHashesNew.Controls.Add(this.btnNewHashesPath);

            // textBoxNewHashPath
            this.textBoxNewHashPath.Location = new System.Drawing.Point(15, 40);
            this.textBoxNewHashPath.Size = new System.Drawing.Size(190, 23);

            // btnNewHashesPath
            this.btnNewHashesPath.Text = "Browse";
            this.btnNewHashesPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNewHashesPath.Location = new System.Drawing.Point(215, 40);
            this.btnNewHashesPath.Size = new System.Drawing.Size(65, 25);
            this.btnNewHashesPath.Click += new System.EventHandler(this.btnBrowseNew_Click);

            // groupBoxHashesOld
            this.groupBoxHashesOld.Text = "Set the default directory for old hashes";
            this.groupBoxHashesOld.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.groupBoxHashesOld.Location = new System.Drawing.Point(15, 120);
            this.groupBoxHashesOld.Size = new System.Drawing.Size(300, 90);
            this.groupBoxHashesOld.Controls.Add(this.textBoxOldHashPath);
            this.groupBoxHashesOld.Controls.Add(this.btnOldHashesPath);

            // textBoxOldHashPath
            this.textBoxOldHashPath.Location = new System.Drawing.Point(15, 40);
            this.textBoxOldHashPath.Size = new System.Drawing.Size(190, 23);
            
            // btnOldHashesPath
            this.btnOldHashesPath.Text = "Browse";
            this.btnOldHashesPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnOldHashesPath.Location = new System.Drawing.Point(215, 40); // Disminuimos y subimos
            this.btnOldHashesPath.Size = new System.Drawing.Size(65, 25);
            this.btnOldHashesPath.Click += new System.EventHandler(this.btnBrowseOld_Click);

            // Tab Advanced
            this.tabAdvanced.Text = "Advanced"; // Nombre visible de la pestaña
            this.tabAdvanced.Controls.Add(this.lblResetSettings);

            // lblResetSettings
            this.lblResetSettings.Location = new System.Drawing.Point(12, 20); // Ajusta la posición hacia arriba
            this.lblResetSettings.Size = new System.Drawing.Size(175, 40);  // Tamaño fijo o ajustado a tus preferencias
            this.lblResetSettings.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.lblResetSettings.Text = "🔁 Restore settings to default values";
            this.lblResetSettings.TextAlign = ContentAlignment.MiddleCenter; // Centrado del texto
            this.lblResetSettings.BorderStyle = BorderStyle.FixedSingle;
            this.lblResetSettings.Cursor = Cursors.Hand;
            this.lblResetSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; // Se ajusta a los bordes izquierdo, derecho y superior
            this.lblResetSettings.MouseEnter += (s, e) => this.lblResetSettings.BackColor = Color.FromArgb(230, 230, 230); // 229, 241, 251 Azul claro (LightBlue)
            this.lblResetSettings.MouseLeave += (s, e) => this.lblResetSettings.BackColor = Color.FromArgb(245, 245, 245); // Gris claro
            this.lblResetSettings.Click += new EventHandler(this.BtnResetDefaults_Click);

            // Tab Logs
            this.tabLogs.Text = "Logs"; // Nombre visible de la pestaña
            this.tabLogs.Controls.Add(this.richTextBoxLogs);

            // Checkboxes y labels dentro del tabGeneralSettings
            this.checkBoxSyncHashes.Location = new System.Drawing.Point(10, 10);
            this.checkBoxSyncHashes.Size = new System.Drawing.Size(220, 24);
            this.checkBoxSyncHashes.Text = "Synchronize hashes with CDTB";
            this.checkBoxSyncHashes.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblSyncHashesInfo.Location = new System.Drawing.Point(10, 35);
            this.lblSyncHashesInfo.Size = new System.Drawing.Size(330, 20);
            this.lblSyncHashesInfo.Text = "Enable this option to synchronize the latest hashes.";
            this.lblSyncHashesInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.panelSyncHashesInfo.Location = new System.Drawing.Point(10, 55);
            this.panelSyncHashesInfo.Size = new System.Drawing.Size(175, 1);
            this.panelSyncHashesInfo.BackColor = System.Drawing.Color.LightBlue;
            this.panelSyncHashesInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.checkBoxAutoCopy.Location = new System.Drawing.Point(10, 65);
            this.checkBoxAutoCopy.Size = new System.Drawing.Size(215, 24);
            this.checkBoxAutoCopy.Text = "Automatically replace old hashes";
            this.checkBoxAutoCopy.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblAutoCopyInfo.Location = new System.Drawing.Point(10, 90);
            this.lblAutoCopyInfo.Size = new System.Drawing.Size(330, 20);
            this.lblAutoCopyInfo.Text = "Automatically replace old hashes with new ones.";
            this.lblAutoCopyInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.panelAutoCopyInfo.Location = new System.Drawing.Point(10, 110);
            this.panelAutoCopyInfo.Size = new System.Drawing.Size(175, 1);
            this.panelAutoCopyInfo.BackColor = System.Drawing.Color.LightBlue;
            this.panelAutoCopyInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.CheckBoxCreateBackUp.Location = new System.Drawing.Point(10, 120);
            this.CheckBoxCreateBackUp.Size = new System.Drawing.Size(250, 24);
            this.CheckBoxCreateBackUp.Text = "Create backup old hashes";
            this.CheckBoxCreateBackUp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblCreateBackUpInfo.Location = new System.Drawing.Point(10, 145);
            this.lblCreateBackUpInfo.Size = new System.Drawing.Size(320, 20);
            this.lblCreateBackUpInfo.Text = "Create backups before replacing hashes.";
            this.lblCreateBackUpInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.panelCreateBackUpInfo.Location = new System.Drawing.Point(10, 165);
            this.panelCreateBackUpInfo.Size = new System.Drawing.Size(175, 1);
            this.panelCreateBackUpInfo.BackColor = System.Drawing.Color.LightBlue;
            this.panelCreateBackUpInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.checkBoxOnlyCheckDifferences.Location = new System.Drawing.Point(10, 175);
            this.checkBoxOnlyCheckDifferences.Size = new System.Drawing.Size(240, 24);
            this.checkBoxOnlyCheckDifferences.Text = "Only check for differences";
            this.checkBoxOnlyCheckDifferences.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.lblOnlyCheckDifferencesInfo.Location = new System.Drawing.Point(10, 200);
            this.lblOnlyCheckDifferencesInfo.Size = new System.Drawing.Size(320, 20);
            this.lblOnlyCheckDifferencesInfo.Text = "Scan for differences without downloading assets.";
            this.lblOnlyCheckDifferencesInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            this.panelOnlyCheckDifferencesInfo.Location = new System.Drawing.Point(10, 220);
            this.panelOnlyCheckDifferencesInfo.Size = new System.Drawing.Size(175, 1);
            this.panelOnlyCheckDifferencesInfo.BackColor = System.Drawing.Color.LightBlue;
            this.panelOnlyCheckDifferencesInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // RichTextBox (Logs)
            this.richTextBoxLogs.Location = new System.Drawing.Point(1, 1);
            this.richTextBoxLogs.Size = new System.Drawing.Size(340, 200);
            this.richTextBoxLogs.ReadOnly = true;
            this.richTextBoxLogs.BorderStyle = BorderStyle.FixedSingle;
            this.richTextBoxLogs.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.richTextBoxLogs.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxLogs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;  // Añadir Anchor en el borde inferior

            // Save Button
            this.btnSave.Location = new System.Drawing.Point(286, 290);
            this.btnSave.Size = new System.Drawing.Size(75, 30);
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // SettingsForm
            this.ClientSize = new System.Drawing.Size(380, 340);  // Tamaño ajustado
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnSave);
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.ResumeLayout(false);
        }
    }
}
