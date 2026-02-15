using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using SystemX.Sound.Sampled;

namespace AudioPlayer02 {
    public partial class AudioPlayer : Form {
        AudioFormat audioFormat;
        AudioInputStream audioInputStream;
        ISourceDataLine sourceDataLine;
        bool stopPlayback = false;

        public AudioPlayer() {
            InitializeComponent();
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            Trace.AutoFlush = true;
        }

        private void playBtn_Click(object sender, EventArgs e) {
            stopBtn.Enabled = (true);
            playBtn.Enabled = (false);
            playAudio(); //Play the file
        }

        private void stopBtn_Click(object sender, EventArgs e) {
            //Terminate playback before EOF
            stopPlayback = true;
        }
        //This method plays back audio data from an
        // audio file whose name is specified in the
        // text field.
        private void playAudio() {
            try {
                FileInfo soundFile =
                             new FileInfo(textField.Text);
                audioInputStream = AudioSystem.
                            getAudioInputStream(soundFile);
                audioFormat = audioInputStream.getFormat();
                systemOut.Text = (audioFormat.ToString());

                DataLine.Info dataLineInfo =
                                    new DataLine.Info(
                                      typeof(ISourceDataLine),
                                              audioFormat);

                sourceDataLine =
                       (ISourceDataLine)AudioSystem.getLine(
                                             dataLineInfo);

                //Create a thread to play back the data and
                // start it running.  It will run until the
                // end of file, or the Stop button is
                // clicked, whichever occurs first.
                // Because of the data buffers involved,
                // there will normally be a delay between
                // the click on the Stop button and the
                // actual termination of playback.
                Thread playThread =
                          new Thread(
                            new PlayThread(this).run);
                playThread.Start();
            }
            catch (Exception e) {
                Trace.WriteLine(e);
                Environment.Exit(0);
            }//end catch
        }//end playAudio


        //=============================================//
        //Inner class to play back the data from the
        // audio file.
        class PlayThread { //extends Thread
            AudioPlayer caller;
            byte[] tempBuffer = new byte[10000];

            public PlayThread(AudioPlayer caller) {
                this.caller = caller;
            }

            public void run() {
                try {
                    caller.sourceDataLine.open(caller.audioFormat);
                    caller.sourceDataLine.start();

                    int cnt;
                    //Keep looping until the input read method
                    // returns -1 for empty stream or the
                    // user clicks the Stop button causing
                    // stopPlayback to switch from false to
                    // true.
                    while ((cnt = caller.audioInputStream.Read(
                         tempBuffer, 0, tempBuffer.Length)) != -1
                                     && caller.stopPlayback == false) {
                        if (cnt > 0) {
                            //Write data to the internal buffer of
                            // the data line where it will be
                            // delivered to the speaker.
                            caller.sourceDataLine.write(
                                             tempBuffer, 0, cnt);
                        }//end if
                    }//end while
                    //Block and wait for internal buffer of the
                    // data line to empty.
                    caller.sourceDataLine.drain();
                    caller.sourceDataLine.close();

                    //Prepare to playback another file
                    caller.Invoke((Action)delegate {
                    caller.stopBtn.Enabled = (false);
                    caller.playBtn.Enabled = (true);
					});
                    caller.stopPlayback = false;
                }
                catch (Exception e) {
                    Trace.WriteLine(e);
                    Environment.Exit(0);
                }//end catch
            }//end run
        }//end inner class PlayThread
        //===================================//

    }
}
