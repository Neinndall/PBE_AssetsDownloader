using System.Windows.Forms;

namespace PBE_AssetsDownloader.UI
{
    partial class PreviewAssetsForm
    {
        private System.Windows.Forms.RichTextBox richTextBoxAssets;

        private void InitializeComponent()
        {
            this.richTextBoxAssets = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();

            // richTextBoxAssets
            this.richTextBoxAssets.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.richTextBoxAssets.Location = new System.Drawing.Point(20, 20); // Ajuste de margen interno
            //this.richTextBoxAssets.BackColor = System.Drawing.SystemColors.Window;
            this.richTextBoxAssets.BorderStyle = BorderStyle.FixedSingle;
            this.richTextBoxAssets.Name = "richTextBoxAssets";
            this.richTextBoxAssets.ReadOnly = true;
            this.richTextBoxAssets.Size = new System.Drawing.Size(940, 410); // Ajuste de tamaño a la ventana
            this.richTextBoxAssets.TabIndex = 0;

            // PreviewAssetsForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 450); // Tamaño de ventana ajustado
            this.Controls.Add(this.richTextBoxAssets);
            this.Name = "PreviewAssetsForm";
            this.Text = "Assets Preview";
            this.ResumeLayout(false);
        }
    }
}
