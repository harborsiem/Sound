/*File AudioRecorder02.java
Copyright 2003, Richard G. Baldwin

This program demonstrates the capture of audio
data from a microphone into an audio file.

A GUI appears on the screen containing the
following buttons:
  Capture
  Stop

In addition, five radio buttons appear on the
screen allowing the user to select one of the
following five audio output file formats:

  AIFC
  AIFF
  AU
  SND
  WAVE

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

Not all file types can be created on all systems.
For example, types AIFC and SND produce a "type
not supported" error on my system.

Be sure to release the old file from the media
player before attempting to create a new file
with the same extension.

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
    public partial class AudioRecorder02 : Form {
        AudioFormat audioFormat;
        ITargetDataLine targetDataLine;
        CheckedAudioFormat _caf;

        public AudioRecorder02() {
            InitializeComponent();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            Trace.AutoFlush = true;
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

                if (aifcBtn.Checked)
                    _caf = CheckedAudioFormat.aifc;
                else if (aiffBtn.Checked)
                    _caf = CheckedAudioFormat.aiff;
                else if (auBtn.Checked)
                    _caf = CheckedAudioFormat.au;
                else if (sndBtn.Checked)
                    _caf = CheckedAudioFormat.snd;
                else if (waveBtn.Checked)
                    _caf = CheckedAudioFormat.wave;
                else _caf = CheckedAudioFormat.None;

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
            float sampleRate = 11025.0F;
            //8000,11025,16000,22050,44100
            int sampleSizeInBits = 8;
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
            AudioRecorder02 caller;
            public CaptureThread(AudioRecorder02 caller) {
                this.caller = caller;
            }

            public void run() {
                AudioFileFormat.Type fileType = null;
                FileInfo audioFile = null;

                //Set the file type and the file extension
                // based on the selected radio button.
                //if (caller.aifcBtn.Checked) {
                //    fileType = AudioFileFormat.Type.AIFC;
                //    audioFile = new FileInfo("junk.aifc");
                //} else if (caller.aiffBtn.Checked) {
                //    fileType = AudioFileFormat.Type.AIFF;
                //    audioFile = new FileInfo("junk.aif");
                //} else if (caller.auBtn.Checked) {
                //    fileType = AudioFileFormat.Type.AU;
                //    audioFile = new FileInfo("junk.au");
                //} else if (caller.sndBtn.Checked) {
                //    fileType = AudioFileFormat.Type.SND;
                //    audioFile = new FileInfo("junk.snd");
                //} else if (caller.waveBtn.Checked) {
                //    fileType = AudioFileFormat.Type.WAVE;
                //    audioFile = new FileInfo("junk.wav");
                //}//end if

                switch (caller._caf) {
                    case CheckedAudioFormat.aifc:
                        fileType = AudioFileFormat.Type.AIFC;
                        audioFile = new FileInfo("junk.aifc");
                        break;
                    case CheckedAudioFormat.aiff:
                        fileType = AudioFileFormat.Type.AIFF;
                        audioFile = new FileInfo("junk.aif");
                        break;
                    case CheckedAudioFormat.au:
                        fileType = AudioFileFormat.Type.AU;
                        audioFile = new FileInfo("junk.au");
                        break;
                    case CheckedAudioFormat.snd:
                        fileType = AudioFileFormat.Type.SND;
                        audioFile = new FileInfo("junk.snd");
                        break;
                    case CheckedAudioFormat.wave:
                        fileType = AudioFileFormat.Type.WAVE;
                        audioFile = new FileInfo("junk.wav");
                        break;
                    default: break;
                }

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

        public enum CheckedAudioFormat {
            None,
            aifc,
            aiff,
            au,
            snd,
            wave,
        }

    }//end outer class AudioRecorder02.java
}
