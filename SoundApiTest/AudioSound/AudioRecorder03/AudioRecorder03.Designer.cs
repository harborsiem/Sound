namespace AudioRecorder02 {
    partial class AudioRecorder03 {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent() {
            this.captureBtn = new System.Windows.Forms.Button();
            this.stopBtn = new System.Windows.Forms.Button();
            this.btnPanel = new System.Windows.Forms.TableLayoutPanel();
            this.SuspendLayout();
            // 
            // captureBtn
            // 
            this.captureBtn.Location = new System.Drawing.Point(67, 12);
            this.captureBtn.Name = "captureBtn";
            this.captureBtn.Size = new System.Drawing.Size(75, 23);
            this.captureBtn.TabIndex = 0;
            this.captureBtn.Text = "Capture";
            this.captureBtn.UseVisualStyleBackColor = true;
            this.captureBtn.Click += new System.EventHandler(this.captureBtn_Click);
            // 
            // stopBtn
            // 
            this.stopBtn.Enabled = false;
            this.stopBtn.Location = new System.Drawing.Point(148, 12);
            this.stopBtn.Name = "stopBtn";
            this.stopBtn.Size = new System.Drawing.Size(75, 23);
            this.stopBtn.TabIndex = 1;
            this.stopBtn.Text = "Stop";
            this.stopBtn.UseVisualStyleBackColor = true;
            this.stopBtn.Click += new System.EventHandler(this.stopBtn_Click);
            // 
            // btnPanel
            // 
            this.btnPanel.AutoSize = true;
            this.btnPanel.ColumnCount = 1;
            this.btnPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.btnPanel.Location = new System.Drawing.Point(12, 45);
            this.btnPanel.Name = "btnPanel";
            this.btnPanel.RowCount = 1;
            this.btnPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.btnPanel.Size = new System.Drawing.Size(270, 25);
            this.btnPanel.TabIndex = 7;
            // 
            // AudioRecorder03
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 82);
            this.Controls.Add(this.btnPanel);
            this.Controls.Add(this.stopBtn);
            this.Controls.Add(this.captureBtn);
            this.Name = "AudioRecorder03";
            this.Text = "Copyright 2003, R.G.Baldwin";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button captureBtn;
        private System.Windows.Forms.Button stopBtn;
        private System.Windows.Forms.TableLayoutPanel btnPanel;
    }
}

