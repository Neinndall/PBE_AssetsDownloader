namespace PBE_AssetsDownloader.UI
{
    partial class HelpForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Button btnChangelogs;
        private System.Windows.Forms.RichTextBox richTextBoxContent; // RichTextBox para mostrar el contenido

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
            this.btnAbout = new System.Windows.Forms.Button();
            this.btnChangelogs = new System.Windows.Forms.Button();
            this.richTextBoxContent = new System.Windows.Forms.RichTextBox(); // Inicialización del RichTextBox
            this.SuspendLayout();
            
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left))); // Anclar al borde superior izquierdo
            this.btnAbout.Location = new System.Drawing.Point(12, 12); // Ajustar la posición
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(100, 30); // Tamaño ajustado
            this.btnAbout.TabIndex = 0;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            
            // 
            // btnChangelogs
            // 
            this.btnChangelogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left))); // Anclar al borde superior izquierdo
            this.btnChangelogs.Location = new System.Drawing.Point(120, 12); // Ajustar la posición para alinearlo horizontalmente
            this.btnChangelogs.Name = "btnChangelogs";
            this.btnChangelogs.Size = new System.Drawing.Size(100, 30); // Tamaño ajustado
            this.btnChangelogs.TabIndex = 1;
            this.btnChangelogs.Text = "Changelogs";
            this.btnChangelogs.UseVisualStyleBackColor = true;
            this.btnChangelogs.Click += new System.EventHandler(this.btnChangelogs_Click);
            
            // 
            // richTextBoxContent
            // 
            this.richTextBoxContent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom)));
            this.richTextBoxContent.Location = new System.Drawing.Point(12, 50);
            this.richTextBoxContent.Name = "richTextBoxContent";
            this.richTextBoxContent.Size = new System.Drawing.Size(816, 338);
            this.richTextBoxContent.TabIndex = 2;
            this.richTextBoxContent.ReadOnly = true;
            this.richTextBoxContent.SelectionIndent = 4;
            this.richTextBoxContent.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle; // Solo una vez
            this.richTextBoxContent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBoxContent.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxContent.Padding = new System.Windows.Forms.Padding(5);
            
            // 
            // HelpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(840, 400); // Tamaño ajustado del formulario
            this.Controls.Add(this.richTextBoxContent); // Agregar el RichTextBox a los controles del formulario
            this.Controls.Add(this.btnChangelogs); // Agregar el botón Changelogs
            this.Controls.Add(this.btnAbout); // Agregar el botón About
            this.Name = "HelpForm";
            this.Text = "Help"; // El título se actualizará por ApplicationInfos.SetInfo
            this.ResumeLayout(false);
        }
    }
}