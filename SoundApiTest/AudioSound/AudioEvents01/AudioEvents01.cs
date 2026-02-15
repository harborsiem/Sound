/*File AudioEvents01.java
The main purpose of this program is to
demonstrate audio line event handling.

This program demonstrates the capture and
subsequent playback of audio data, and
demonstrates the instantiation and registration
of line event listeners as well.  The event
listeners display messages on the screen when
the various audio line events occur.

A GUI appears on the screen containing the
following buttons:
Capture
Stop
Playback

Input data from a microphone is captured and
saved in a ByteArrayOutputStream object when the
user clicks the Capture button.

Data capture stops when the user clicks the Stop
button.

Playback begins when the user clicks the Playback
button.

Following is the screen output following the
click on the Capture button.  Note that line
breaks were manually inserted in this, and the
other output material shown below, to cause the
material to fit this narrow format.

Event handler for TargetDataLine
Event type: Open
Line info: interface TargetDataLine supporting
 64 audio formats

Event handler for TargetDataLine
Event type: Start
Line info: interface TargetDataLine supporting
 64 audio formats



Following is the screen output following the
click on the Stop button.

Event handler for TargetDataLine
Event type: Stop
Line info: interface TargetDataLine supporting
 64 audio formats

Event handler for TargetDataLine
Event type: Close
Line info: interface TargetDataLine supporting
 64 audio formats



Following is the screen output following the
click on the Playback button.

Event handler for SourceDataLine
Event type: Open
Line info: interface SourceDataLine supporting
 8 audio formats

Event handler for SourceDataLine
Event type: Start
Line info: interface SourceDataLine supporting
 8 audio formats

Event handler for SourceDataLine
Event type: Stop
Line info: interface SourceDataLine supporting
 8 audio formats

Event handler for SourceDataLine
Event type: Close
Line info: interface SourceDataLine supporting
 8 audio formats

Tested using SDK 1.4.0 under Win2000
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

namespace AudioEvents01 {
    public partial class AudioEvents01 : Form {
        bool stopCapture = false;
        MemoryStream byteArrayOutputStream;
        AudioFormat audioFormat;
        ITargetDataLine targetDataLine;
        AudioInputStream audioInputStream;
        ISourceDataLine sourceDataLine;

        public AudioEvents01() {
            InitializeComponent();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            Trace.AutoFlush = true;
        }

        //This method captures audio input from a
        // microphone and saves it in a
        // ByteArrayOutputStream object.
        private void captureAudio() {
            try {
                //Get everything set up for capture
                audioFormat = getAudioFormat();
                DataLine.Info dataLineInfo =
                                    new DataLine.Info(
                                      typeof(ITargetDataLine),
                                      audioFormat);
                targetDataLine =
                       (ITargetDataLine)AudioSystem.getLine(
                                             dataLineInfo);

                //Register a line listener on the
                // TargetDataLine object
                targetDataLine.addLineListener(
                    new TargetLineListener()
                );//end addLineListener()

                //Create a thread to capture the
                // microphone data and start it running. It
                // will run until the Stop button is
                // clicked.
                Thread capture = new Thread(new CaptureThread(this).run);
                capture.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end captureAudio method

        class TargetLineListener : ILineListener {
            public void update(LineEvent e) {
                Trace.WriteLine(
                 "Event handler for TargetDataLine");
                Trace.WriteLine(
                       "Event type: " + e.getType());
                Trace.WriteLine("Line info: " +
                          e.getLine().getLineInfo());
                Trace.WriteLine("");//blank line
            }//end update
        }

        //This method plays back the audio
        // data that has been saved in the
        // ByteArrayOutputStream
        private void playAudio() {
            try {
                //Get everything set up for playback.
                //Get the previously-saved data into a
                // byte array object.
                byte[] audioData = byteArrayOutputStream.
                                             ToArray();
                byteArrayOutputStream.Close();

                //Get an input stream on the byte array
                // containing the data
                Stream byteArrayInputStream =
                                  new MemoryStream(
                                                audioData);
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

                sourceDataLine =
                       (ISourceDataLine)AudioSystem.getLine(dataLineInfo);


                //Register a line listener on the
                // SourceDataLine object
                sourceDataLine.addLineListener(
                  new SourceLineListener()
                );//end addLineListener()

                //Create a thread to play back the data and
                // start it running.  It will run until all
                // the data has been played back, at which
                // time it will automatically stop the
                // line and fire a Stop event.
                Thread play = new Thread(new PlayThread(this).run);
                play.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end playAudio

        class SourceLineListener : ILineListener {
            public void update(LineEvent e) {
                Trace.WriteLine(
                 "Event handler for SourceDataLine");
                Trace.WriteLine(
                       "Event type: " + e.getType());
                Trace.WriteLine("Line info: "
                        + e.getLine().getLineInfo());
                Trace.WriteLine("");//blank line
            }//end update
        }

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
        class CaptureThread { //extends Thread
            AudioEvents01 caller;
            //An arbitrary-size temporary holding buffer
            byte[] tempBuffer = new byte[10000];
            public CaptureThread(AudioEvents01 caller) {
                this.caller = caller;
            }

            public void run() {

                caller.byteArrayOutputStream =
                                 new MemoryStream();
                caller.stopCapture = false;
                try {
                    caller.targetDataLine.open(caller.audioFormat);
                    caller.targetDataLine.start();

                    //Loop until stopCapture is set by another
                    // thread that services the Stop button.
                    while (!caller.stopCapture) {
                        //Read data from the internal buffer of
                        // the data line.
                        int cnt = caller.targetDataLine.read(
                                              tempBuffer,
                                              0,
                                              tempBuffer.Length);
                        if (cnt > 0) {
                            //Save data in output stream object.
                            caller.byteArrayOutputStream.Write(
                                               tempBuffer, 0, cnt);
                        }//end if
                    }//end while
                    //caller.byteArrayOutputStream.Close();

                    caller.targetDataLine.stop();
                    caller.targetDataLine.close();

                }
                catch (Exception e) {
                    Trace.WriteLine(e);
                    Environment.Exit(0);
                }//end catch
            }//end run
        }//end inner class CaptureThread
        //=============================================//

        //Inner class to play back the data that was
        // saved.
        class PlayThread { //extends Thread
            AudioEvents01 caller;
            byte[] tempBuffer = new byte[10000];

            public PlayThread(AudioEvents01 caller) {
                this.caller = caller;
            }

            public void run() {
                try {
                    int cnt;

                    caller.sourceDataLine.open(caller.audioFormat);
                    caller.sourceDataLine.start();

                    //Loop until the input read method returns
                    // -1 for empty stream.
                    while ((cnt = caller.audioInputStream.Read(
                                            tempBuffer,
                                            0,
                                            tempBuffer.Length))
                                                        != -1) {
                        if (cnt > 0) {
                            //Write data to the internal buffer of
                            // the data line where it will be
                            // delivered to the speaker.
                            caller.sourceDataLine.write(
                                               tempBuffer, 0, cnt);
                        }//end if
                    }//end while
                    //Block and wait for internal buffer of the
                    // data line to become empty.  When it
                    // becomes empty, it will fire a Stop
                    // event and return.
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
        //=============================================//
		
        private void captureBtn_Click(object sender, EventArgs e) {
            captureBtn.Enabled = (false);
            stopBtn.Enabled = (true);
            playBtn.Enabled = (false);
            //Capture input data from the
            // microphone until the Stop button is
            // clicked.
            captureAudio();
        }

        private void stopBtn_Click(object sender, EventArgs e) {
            captureBtn.Enabled = (true);
            stopBtn.Enabled = (false);
            playBtn.Enabled = (true);
            //Terminate the capturing of input
            // data from the microphone.
            stopCapture = true;
        }

        private void playBtn_Click(object sender, EventArgs e) {
            //Play back all of the data that was
            // saved during capture.
            playAudio();
        }

    }//end outer class AudioEvents01.java
}

