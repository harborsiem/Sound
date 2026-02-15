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

namespace AudioCapture01 {
    public partial class AudioCapture : Form {
        bool stopCapture = false;
        MemoryStream byteArrayOutputStream;
        AudioFormat audioFormat;
        ITargetDataLine targetDataLine;
        AudioInputStream audioInputStream;
        ISourceDataLine sourceDataLine;

        public AudioCapture() {
            InitializeComponent();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            Trace.AutoFlush = true;
        }

        //This method captures audio input
        // from a microphone and saves it in
        // a MemoryStream object.
        private void captureAudio() {
            try {
                //Get everything set up for
                // capture
                audioFormat = getAudioFormat();
                DataLine.Info dataLineInfo =
                          new DataLine.Info(
                            typeof(ITargetDataLine),
                             audioFormat);
                targetDataLine = (ITargetDataLine)
                             AudioSystem.getLine(
                                   dataLineInfo);
                targetDataLine.open(audioFormat);
                targetDataLine.start();

                //Create a thread to capture the
                // microphone data and start it
                // running.  It will run until
                // the Stop button is clicked.
                Thread captureThread =
                          new Thread(
                            new CaptureThread(this).run);
                captureThread.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end captureAudio method

        //This method plays back the audio
        // data that has been saved in the
        // ByteArrayOutputStream
        private void playAudio() {
            try {
                //Get everything set up for
                // playback.
                //Get the previously-saved data
                // into a byte array object.
                byte[] audioData =
                           byteArrayOutputStream.ToArray();
				byteArrayOutputStream.Close();
                //Get an input stream on the
                // byte array containing the data
                Stream byteArrayInputStream
                      = new MemoryStream(audioData);
                AudioFormat audioFormat = getAudioFormat();
                audioInputStream =
                  new AudioInputStream(
                    byteArrayInputStream,
                    audioFormat,
                    audioData.Length / audioFormat.
                                 getFrameSize());
                DataLine.Info dataLineInfo =
                          new DataLine.Info(
                            typeof(ISourceDataLine),
                                    audioFormat);
                sourceDataLine = (ISourceDataLine)
                             AudioSystem.getLine(
                                   dataLineInfo);
                sourceDataLine.open(audioFormat);
                sourceDataLine.start();

                //Create a thread to play back
                // the data and start it
                // running.  It will run until
                // all the data has been played
                // back.
                Thread playThread =
                    new Thread(new PlayThread(this).run);
                playThread.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end playAudio

        //This method creates and returns an
        // AudioFormat object for a given set
        // of format parameters.  If these
        // parameters don't work well for
        // you, try some of the other
        // allowable parameter values, which
        // are shown in comments following
        // the declarations.
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
            return new AudioFormat(
                              sampleRate,
                              sampleSizeInBits,
                              channels,
                              signed,
                              bigEndian);
        }//end getAudioFormat
        //===================================//

        //Inner class to capture data from
        // microphone
        class CaptureThread { //extends Thread
            //An arbitrary-size temporary holding
            // buffer
            AudioCapture caller;
            byte[] tempBuffer = new byte[10000];

            public CaptureThread(AudioCapture caller) {
                this.caller = caller;
            }

            public void run() {
                caller.byteArrayOutputStream =
                       new MemoryStream();
                caller.stopCapture = false;
                try {//Loop until stopCapture is set
                    // by another thread that
                    // services the Stop button.
                    while (!caller.stopCapture) {
                        //Read data from the internal
                        // buffer of the data line.
                        int cnt = caller.targetDataLine.read(
                                    tempBuffer,
                                    0,
                                    tempBuffer.Length);
                        if (cnt > 0) {
                            //Save data in output stream
                            // object.
                            caller.byteArrayOutputStream.Write(
                                     tempBuffer, 0, cnt);
                        }//end if
                    }//end while
                    //caller.byteArrayOutputStream.Close();
                }
                catch (Exception e) {
                    Trace.WriteLine(e);
                    Environment.Exit(0);
                }//end catch
            }//end run
        }//end inner class CaptureThread
        //===================================//
        //Inner class to play back the data
        // that was saved.
        class PlayThread { //extends Thread
            AudioCapture caller;
            byte[] tempBuffer = new byte[10000];

            public PlayThread(AudioCapture caller) {
                this.caller = caller;
            }

            public void run() {
                try {
                    int cnt;
                    //Keep looping until the input
                    // read method returns -1 for
                    // empty stream.
                    while ((cnt = caller.audioInputStream.
                      Read(tempBuffer, 0,
                          tempBuffer.Length)) != -1) {
                        if (cnt > 0) {
                            //Write data to the internal
                            // buffer of the data line
                            // where it will be delivered
                            // to the speaker.
                            caller.sourceDataLine.write(
                                     tempBuffer, 0, cnt);
                        }//end if
                    }//end while
                    //Block and wait for internal
                    // buffer of the data line to
                    // empty.
                    caller.sourceDataLine.drain();
                    caller.sourceDataLine.close();
                }
                catch (Exception e) {
                    Trace.WriteLine(e);
                    Environment.Exit(0);
                }//end catch
            }//end run
        }

        //end inner class PlayThread
        //===================================//

        private void captureBtn_Click(object sender, EventArgs e) {
            captureBtn.Enabled = (false);
            stopBtn.Enabled = (true);
            playBtn.Enabled = (false);
            //Capture input data from the
            // microphone until the Stop
            // button is clicked.
            captureAudio();
        }

        private void stopBtn_Click(object sender, EventArgs e) {
            captureBtn.Enabled = (true);
            stopBtn.Enabled = (false);
            playBtn.Enabled = (true);
            //Terminate the capturing of
            // input data from the
            // microphone.
            stopCapture = true;
        }

        private void playBtn_Click(object sender, EventArgs e) {
            //Play back all of the data
            // that was saved during
            // capture.
            playAudio();
        }

    }//end outer class AudioCapture01.java

}
