/*File AudioSynth01.java
Copyright 2003, R.G.Baldwin

This program demonstrates the ability to create
synthetic audio data, and to play it back
immediately, or to store it in an AU file for
later playback.

A GUI appears on the screen containing the
following components in the North position:

Generate button
Play/File button
Elapsed time meter (JTextField)

Several radio buttons appear in the Center
position of the GUI.  Each radio button selects
a different format for synthetic audio data.

The South position of the GUI contains the
following components:

Listen radio button
File radio button
File Name text field

Select a radio button from the Center and click
the Generate button.  A short segment of
synthetic audio data will be generated and saved
in memory.  The segment length is two seconds
for monaural data and one second for stereo data,
at 16000 samp/sec and 16 bits per sample.

To listen to the audio data, select the Listen
radio button in the South position and click the
Play/File button.  You can listen to the data
repeatedly if you so choose.  In addition to
listening to the data, you can also save it in
an audio file.

To save the audio data in an audio file of type
AU, enter a file name (without extension) in the
text field in the South position, select the
File radio button in the South position, and
click the Play/File button.

You should be able to play the audio file back
with any standard media player that can handle
the AU file type, or with a program written in
Java, such as the program named AudioPlayer02
that was discussed in an earlier lesson.

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

namespace AudioSynth01 {
    public partial class AudioSynth01 : Form {
        //The following are general instance variables
        // used to create a SourceDataLine object.
        AudioFormat audioFormat;
        AudioInputStream audioInputStream;
        ISourceDataLine sourceDataLine;

        //The following are audio format parameters.
        // They may be modified by the signal generator
        // at runtime.  Values allowed by Java
        // SDK 1.4.1 are shown in comments.
        float sampleRate = 16000.0F;
        //Allowable 8000,11025,16000,22050,44100
        int sampleSizeInBits = 16;
        //Allowable 8,16
        int channels = 1;
        //Allowable 1,2
        bool signed = true;
        //Allowable true,false
        bool bigEndian = true;
        //Allowable true,false

        //A buffer to hold two seconds monaural and one
        // second stereo data at 16000 samp/sec for
        // 16-bit samples
        byte[] audioData = new byte[16000 * 4];

        public AudioSynth01() {
            InitializeComponent();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            Trace.AutoFlush = true;
        }
        //-------------------------------------------//

        //This method plays or files the synthetic
        // audio data that has been generated and saved
        // in an array in memory.
        private void playOrFileData() {
            try {
                //Get an input stream on the byte array
                // containing the data
                Stream byteArrayInputStream = new MemoryStream(audioData);

                //Get the required audio format
                audioFormat = new AudioFormat(
                                          sampleRate,
                                          sampleSizeInBits,
                                          channels,
                                          signed,
                                          bigEndian);

                //Get an audio input stream from the
                // ByteArrayInputStream
                audioInputStream = new AudioInputStream(
                              byteArrayInputStream,
                              audioFormat,
                              audioData.Length / audioFormat.
                              getFrameSize());

                //Get info on the required data line
                DataLine.Info dataLineInfo = new DataLine.Info(
                                      typeof(ISourceDataLine),
                                      audioFormat);

                //Decide whether to play the synthetic
                // data immediately, or to write it into
                // an audio file, based on the user
                // selection of the radio buttons in the
                // South of the GUI..
                if (listen.Checked) {
                    //Get a SourceDataLine object
                    sourceDataLine = (ISourceDataLine)
                                           AudioSystem.getLine(dataLineInfo);
                    //Create a thread to play back the data and
                    // start it running.  It will run until all
                    // the data has been played back
                    Thread listenThread = new Thread(new ListenThread(this).run);
                    listenThread.Start();
                } else {
                    //Disable buttons until existing data
                    // is written to the file.
                    generateBtn.Enabled = (false);
                    playOrFileBtn.Enabled = (false);

                    //Write the data to an output file with
                    // the name provided by the text field
                    // in the South of the GUI.
                    try {
                        AudioSystem.write(
                                  audioInputStream,
                                  AudioFileFormat.Type.AU,
                                  new FileInfo(fileName.Text + ".au"));
                    }
                    catch (Exception e) {
                        Trace.WriteLine(e.StackTrace);
                        Environment.Exit(0);
                    }//end catch
                    //Enable buttons for another operation
                    generateBtn.Enabled = (true);
                    playOrFileBtn.Enabled = (true);
                }//end else
            }
            catch (Exception e) {
                Trace.WriteLine(e.StackTrace);
                Environment.Exit(0);
            }//end catch
        }//end playOrFileData
        //=============================================//

        //Inner class to play back the data that was
        // saved.
        class ListenThread { //extends Thread
            AudioSynth01 caller;
            //This is a working buffer used to transfer
            // the data between the AudioInputStream and
            // the SourceDataLine.  The size is rather
            // arbitrary.
            byte[] playBuffer = new byte[16384];

            public ListenThread(AudioSynth01 caller) {
                this.caller = caller;
            }

            public void run() {
                try {
                    //Disable buttons while data is being
                    // played.
                    caller.Invoke((Action)delegate {
                        caller.generateBtn.Enabled = (false);
                        caller.playOrFileBtn.Enabled = (false);
                    }
                    );

                    //Open and start the SourceDataLine
                    caller.sourceDataLine.open(caller.audioFormat);
                    caller.sourceDataLine.start();

                    int cnt;
                    //Get beginning of elapsed time for
                    // playback
                    long startTime = Environment.TickCount;

                    //Transfer the audio data to the speakers
                    while ((cnt = caller.audioInputStream.Read(
                                            playBuffer, 0,
                                            playBuffer.Length))
                                                        != -1) {
                        //Keep looping until the input read
                        // method returns -1 for empty stream.
                        if (cnt > 0) {
                            //Write data to the internal buffer of
                            // the data line where it will be
                            // delivered to the speakers in real
                            // time
                            caller.sourceDataLine.write(playBuffer, 0, cnt);
                        }//end if
                    }//end while

                    //Block and wait for internal buffer of the
                    // SourceDataLine to become empty.
                    caller.sourceDataLine.drain();


                    //Get and display the elapsed time for
                    // the previous playback.
                    int elapsedTime = Environment.TickCount - (int)startTime;
                    caller.elapsedTimeMeter.Text = ("" + elapsedTime);

                    //Finish with the SourceDataLine
                    caller.sourceDataLine.stop();
                    caller.sourceDataLine.close();

                    //Re-enable buttons for another operation
                    caller.Invoke((Action)delegate {
                        caller.generateBtn.Enabled = (true);
                        caller.playOrFileBtn.Enabled = (true);
                    });
                }
                catch (Exception e) {
                    Trace.WriteLine(e.StackTrace);
                    Environment.Exit(0);
                }//end catch

            }//end run
        }//end inner class ListenThread
        //=============================================//

        //Inner signal generator class.

        //An object of this class can be used to
        // generate a variety of different synthetic
        // audio signals.  Each time the getSyntheticData
        // method is called on an object of this class,
        // the method will fill the incoming array with
        // the samples for a synthetic signal.
        class SynGen {
            AudioSynth01 caller;
            //Note:  Because this class uses a ByteBuffer
            // asShortBuffer to handle the data, it can
            // only be used to generate signed 16-bit
            // data.
            MemoryStream byteBuff; //@
            BigEndianBinaryWriter shortBuff;
            //ByteBuffer byteBuffer;
            //ShortBuffer shortBuffer;
            int byteLength;

            public SynGen(AudioSynth01 caller) {
                this.caller = caller;
            }

            internal void getSyntheticData(byte[] synDataBuffer) {
                //Prepare the ByteBuffer and the shortBuffer
                // for use

                byteBuff = new MemoryStream(synDataBuffer, 0, synDataBuffer.Length, true, true);
                shortBuff = new BigEndianBinaryWriter(byteBuff);
                //byteBuffer = ByteBuffer.wrap(synDataBuffer);
                //shortBuffer = byteBuffer.asShortBuffer();

                byteLength = synDataBuffer.Length;

                //Decide which synthetic data generator
                // method to invoke based on which radio
                // button the user selected in the Center of
                // the GUI.  If you add more methods for
                // other synthetic data types, you need to
                // add corresponding radio buttons to the
                // GUI and add statements here to test the
                // new radio buttons.  Make additions here
                // if you add new synthetic generator
                // methods.

                if (caller.tones.Checked) tones();
                if (caller.stereoPanning.Checked)
                    stereoPanning();
                if (caller.stereoPingpong.Checked)
                    stereoPingpong();
                if (caller.fmSweep.Checked) fmSweep();
                if (caller.decayPulse.Checked) decayPulse();
                if (caller.echoPulse.Checked) echoPulse();
                if (caller.waWaPulse.Checked) waWaPulse();

            }//end getSyntheticData method
            //-------------------------------------------//

            //This method generates a monaural tone
            // consisting of one sinusoids.
            void tones1() {
                caller.channels = 1;//Java allows 1 or 2
                //Each channel requires two 8-bit bytes per
                // 16-bit sample.
                caller.sampleSizeInBits = 8;
                int bytesPerSamp = 1;
                caller.sampleRate = 11025.0F;
                // Allowable 8000,11025,16000,22050,44100
                caller.signed = true;
                //Allowable true,false
                caller.bigEndian = false; //true;
                //Allowable true,false
                int sampLength = byteLength / bytesPerSamp;
                for (int cnt = 0; cnt < sampLength; cnt++) {
                    double time = cnt / caller.sampleRate;
                    double freq = 329.628;//arbitrary frequency
                    double sinValue =
                      (Math.Sin(2 * Math.PI * freq * time));
                    shortBuff.Write((sbyte)(127 * sinValue));
                    //shortBuffer.put((short)(16000 * sinValue));
                }//end for loop
            }//end method tones
            //-------------------------------------------//

            //This method generates a monaural tone
            // consisting of the sum of three sinusoids.
            void tones() {
                caller.channels = 1;//Java allows 1 or 2
                //Each channel requires two 8-bit bytes per
                // 16-bit sample.
                int bytesPerSamp = 2;
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                for (int cnt = 0; cnt < sampLength; cnt++) {
                    double time = cnt / caller.sampleRate;
                    double freq = 950.0;//arbitrary frequency
                    double sinValue =
                      (Math.Sin(2 * Math.PI * freq * time) +
                      Math.Sin(2 * Math.PI * (freq / 1.8) * time) +
                      Math.Sin(2 * Math.PI * (freq / 1.5) * time)) / 3.0;
                    shortBuff.Write((short)(16000 * sinValue));
                    //shortBuffer.put((short)(16000 * sinValue));
                }//end for loop
            }//end method tones
            //-------------------------------------------//

            //This method generates a stereo speaker sweep,
            // starting with a relatively high frequency
            // tone on the left speaker and moving across
            // to a lower frequency tone on the right
            // speaker.
            void stereoPanning() {
                caller.channels = 2;//Java allows 1 or 2
                int bytesPerSamp = 4;//Based on channels
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                for (int cnt = 0; cnt < sampLength; cnt++) {
                    //Calculate time-varying gain for each
                    // speaker
                    double rightGain = 16000.0 * cnt / sampLength;
                    double leftGain = 16000.0 - rightGain;

                    double time = cnt / caller.sampleRate;
                    double freq = 600;//An arbitrary frequency
                    //Generate data for left speaker
                    double sinValue =
                               Math.Sin(2 * Math.PI * (freq) * time);
                    shortBuff.Write((short)(leftGain * sinValue));
                    //Generate data for right speaker
                    sinValue = Math.Sin(2 * Math.PI * (freq * 0.8) * time);
                    shortBuff.Write((short)(rightGain * sinValue));
                }//end for loop
            }//end method stereoPanning
            //-------------------------------------------//

            //This method uses stereo to switch a sound
            // back and forth between the left and right
            // speakers at a rate of about eight switches
            // per second.  On my system, this is a much
            // better demonstration of the sound separation
            // between the two speakers than is the
            // demonstration produced by the stereoPanning
            // method.  Note also that because the sounds
            // are at different frequencies, the sound
            // produced is similar to that of U.S.
            // emergency vehicles.

            void stereoPingpong() {
                caller.channels = 2;//Java allows 1 or 2
                int bytesPerSamp = 4;//Based on channels
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                double leftGain = 0.0;
                double rightGain = 16000.0;
                for (int cnt = 0; cnt < sampLength; cnt++) {
                    //Calculate time-varying gain for each
                    // speaker
                    if (cnt % (sampLength / 8) == 0) {
                        //swap gain values
                        double temp = leftGain;
                        leftGain = rightGain;
                        rightGain = temp;
                    }//end if

                    double time = cnt / caller.sampleRate;
                    double freq = 600;//An arbitrary frequency
                    //Generate data for left speaker
                    double sinValue = Math.Sin(2 * Math.PI * (freq) * time);
                    shortBuff.Write((short)(leftGain * sinValue));
                    //Generate data for right speaker
                    sinValue = Math.Sin(2 * Math.PI * (freq * 0.8) * time);
                    shortBuff.Write((short)(rightGain * sinValue));
                }//end for loop
            }//end stereoPingpong method
            //-------------------------------------------//

            //This method generates a monaural linear
            // frequency sweep from 100 Hz to 1000Hz.
            void fmSweep() {
                caller.channels = 1;//Java allows 1 or 2
                int bytesPerSamp = 2;//Based on channels
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                double lowFreq = 100.0;
                double highFreq = 1000.0;

                for (int cnt = 0; cnt < sampLength; cnt++) {
                    double time = cnt / caller.sampleRate;

                    double freq = lowFreq +
                             cnt * (highFreq - lowFreq) / sampLength;
                    double sinValue = Math.Sin(2 * Math.PI * freq * time);
                    shortBuff.Write((short)(16000 * sinValue));
                }//end for loop
            }//end method fmSweep
            //-------------------------------------------//

            //This method generates a monaural triple-
            // frequency pulse that decays in a linear
            // fashion with time.
            void decayPulse() {
                caller.channels = 1;//Java allows 1 or 2
                int bytesPerSamp = 2;//Based on channels
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                for (int cnt = 0; cnt < sampLength; cnt++) {
                    //The value of scale controls the rate of
                    // decay - large scale, fast decay.
                    double scale = 2 * cnt;
                    if (scale > sampLength)
                        scale = sampLength;
                    double gain = 16000 * (sampLength - scale) / sampLength;
                    double time = cnt / caller.sampleRate;
                    double freq = 499.0;//an arbitrary freq
                    double sinValue =
                      (Math.Sin(2 * Math.PI * freq * time) +
                      Math.Sin(2 * Math.PI * (freq / 1.8) * time) +
                      Math.Sin(2 * Math.PI * (freq / 1.5) * time)) / 3.0;
                    shortBuff.Write((short)(gain * sinValue));
                }//end for loop
            }//end method decayPulse
            //-------------------------------------------//

            //This method generates a monaural triple-
            // frequency pulse that decays in a linear
            // fashion with time.  However, three echoes
            // can be heard over time with the amplitude
            // of the echoes also decreasing with time.
            void echoPulse() {
                caller.channels = 1;//Java allows 1 or 2
                int bytesPerSamp = 2;//Based on channels
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                int cnt2 = -8000;
                int cnt3 = -16000;
                int cnt4 = -24000;
                for (int cnt1 = 0; cnt1 < sampLength; cnt1++, cnt2++, cnt3++, cnt4++) {
                    double val = echoPulseHelper(cnt1, sampLength);
                    if (cnt2 > 0) {
                        val += 0.7 * echoPulseHelper(cnt2, sampLength);
                    }//end if
                    if (cnt3 > 0) {
                        val += 0.49 * echoPulseHelper(cnt3, sampLength);
                    }//end if
                    if (cnt4 > 0) {
                        val += 0.34 * echoPulseHelper(cnt4, sampLength);
                    }//end if

                    shortBuff.Write((short)val);
                }//end for loop
            }//end method echoPulse
            //-------------------------------------------//

            double echoPulseHelper(int cnt, int sampLength) {
                //The value of scale controls the rate of
                // decay - large scale, fast decay.
                double scale = 2 * cnt;
                if (scale > sampLength)
                    scale = sampLength;
                double gain = 16000 * (sampLength - scale) / sampLength;
                double time = cnt / caller.sampleRate;
                double freq = 499.0;//an arbitrary freq
                double sinValue =
                  (Math.Sin(2 * Math.PI * freq * time) +
                  Math.Sin(2 * Math.PI * (freq / 1.8) * time) +
                  Math.Sin(2 * Math.PI * (freq / 1.5) * time)) / 3.0;
                return (short)(gain * sinValue);
            }//end echoPulseHelper

            //-------------------------------------------//

            //This method generates a monaural triple-
            // frequency pulse that decays in a linear
            // fashion with time.  However, three echoes
            // can be heard over time with the amplitude
            // of the echoes also decreasing with time.
            //Note that this method is identical to the
            // method named echoPulse, except that the
            // algebraic sign was switched on the amplitude
            // of two of the echoes before adding them to
            // the composite synthetic signal.  This
            // resulted in a difference in the
            // sound.
            void waWaPulse() {
                caller.channels = 1;//Java allows 1 or 2
                int bytesPerSamp = 2;//Based on channels
                caller.sampleRate = 16000.0F;
                // Allowable 8000,11025,16000,22050,44100
                int sampLength = byteLength / bytesPerSamp;
                int cnt2 = -8000;
                int cnt3 = -16000;
                int cnt4 = -24000;
                for (int cnt1 = 0; cnt1 < sampLength; cnt1++, cnt2++, cnt3++, cnt4++) {
                    double val = waWaPulseHelper(cnt1, sampLength);
                    if (cnt2 > 0) {
                        val += -0.7 * waWaPulseHelper(cnt2, sampLength);
                    }//end if
                    if (cnt3 > 0) {
                        val += 0.49 * waWaPulseHelper(cnt3, sampLength);
                    }//end if
                    if (cnt4 > 0) {
                        val += -0.34 * waWaPulseHelper(cnt4, sampLength);
                    }//end if

                    shortBuff.Write((short)val);
                }//end for loop
            }//end method waWaPulse
            //-------------------------------------------//

            double waWaPulseHelper(int cnt, int sampLength) {
                //The value of scale controls the rate of
                // decay - large scale, fast decay.
                double scale = 2 * cnt;
                if (scale > sampLength)
                    scale = sampLength;
                double gain = 16000 * (sampLength - scale) / sampLength;
                double time = cnt / caller.sampleRate;
                double freq = 499.0;//an arbitrary freq
                double sinValue =
                  (Math.Sin(2 * Math.PI * freq * time) +
                  Math.Sin(2 * Math.PI * (freq / 1.8) * time) +
                  Math.Sin(2 * Math.PI * (freq / 1.5) * time)) / 3.0;
                return (short)(gain * sinValue);
            }//end waWaPulseHelper

            //-------------------------------------------//
        }

        private void generateBtn_Click(object sender, EventArgs e) {
            //Don't allow Play during generation
            playOrFileBtn.Enabled = (false);
            //Generate synthetic data
            new SynGen(this).getSyntheticData(audioData);
            //Now it is OK for the user to listen
            // to or file the synthetic audio data.
            playOrFileBtn.Enabled = (true);
        }

        private void playOrFileBtn_Click(object sender, EventArgs e) {
            //Play or file the data synthetic data
            playOrFileData();
        }
        //end SynGen class
        //=============================================//

    }//end outer class AudioSynth01.java

}
