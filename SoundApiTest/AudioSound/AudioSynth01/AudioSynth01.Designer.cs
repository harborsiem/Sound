namespace AudioSynth01 {
    partial class AudioSynth01 {
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.elapsedTimeMeter = new System.Windows.Forms.Label();
            this.playOrFileBtn = new System.Windows.Forms.Button();
            this.generateBtn = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.fileName = new System.Windows.Forms.TextBox();
            this.file = new System.Windows.Forms.RadioButton();
            this.listen = new System.Windows.Forms.RadioButton();
            this.tones = new System.Windows.Forms.RadioButton();
            this.stereoPanning = new System.Windows.Forms.RadioButton();
            this.stereoPingpong = new System.Windows.Forms.RadioButton();
            this.fmSweep = new System.Windows.Forms.RadioButton();
            this.decayPulse = new System.Windows.Forms.RadioButton();
            this.echoPulse = new System.Windows.Forms.RadioButton();
            this.waWaPulse = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.elapsedTimeMeter);
            this.groupBox1.Controls.Add(this.playOrFileBtn);
            this.groupBox1.Controls.Add(this.generateBtn);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(268, 60);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // elapsedTimeMeter
            // 
            this.elapsedTimeMeter.AutoSize = true;
            this.elapsedTimeMeter.Location = new System.Drawing.Point(168, 24);
            this.elapsedTimeMeter.Name = "elapsedTimeMeter";
            this.elapsedTimeMeter.Size = new System.Drawing.Size(31, 13);
            this.elapsedTimeMeter.TabIndex = 2;
            this.elapsedTimeMeter.Text = "0000";
            // 
            // playOrFileBtn
            // 
            this.playOrFileBtn.Enabled = false;
            this.playOrFileBtn.Location = new System.Drawing.Point(87, 19);
            this.playOrFileBtn.Name = "playOrFileBtn";
            this.playOrFileBtn.Size = new System.Drawing.Size(75, 23);
            this.playOrFileBtn.TabIndex = 1;
            this.playOrFileBtn.Text = "Play/File";
            this.playOrFileBtn.UseVisualStyleBackColor = true;
            this.playOrFileBtn.Click += new System.EventHandler(this.playOrFileBtn_Click);
            // 
            // generateBtn
            // 
            this.generateBtn.Location = new System.Drawing.Point(6, 19);
            this.generateBtn.Name = "generateBtn";
            this.generateBtn.Size = new System.Drawing.Size(75, 23);
            this.generateBtn.TabIndex = 0;
            this.generateBtn.Text = "Generate";
            this.generateBtn.UseVisualStyleBackColor = true;
            this.generateBtn.Click += new System.EventHandler(this.generateBtn_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.fileName);
            this.groupBox2.Controls.Add(this.file);
            this.groupBox2.Controls.Add(this.listen);
            this.groupBox2.Location = new System.Drawing.Point(12, 248);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(268, 54);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "groupBox2";
            // 
            // fileName
            // 
            this.fileName.Location = new System.Drawing.Point(112, 19);
            this.fileName.Name = "fileName";
            this.fileName.Size = new System.Drawing.Size(150, 20);
            this.fileName.TabIndex = 2;
            this.fileName.Text = "junk";
            // 
            // file
            // 
            this.file.AutoSize = true;
            this.file.Location = new System.Drawing.Point(65, 19);
            this.file.Name = "file";
            this.file.Size = new System.Drawing.Size(41, 17);
            this.file.TabIndex = 1;
            this.file.Text = "File";
            this.file.UseVisualStyleBackColor = true;
            // 
            // listen
            // 
            this.listen.AutoSize = true;
            this.listen.Checked = true;
            this.listen.Location = new System.Drawing.Point(6, 19);
            this.listen.Name = "listen";
            this.listen.Size = new System.Drawing.Size(53, 17);
            this.listen.TabIndex = 0;
            this.listen.TabStop = true;
            this.listen.Text = "Listen";
            this.listen.UseVisualStyleBackColor = true;
            // 
            // tones
            // 
            this.tones.AutoSize = true;
            this.tones.Checked = true;
            this.tones.Location = new System.Drawing.Point(89, 78);
            this.tones.Name = "tones";
            this.tones.Size = new System.Drawing.Size(55, 17);
            this.tones.TabIndex = 2;
            this.tones.TabStop = true;
            this.tones.Text = "Tones";
            this.tones.UseVisualStyleBackColor = true;
            // 
            // stereoPanning
            // 
            this.stereoPanning.AutoSize = true;
            this.stereoPanning.Location = new System.Drawing.Point(89, 101);
            this.stereoPanning.Name = "stereoPanning";
            this.stereoPanning.Size = new System.Drawing.Size(98, 17);
            this.stereoPanning.TabIndex = 3;
            this.stereoPanning.Text = "Stereo Panning";
            this.stereoPanning.UseVisualStyleBackColor = true;
            // 
            // stereoPingpong
            // 
            this.stereoPingpong.AutoSize = true;
            this.stereoPingpong.Location = new System.Drawing.Point(89, 124);
            this.stereoPingpong.Name = "stereoPingpong";
            this.stereoPingpong.Size = new System.Drawing.Size(104, 17);
            this.stereoPingpong.TabIndex = 4;
            this.stereoPingpong.Text = "Stereo Pingpong";
            this.stereoPingpong.UseVisualStyleBackColor = true;
            // 
            // fmSweep
            // 
            this.fmSweep.AutoSize = true;
            this.fmSweep.Location = new System.Drawing.Point(89, 147);
            this.fmSweep.Name = "fmSweep";
            this.fmSweep.Size = new System.Drawing.Size(76, 17);
            this.fmSweep.TabIndex = 5;
            this.fmSweep.Text = "FM Sweep";
            this.fmSweep.UseVisualStyleBackColor = true;
            // 
            // decayPulse
            // 
            this.decayPulse.AutoSize = true;
            this.decayPulse.Location = new System.Drawing.Point(89, 170);
            this.decayPulse.Name = "decayPulse";
            this.decayPulse.Size = new System.Drawing.Size(85, 17);
            this.decayPulse.TabIndex = 6;
            this.decayPulse.Text = "Decay Pulse";
            this.decayPulse.UseVisualStyleBackColor = true;
            // 
            // echoPulse
            // 
            this.echoPulse.AutoSize = true;
            this.echoPulse.Location = new System.Drawing.Point(89, 193);
            this.echoPulse.Name = "echoPulse";
            this.echoPulse.Size = new System.Drawing.Size(79, 17);
            this.echoPulse.TabIndex = 7;
            this.echoPulse.Text = "Echo Pulse";
            this.echoPulse.UseVisualStyleBackColor = true;
            // 
            // waWaPulse
            // 
            this.waWaPulse.AutoSize = true;
            this.waWaPulse.Location = new System.Drawing.Point(89, 216);
            this.waWaPulse.Name = "waWaPulse";
            this.waWaPulse.Size = new System.Drawing.Size(91, 17);
            this.waWaPulse.TabIndex = 8;
            this.waWaPulse.Text = "Wa Wa Pulse";
            this.waWaPulse.UseVisualStyleBackColor = true;
            // 
            // AudioSynth01
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 314);
            this.Controls.Add(this.waWaPulse);
            this.Controls.Add(this.echoPulse);
            this.Controls.Add(this.decayPulse);
            this.Controls.Add(this.fmSweep);
            this.Controls.Add(this.stereoPingpong);
            this.Controls.Add(this.stereoPanning);
            this.Controls.Add(this.tones);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "AudioSynth01";
            this.Text = "Copyright 2003, R.G.Baldwin";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label elapsedTimeMeter;
        private System.Windows.Forms.Button playOrFileBtn;
        private System.Windows.Forms.Button generateBtn;
        private System.Windows.Forms.TextBox fileName;
        private System.Windows.Forms.RadioButton file;
        private System.Windows.Forms.RadioButton listen;
        private System.Windows.Forms.RadioButton tones;
        private System.Windows.Forms.RadioButton stereoPanning;
        private System.Windows.Forms.RadioButton stereoPingpong;
        private System.Windows.Forms.RadioButton fmSweep;
        private System.Windows.Forms.RadioButton decayPulse;
        private System.Windows.Forms.RadioButton echoPulse;
        private System.Windows.Forms.RadioButton waWaPulse;
    }
}

