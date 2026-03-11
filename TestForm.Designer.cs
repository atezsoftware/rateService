namespace rateService
{
    partial class TestForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.btnCurrency = new System.Windows.Forms.Button();
            this.btnMetal = new System.Windows.Forms.Button();
            this.btnBoth = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnShowLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();

            // btnCurrency
            this.btnCurrency.Location = new System.Drawing.Point(12, 12);
            this.btnCurrency.Size = new System.Drawing.Size(150, 40);
            this.btnCurrency.Name = "btnCurrency";
            this.btnCurrency.Text = "Kur Çek";
            this.btnCurrency.UseVisualStyleBackColor = true;
            this.btnCurrency.Click += new System.EventHandler(this.btnCurrency_Click);

            // btnMetal
            this.btnMetal.Location = new System.Drawing.Point(168, 12);
            this.btnMetal.Size = new System.Drawing.Size(150, 40);
            this.btnMetal.Name = "btnMetal";
            this.btnMetal.Text = "Metal Çek";
            this.btnMetal.UseVisualStyleBackColor = true;
            this.btnMetal.Click += new System.EventHandler(this.btnMetal_Click);

            // btnBoth
            this.btnBoth.Location = new System.Drawing.Point(324, 12);
            this.btnBoth.Size = new System.Drawing.Size(150, 40);
            this.btnBoth.Name = "btnBoth";
            this.btnBoth.Text = "Hepsini Çek";
            this.btnBoth.UseVisualStyleBackColor = true;
            this.btnBoth.Click += new System.EventHandler(this.btnBoth_Click);

            // btnShowLog
            this.btnShowLog.Location = new System.Drawing.Point(480, 12);
            this.btnShowLog.Size = new System.Drawing.Size(150, 40);
            this.btnShowLog.Name = "btnShowLog";
            this.btnShowLog.Text = "Logları Göster";
            this.btnShowLog.UseVisualStyleBackColor = true;
            this.btnShowLog.Click += new System.EventHandler(this.btnShowLog_Click);

            // btnClear
            this.btnClear.Location = new System.Drawing.Point(636, 12);
            this.btnClear.Size = new System.Drawing.Size(100, 40);
            this.btnClear.Name = "btnClear";
            this.btnClear.Text = "Temizle";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);

            // txtLog
            this.txtLog.Location = new System.Drawing.Point(12, 62);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.ReadOnly = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Size = new System.Drawing.Size(724, 380);

            // TestForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 454);
            this.Controls.Add(this.btnCurrency);
            this.Controls.Add(this.btnMetal);
            this.Controls.Add(this.btnBoth);
            this.Controls.Add(this.btnShowLog);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.txtLog);
            this.Name = "TestForm";
            this.Text = "Rate Service - Test";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnCurrency;
        private System.Windows.Forms.Button btnMetal;
        private System.Windows.Forms.Button btnBoth;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnShowLog;
        private System.Windows.Forms.TextBox txtLog;
    }
}
