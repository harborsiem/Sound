namespace AudioPlayer02 {
    partial class AudioPlayer {
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
            this.playBtn = new System.Windows.Forms.Button();
            this.stopBtn = new System.Windows.Forms.Button();
            this.textField = new System.Windows.Forms.TextBox();
            this.systemOut = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // playBtn
            // 
            this.playBtn.Location = new System.Drawing.Point(12, 26);
            this.playBtn.Name = "playBtn";
            this.playBtn.Size = new System.Drawing.Size(75, 23);
            this.playBtn.TabIndex = 0;
            this.playBtn.Text = "Play";
            this.playBtn.UseVisualStyleBackColor = true;
            this.playBtn.Click += new System.EventHandler(this.playBtn_Click);
            // 
            // stopBtn
            // 
            this.stopBtn.Location = new System.Drawing.Point(155, 26);
            this.stopBtn.Name = "stopBtn";
            this.stopBtn.Size = new System.Drawing.Size(75, 23);
            this.stopBtn.TabIndex = 1;
            this.stopBtn.Text = "Stop";
            this.stopBtn.UseVisualStyleBackColor = true;
            this.stopBtn.Click += new System.EventHandler(this.stopBtn_Click);
            // 
            // textField
            // 
            this.textField.Location = new System.Drawing.Point(12, 0);
            this.textField.Name = "textField";
            this.textField.Size = new System.Drawing.Size(218, 20);
            this.textField.TabIndex = 2;
            this.textField.Text = "junk.au";
            // 
            // systemOut
            // 
            this.systemOut.Location = new System.Drawing.Point(12, 55);
            this.systemOut.Name = "systemOut";
            this.systemOut.Size = new System.Drawing.Size(218, 20);
            this.systemOut.TabIndex = 3;
            // 
            // AudioPlayer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(242, 85);
            this.Controls.Add(this.systemOut);
            this.Controls.Add(this.textField);
            this.Controls.Add(this.stopBtn);
            this.Controls.Add(this.playBtn);
            this.Name = "AudioPlayer";
            this.Text = "Copyright 2003, R.G.Baldwin";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button playBtn;
        private System.Windows.Forms.Button stopBtn;
        private System.Windows.Forms.TextBox textField;
        private System.Windows.Forms.TextBox systemOut;
    }
}

