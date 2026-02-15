namespace AudioRecorder02 {
    partial class AudioRecorder02 {
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
            this.aifcBtn = new System.Windows.Forms.RadioButton();
            this.aiffBtn = new System.Windows.Forms.RadioButton();
            this.auBtn = new System.Windows.Forms.RadioButton();
            this.sndBtn = new System.Windows.Forms.RadioButton();
            this.waveBtn = new System.Windows.Forms.RadioButton();
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
            // aifcBtn
            // 
            this.aifcBtn.AutoSize = true;
            this.aifcBtn.Location = new System.Drawing.Point(12, 50);
            this.aifcBtn.Name = "aifcBtn";
            this.aifcBtn.Size = new System.Drawing.Size(48, 17);
            this.aifcBtn.TabIndex = 2;
            this.aifcBtn.Text = "AIFC";
            this.aifcBtn.UseVisualStyleBackColor = true;
            // 
            // aiffBtn
            // 
            this.aiffBtn.AutoSize = true;
            this.aiffBtn.Location = new System.Drawing.Point(66, 50);
            this.aiffBtn.Name = "aiffBtn";
            this.aiffBtn.Size = new System.Drawing.Size(47, 17);
            this.aiffBtn.TabIndex = 3;
            this.aiffBtn.Text = "AIFF";
            this.aiffBtn.UseVisualStyleBackColor = true;
            // 
            // auBtn
            // 
            this.auBtn.AutoSize = true;
            this.auBtn.Checked = true;
            this.auBtn.Location = new System.Drawing.Point(119, 50);
            this.auBtn.Name = "auBtn";
            this.auBtn.Size = new System.Drawing.Size(40, 17);
            this.auBtn.TabIndex = 4;
            this.auBtn.TabStop = true;
            this.auBtn.Text = "AU";
            this.auBtn.UseVisualStyleBackColor = true;
            // 
            // sndBtn
            // 
            this.sndBtn.AutoSize = true;
            this.sndBtn.Location = new System.Drawing.Point(165, 50);
            this.sndBtn.Name = "sndBtn";
            this.sndBtn.Size = new System.Drawing.Size(48, 17);
            this.sndBtn.TabIndex = 5;
            this.sndBtn.Text = "SND";
            this.sndBtn.UseVisualStyleBackColor = true;
            // 
            // waveBtn
            // 
            this.waveBtn.AutoSize = true;
            this.waveBtn.Location = new System.Drawing.Point(219, 50);
            this.waveBtn.Name = "waveBtn";
            this.waveBtn.Size = new System.Drawing.Size(57, 17);
            this.waveBtn.TabIndex = 6;
            this.waveBtn.Text = "WAVE";
            this.waveBtn.UseVisualStyleBackColor = true;
            // 
            // AudioRecorder02
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 82);
            this.Controls.Add(this.waveBtn);
            this.Controls.Add(this.sndBtn);
            this.Controls.Add(this.auBtn);
            this.Controls.Add(this.aiffBtn);
            this.Controls.Add(this.aifcBtn);
            this.Controls.Add(this.stopBtn);
            this.Controls.Add(this.captureBtn);
            this.Name = "AudioRecorder02";
            this.Text = "Copyright 2003, R.G.Baldwin";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button captureBtn;
        private System.Windows.Forms.Button stopBtn;
        private System.Windows.Forms.RadioButton aifcBtn;
        private System.Windows.Forms.RadioButton aiffBtn;
        private System.Windows.Forms.RadioButton auBtn;
        private System.Windows.Forms.RadioButton sndBtn;
        private System.Windows.Forms.RadioButton waveBtn;
    }
}

