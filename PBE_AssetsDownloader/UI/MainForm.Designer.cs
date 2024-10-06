using System.Drawing; // Asegúrate de que esta línea está presente
using System.IO; // Necesario para Path.Combine
using System.Windows.Forms; // Asegúrate de que esta línea está presente
using System;

namespace PBE_NewFileExtractor.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        
        // Controles del formulario
        private System.Windows.Forms.Label labelNewHashes;
        private System.Windows.Forms.TextBox newHashesTextBox;
        private System.Windows.Forms.Button btnSelectNewHashesDirectory;
        
        private System.Windows.Forms.Label labelOldHashes;
        private System.Windows.Forms.Button btnSelectOldHashesDirectory;
        private System.Windows.Forms.TextBox oldHashesTextBox;

        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Button btnSettings; // Boton
        //private System.Windows.Forms.PictureBox pbSettings; // Cambiar de Button a PictureBox
        private System.Windows.Forms.TextBox textBoxLogs;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        
        //private ToolTip toolTip; // Declaración del ToolTip

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
            this.labelNewHashes = new System.Windows.Forms.Label();
            this.newHashesTextBox = new System.Windows.Forms.TextBox();
            this.btnSelectNewHashesDirectory = new System.Windows.Forms.Button();
            
            this.labelOldHashes = new System.Windows.Forms.Label();
            this.oldHashesTextBox = new System.Windows.Forms.TextBox();
            this.btnSelectOldHashesDirectory = new System.Windows.Forms.Button();
            
            this.startButton = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button(); // Boton
            //this.pbSettings = new System.Windows.Forms.PictureBox(); // Cambiar de Button a PictureBox

            this.textBoxLogs = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            
            this.SuspendLayout();

            // 
            // labelNewHashes
            // 
            this.labelNewHashes.AutoSize = true;
            this.labelNewHashes.Location = new System.Drawing.Point(12, 50); // 12, 100
            this.labelNewHashes.Name = "labelNewHashes";
            this.labelNewHashes.Size = new System.Drawing.Size(226, 17);
            this.labelNewHashes.TabIndex = 0;
            this.labelNewHashes.Text = "Choose your directory new hashes";
            
            // 
            // newHashesTextBox
            // 
            this.newHashesTextBox.Location = new System.Drawing.Point(12, 70); // 12, 120
            this.newHashesTextBox.Name = "newHashesTextBox";
            this.newHashesTextBox.Size = new System.Drawing.Size(676, 22);
            this.newHashesTextBox.TabIndex = 1;
            
            // 
            // btnSelectNewHashesDirectory
            // 
            this.btnSelectNewHashesDirectory.Location = new System.Drawing.Point(694, 70); // 694, 120
            this.btnSelectNewHashesDirectory.Name = "btnSelectNewHashesDirectory";
            this.btnSelectNewHashesDirectory.Size = new System.Drawing.Size(75, 26); // 75,23
            this.btnSelectNewHashesDirectory.TabIndex = 2;
            this.btnSelectNewHashesDirectory.Text = "Browse";
            this.btnSelectNewHashesDirectory.UseVisualStyleBackColor = true;
            this.btnSelectNewHashesDirectory.Click += new System.EventHandler(this.btnSelectNewHashesDirectory_Click);
            
            // 
            // labelOldHashes
            // 
            this.labelOldHashes.AutoSize = true;
            this.labelOldHashes.Location = new System.Drawing.Point(12, 100); // 12, 50
            this.labelOldHashes.Name = "labelOldHashes";
            this.labelOldHashes.Size = new System.Drawing.Size(226, 17);
            this.labelOldHashes.TabIndex = 3;
            this.labelOldHashes.Text = "Choose your directory old hashes";
            
            // 
            // oldHashesTextBox
            // 
            this.oldHashesTextBox.Location = new System.Drawing.Point(12, 120); // 12, 70
            this.oldHashesTextBox.Name = "oldHashesTextBox";
            this.oldHashesTextBox.Size = new System.Drawing.Size(676, 22);
            this.oldHashesTextBox.TabIndex = 4;
            
            // 
            // btnSelectOldHashesDirectory
            // 
            this.btnSelectOldHashesDirectory.Location = new System.Drawing.Point(694, 120); // 694, 70
            this.btnSelectOldHashesDirectory.Name = "btnSelectOldHashesDirectory";
            this.btnSelectOldHashesDirectory.Size = new System.Drawing.Size(75, 26);  // 75,23
            this.btnSelectOldHashesDirectory.TabIndex = 5;
            this.btnSelectOldHashesDirectory.Text = "Browse";
            this.btnSelectOldHashesDirectory.UseVisualStyleBackColor = true;
            this.btnSelectOldHashesDirectory.Click += new System.EventHandler(this.btnSelectOldHashesDirectory_Click);
            
            // 
            // textBoxLogs
            // 
            this.textBoxLogs.Location = new System.Drawing.Point(12, 150);
            this.textBoxLogs.Multiline = true;
            this.textBoxLogs.Name = "textBoxLogs";
            this.textBoxLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLogs.Size = new System.Drawing.Size(776, 288);
            this.textBoxLogs.TabIndex = 6;
            this.textBoxLogs.ReadOnly = true;
            
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(12, 12);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(75, 26); // 75,23 
            this.startButton.TabIndex = 7;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            
            // 
            // btnHelp
            // 
            this.btnHelp.Location = new System.Drawing.Point(713, 12);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(75, 26); // 75,23 
            this.btnHelp.TabIndex = 8;
            this.btnHelp.Text = "Help";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            
            // 
            // btnSettings
            // 
            this.btnSettings.Location = new System.Drawing.Point(632, 12);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(75, 26); // 75,23 
            this.btnSettings.TabIndex = 9;
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

            // 
            // pbSettings
            // 
            //this.pbSettings.Location = new System.Drawing.Point(680, 14); // Establece la ubicación donde quieras
            //this.pbSettings.Name = "pbSettings";
            //this.pbSettings.Size = new System.Drawing.Size(20, 20); // Tamaño del PictureBox
            //this.pbSettings.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage; // Para que ajuste la imagen al tamaño
            //this.pbSettings.Click += new System.EventHandler(this.btnSettings_Click); // Hacer clickeable
            //this.pbSettings.MouseEnter += new EventHandler(this.pbSettings_MouseEnter);
            //this.pbSettings.MouseLeave += new EventHandler(this.pbSettings_MouseLeave);
            
            // Cargar el icono de configuración
            //var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "settings3.ico");
            //this.pbSettings.Image = new Icon(iconPath).ToBitmap(); // Asignar la imagen al PictureBox
            
            // Inicializar el ToolTip
            //toolTip = new ToolTip(); // Agregar esto
            //toolTip.SetToolTip(this.pbSettings, "Settings"); // Establecer el texto del ToolTip

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.labelNewHashes);
            this.Controls.Add(this.newHashesTextBox);
            this.Controls.Add(this.btnSelectNewHashesDirectory);
            this.Controls.Add(this.labelOldHashes);
            this.Controls.Add(this.oldHashesTextBox);
            this.Controls.Add(this.btnSelectOldHashesDirectory);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.btnHelp);
            //this.Controls.Add(this.pbSettings); // Imagen
            this.Controls.Add(this.btnSettings); // Boton
            this.Controls.Add(this.textBoxLogs);
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}