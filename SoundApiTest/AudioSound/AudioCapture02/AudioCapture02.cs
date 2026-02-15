/*File AudioCapture02.java
This program demonstrates the capture and 
subsequent playback of audio data.

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

This version of the program gets and  displays a
list of available mixers, producing the following
output:

Available mixers:
Java Sound Audio Engine
Microsoft Sound Mapper
Modem #0 Line Record
ESS Maestro

Thus, this machine had the four mixers listed 
above available at the time the program was run.

Then the program gets and uses one of the 
available mixers instead of simply asking for a 
compatible mixer as was the case in a previous 
version of the program.

Either of the following two mixers can be used in
this program:

Microsoft Sound Mapper
ESS Maestro

Neither of the following two mixers will work in
this program.  The mixers fail at runtime for 
different reasons:

Java Sound Audio Engine
Modem #0 Line Record

The Java Sound Audio Engine mixer fails due to a 
data format compatibility problem.

The Modem #0 Line Record mixer fails due to an 
"Unexpected Error"

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

namespace AudioCapture02 {
    public partial class AudioCapture02 : Form {
        bool stopCapture = false;
        MemoryStream byteArrayOutputStream;
        AudioFormat audioFormat;
        ITargetDataLine targetDataLine;
        AudioInputStream audioInputStream;
        ISourceDataLine sourceDataLine;
        //IPort port;

        public AudioCapture02() {
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
                //Get and display a list of
                // available mixers.
                Mixer.Info[] mixerInfo =
                                AudioSystem.getMixerInfo();
                Trace.WriteLine("Available mixers:");
                for (int cnt = 0; cnt < mixerInfo.Length;
                                                    cnt++) {
                    Trace.WriteLine(mixerInfo[cnt].getName());
                }//end for loop

                //Get everything set up for capture
                audioFormat = getAudioFormat();

                DataLine.Info dataLineInfo =
                                      new DataLine.Info(
                                      typeof(ITargetDataLine),
                                      audioFormat);

                //Select one of the available
                // mixers.
                IMixer mixer = AudioSystem.
                                    getMixer(mixerInfo[5]); //@Todo: adjust index to a microphone mixer

                //Get a TargetDataLine on the selected
                // mixer.
                targetDataLine = (ITargetDataLine)
                               mixer.getLine(dataLineInfo);
                //Prepare the line for use.
                targetDataLine.open(audioFormat);
                targetDataLine.start();

                //Create a thread to capture the microphone
                // data and start it running.  It will run
                // until the Stop button is clicked.
                CaptureThread myTh = new CaptureThread(this);
                Thread captureThread = new Thread(myTh.run);
                captureThread.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end captureAudio method

        //This method plays back the audio data that
        // has been saved in the ByteArrayOutputStream
        private void playAudio() {
            try {
                //Get everything set up for playback.
                //Get the previously-saved data into a byte
                // array object.
                byte[] audioData = byteArrayOutputStream.
                                             ToArray();
                byteArrayOutputStream.Close();

                //Get an input stream on the byte array
                // containing the data
                Stream byteArrayInputStream =
                       new MemoryStream(audioData);
                AudioFormat audioFormat = getAudioFormat();
                audioInputStream = new AudioInputStream(
                              byteArrayInputStream,
                              audioFormat,
                              audioData.Length / audioFormat.
                                           getFrameSize());
                
                //AudioSystem.write(audioInputStream, AudioFileFormat.Type.WAVE, new FileInfo("junk.wav"));
                //Port.Info pinf = Port.Info.SPEAKER;
                //port = (IPort)AudioSystem.getLine(pinf);
                //port.open();
                //SystemX.Sound.Sampled.Control[] control = port.getControls();
                //FloatControl c = (FloatControl)control[1];
                //float v = c.getValue();
                //Line.Info inf = port.getLineInfo();
                //port.close();

                DataLine.Info dataLineInfo =
                                      new DataLine.Info(
                                      typeof(ISourceDataLine),
                                      audioFormat);
                sourceDataLine = (ISourceDataLine)
                         AudioSystem.getLine(dataLineInfo);
                sourceDataLine.open(audioFormat);
                sourceDataLine.start();

                //Create a thread to play back the data and
                // start it  running.  It will run until
                // all the data has been played back.
                Thread playThread = new Thread(new PlayThread(this).run);
                playThread.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end playAudio

        //This method creates and returns an
        // AudioFormat object for a given set of format
        // parameters.  If these parameters don't work
        // well for you, try some of the other
        // allowable parameter values, which are shown
        // in comments following the declartions.
        private AudioFormat getAudioFormat() {
            float sampleRate = 11025.0F;
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
        //=============================================//

        //Inner class to capture data from microphone
        class CaptureThread { //extends Thread
            AudioCapture02 caller;
            //An arbitrary-size temporary holding buffer
            byte[] tempBuffer = new byte[10000];
            public CaptureThread(AudioCapture02 caller) {
                this.caller = caller;
            }

            public void run() {
                caller.byteArrayOutputStream =
                                 new MemoryStream();
                caller.stopCapture = false;
                try {//Loop until stopCapture is set by
                    // another thread that services the Stop
                    // button.
                    while (!caller.stopCapture) {
                        //Read data from the internal buffer of
                        // the data line.
                        int cnt = caller.targetDataLine.read(tempBuffer,
                                              0,
                                              tempBuffer.Length);
                        if (cnt > 0) {
                            //Save data in output stream object.
                            caller.byteArrayOutputStream.Write(tempBuffer,
                                                        0,
                                                        cnt);
                        }//end if
                    }//end while
                    //caller.byteArrayOutputStream.Close();
                    caller.targetDataLine.close(); //@ added
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
        class PlayThread {//extends Thread
            AudioCapture02 caller;
            byte[] tempBuffer = new byte[10000];

            public PlayThread(AudioCapture02 caller) {
                this.caller = caller;
            }
            public Object Clone() {
                return this.MemberwiseClone();
            }

            public void run() {
                try {
                    int cnt;
                    int bytesRead = 0;
                    //Keep looping until the input read method
                    // returns -1 for empty stream.
                    while ((cnt = caller.audioInputStream.Read(
                                    tempBuffer, 0,
                                    tempBuffer.Length)) >= 0) {
                        if (cnt > 0) {
                            bytesRead += cnt;
                            //Write data to the internal buffer of
                            // the data line where it will be
                            // delivered to the speaker.
                            caller.sourceDataLine.write(tempBuffer, 0, cnt);
                        }//end if
                    }//end while
                    //Block and wait for internal buffer of the
                    // data line to empty.
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
            //Terminate the capturing of input data
            // from the microphone.
            stopCapture = true;
        }

        private void playBtn_Click(object sender, EventArgs e) {
            //Play back all of the data that was
            // saved during capture.
            playAudio();
        }

        private void garbageCollect_Click(object sender, EventArgs e) {
            GC.Collect();
        }

    }//end outer class AudioCapture02.java
}
