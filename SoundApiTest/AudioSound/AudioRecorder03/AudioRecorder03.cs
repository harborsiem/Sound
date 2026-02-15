/*File AudioRecorder03.java
Copyright 2003, Richard G. Baldwin

This is an update of the program named
AudioRecorder02.  This version demonstrates how
to limit the file type choices to those that are
supported by the system.

This program demonstrates the capture of audio
data from a microphone into an audio file.

A GUI appears on the screen containing the
following buttons:
  Capture
  Stop

In addition, up to five radio buttons appear on
the screen allowing the user to select one of the
following five audio output file formats:

  AIFC
  AIFF
  AU
  SND
  WAVE

Only those file formats supported by the system
are presented to the user.  Therefore, only those
file formats supported by the system can be
selected.

When the user clicks the Capture button, input
data from a microphone is captured and saved in
an audio file named junk.xx having the specified
file format.  (xx is the file extension for the
specified file format.  You can easily change the
file name to something other than junk if you
choose to do so.)

Data capture stops and the output file is closed
when the user clicks the Stop button.

It should be possible to play the audio file
using any of a variety of readily available
media players, such as the Windows Media Player.

Be sure to release the old file from the media
player before attempting to create a new file
with the same extension.  Otherwise, a runtime
error will occur when the program attempts to
create the new file.

Tested using SDK 1.4.1 under Win2000
************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using SystemX.Sound.Sampled;

namespace AudioRecorder02 {
    public partial class AudioRecorder03 : Form {
        AudioFormat audioFormat;
        ITargetDataLine targetDataLine;
        AudioFileFormat.Type[] fileTypes;
        RadioButton[] radioBtnArray;

        public AudioRecorder03() {
            InitializeComponent();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            Trace.AutoFlush = true;
            //Get the file types for which file writing
            // support is provided by the system.
            fileTypes = AudioSystem.getAudioFileTypes();
            //Create an array of radio buttons
            radioBtnArray = new RadioButton[
                                       fileTypes.Length];
            this.btnPanel.ColumnCount = fileTypes.Length;
            for (int cnt = 0; cnt < fileTypes.Length;
                                                  cnt++) {
                String strType = fileTypes[cnt].ToString();
                RadioButton radioBtn = new RadioButton();
                radioBtn.AutoSize = true;
                radioBtn.UseVisualStyleBackColor = true;
                radioBtn.Text = strType;

                radioBtnArray[cnt] = radioBtn;
                if (cnt == 0) {
                    radioBtnArray[cnt].Checked = true;
                } else {
                    this.btnPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
                }
                this.btnPanel.Controls.Add(radioBtn, cnt, 0);
            }
        }

        //This method captures audio input from a
        // microphone and saves it in an audio file.
        private void captureAudio() {
            try {
                //Get things set up for capture
                audioFormat = getAudioFormat();
                DataLine.Info dataLineInfo =
                                    new DataLine.Info(
                                      typeof(ITargetDataLine),
                                      audioFormat);
                targetDataLine = (ITargetDataLine)
                         AudioSystem.getLine(dataLineInfo);

                //Create a thread to capture the microphone
                // data into an audio file and start the
                // thread running.  It will run until the
                // Stop button is clicked.  This method
                // will return after starting the thread.
                Thread capture = new Thread(new CaptureThread(this).run);
                capture.Start();
            } catch (Exception e) {
                Trace.WriteLine(e.StackTrace);
                Environment.Exit(0);
            }//end catch
        }//end captureAudio method

        //This method creates and returns an
        // AudioFormat object for a given set of format
        // parameters.  If these parameters don't work
        // well for you, try some of the other
        // allowable parameter values, which are shown
        // in comments following the declarations.
        private AudioFormat getAudioFormat() {
            float sampleRate = 8000.0F;
            //8000,11025,16000,22050,44100
            int sampleSizeInBits = 16;
            //8,16
            int channels = 1;
            //1,2
            bool signed = true;
            //true,false
            bool bigEndian = false;
            //true,false
            return new AudioFormat(sampleRate,
                                   sampleSizeInBits,
                                   channels,
                                   signed,
                                   bigEndian);
        }//end getAudioFormat
        //=============================================//

        //Inner class to capture data from microphone
        // and write it to an output audio file.
        class CaptureThread { //extends Thread
            AudioRecorder03 caller;
            public CaptureThread(AudioRecorder03 caller) {
                this.caller = caller;
            }

            public void run() {
                AudioFileFormat.Type fileType = null;
                FileInfo audioFile = null;

                //Get the selected file type described as 
                // a String
                caller.Invoke((Action)delegate {
                    String strType = String.Empty;
                    for (int i = 0; i < caller.radioBtnArray.Length; i++) {
                        if (caller.radioBtnArray[i].Checked) {
                            strType = caller.radioBtnArray[i].Text;
                        }
                    }
                    //Set the file type and the file extension
                    // based on the selected radio button.
                    if (strType.Equals("AIFC")) {
                        fileType = AudioFileFormat.Type.AIFC;
                        audioFile = new FileInfo("junk." +
                            fileType.getExtension());
                    } else if (strType.Equals("AIFF")) {
                        fileType = AudioFileFormat.Type.AIFF;
                        audioFile = new FileInfo("junk." +
                            fileType.getExtension());
                    } else if (strType.Equals("AU")) {
                        fileType = AudioFileFormat.Type.AU;
                        audioFile = new FileInfo("junk." +
                            fileType.getExtension());
                    } else if (strType.Equals("SND")) {
                        fileType = AudioFileFormat.Type.SND;
                        audioFile = new FileInfo("junk." +
                            fileType.getExtension());
                    } else if (strType.Equals("WAVE")) {
                        fileType = AudioFileFormat.Type.WAVE;
                        audioFile = new FileInfo("junk." +
                            fileType.getExtension());
                    }//end if
                });

                try {
                    caller.targetDataLine.open(caller.audioFormat);
                    caller.targetDataLine.start();
                    AudioSystem.write(
                          new AudioInputStream(caller.targetDataLine),
                          fileType,
                          audioFile);
                } catch (Exception e) {
                    Trace.WriteLine(e.StackTrace);
                }//end catch

            }//end run
        }
        //end inner class CaptureThread
        //=============================================//

        private void captureBtn_Click(object sender, EventArgs e) {
            captureBtn.Enabled = (false);
            stopBtn.Enabled = (true);
            //Capture input data from the
            // microphone until the Stop button is
            // clicked.
            captureAudio();
        }

        private void stopBtn_Click(object sender, EventArgs e) {
            captureBtn.Enabled = (true);
            stopBtn.Enabled = (false);
            //Terminate the capturing of input data
            // from the microphone.
            targetDataLine.stop();
            targetDataLine.close();
        }

    }//end outer class AudioRecorder03.java
}
