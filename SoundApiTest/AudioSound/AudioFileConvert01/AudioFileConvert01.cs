/*File AudioFileConvert01.java
Copyright 2003, R.G.Baldwin
This program demonstrates the ability to write a
Java program to convert one audio file type to a
different audio file type.
Usage: java AudioFileConvert01
                             inputFile outputFile
Output file type depends on the output file name
extension, such as au, wav, or aif.
Input file type does not depend on input file
name or extension.  Actual type of input file is
determined by the program irrespective of name
or extension.
You should be able to play the output file with
any standard media player that can handle the
file type, or with a program written in Java,
such as the program named AudioPlayer02 that was
discussed in an earlier lesson.
The following are sample screen outputs for
different input and output file types.  Note that
line breaks were manually inserted to force the
material to fit in this narrow publication
format.
In this example, the valid input wav file was
forced to have an invalid file extension.  The
wav file was successfully converted to
an au file.
java AudioFileConvert01 ringout.txt junk.au
Input file: ringout.txt
Output file: junk.au
Output type: au
Output type is supported
Input file format:
WAVE (.wav) file, byte length: 5212,
data format: PCM_UNSIGNED, 11025.0 Hz,
8 bit, mono, audio data
Bytes written: 5191
Output file format:
AU (.au) file, byte length: 5191,
data format: PCM_SIGNED, 11025.0 Hz,
8 bit, mono, audio data, frame length: 5167
In this example, the input file was a stereo au
file produced by a sample program from an earlier
lesson.  The au file was successfully converted
to a wav file.
java AudioFileConvert01 junk3.au junk.wav
Input file: junk3.au
Output file: junk.wav
Output type: wav
Output type is supported
Input file format:
AU (.au) file, byte length: 64024,
data format: PCM_SIGNED, 16000.0 Hz, 16 bit,
stereo, big-endian, audio data,
frame length: 16000
Bytes written: 64044
Output file format:
WAVE (.wav) file, byte length: 64044,
data format: PCM_SIGNED, 16000.0 Hz, 16 bit,
stereo, little-endian, audio data
In this example, the input file was a standard
Windows wav file, which was successfully
converted to an aif file.
java AudioFileConvert01 ringout.wav junk.aif
Input file: ringout.wav
Output file: junk.aif
Output type: aif
Output type is supported
Input file format:
WAVE (.wav) file, byte length: 5212,
data format: PCM_UNSIGNED, 11025.0 Hz, 8 bit,
mono, audio data
Bytes written: 5221
Output file format:
AIFF (.aif) file, byte length: 5221,
data format: PCM_SIGNED, 11025.0 Hz, 8 bit,
mono, audio data, frame length: 5167
In this example, the output file was specified
with an unsupported type.  Thus, the program
aborted, providing a list of the output file
types that are supported.
java AudioFileConvert01 junk3.au junk.xyz
Input file: junk3.au
Output file: junk.xyz
Output type: xyz
Output type not supported.
Supported audio file types: au aif wav
In this example, although the input file claimed
to be a wav file, it was not a valid audio file.
Rather, it was a text file that was renamed to
impersonate a wav file.  This caused the program
to throw a runtime exception and abort.
java AudioFileConvert01 invalidFile.wav junk.au
Input file: invalidFile.wav
Output file: junk.au
Output type: au
Output type is supported
javax.sound.sampled.
UnsupportedAudioFileException: could not get
audio input stream from input stream
at javax.sound.sampled.AudioSystem.
getAudioInputStream(AudioSystem.java:756)
at AudioFileConvert01.
main(AudioFileConvert01.java:84)
In this example, the program was run with no
command-line parameters, causing the program to
provide usage information and abort.
java AudioFileConvert01
Usage: java AudioFileConvert01
                            inputFile outputFile
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SystemX.Sound.Sampled;

namespace AudioFileConvert01 {
    class AudioFileConvert01 {
        public static void Main0(string[] args) {
            if (args.Length != 2) {
                Console.WriteLine(
                          "Usage: java AudioFileConvert01 "
                                 + "inputFile outputFile");
                Environment.Exit(0);
            }//end if
            Console.WriteLine("Input file: " + args[0]);
            Console.WriteLine("Output file: " + args[1]);
            //Output file type depends on output file
            // name extension.
            String outputTypeStr =
                               args[1].Substring(args[1].
                                   LastIndexOf(".") + 1);
            Console.WriteLine("Output type: "
                                        + outputTypeStr);
            AudioFileFormat.Type outputType =
                            getTargetType(outputTypeStr);
            if (outputType != null) {
                Console.WriteLine(
                               "Output type is supported");
            } else {
                Console.WriteLine(
                             "Output type not supported.");
                getTargetTypesSupported();
                Environment.Exit(0);
            }//end else
            //Note that input file type does not depend
            // on file name or extension.
            FileInfo inputFileObj = new FileInfo(args[0]);
            AudioInputStream audioInputStream = null;
            try {
                audioInputStream = AudioSystem.
                         getAudioInputStream(inputFileObj);
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
                Environment.Exit(0);
            }//end catch
            Console.WriteLine("Input file format:");
            showFileType(inputFileObj);
            int bytesWritten = 0;
            try {
                bytesWritten = AudioSystem.
                                  write(audioInputStream,
                                        outputType,
                                        new FileInfo(args[1]));
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
                Environment.Exit(0);
            }//end catch
            Console.WriteLine("Bytes written: "
                                         + bytesWritten);
            Console.WriteLine("Output file format:");
            showFileType(new FileInfo(args[1]));
        }//end main

        private static void getTargetTypesSupported() {
            AudioFileFormat.Type[] typesSupported =
                         AudioSystem.getAudioFileTypes();
            Console.Write(
                          "Supported audio file types:");
            for (int i = 0; i < typesSupported.Length;
                                                    i++) {
                Console.Write(" " +
                         typesSupported[i].getExtension());
            }//end for loop
            Console.WriteLine();
        }//end getTargetTypesSupported
        private static AudioFileFormat.Type
                       getTargetType(String extension) {
            AudioFileFormat.Type[] typesSupported =
                         AudioSystem.getAudioFileTypes();
            for (int i = 0; i < typesSupported.Length;
                                                    i++) {
                if (typesSupported[i].getExtension().
                                        Equals(extension)) {
                    return typesSupported[i];
                }//end if
            }//end for loop
            return null;//no match
        }//end getTargetType

        private static void showFileType(FileInfo file) {
            try {
                Console.WriteLine(AudioSystem.
                                 getAudioFileFormat(file));
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
                Environment.Exit(0);
            }//end catch
        }//end showFileFormat
    }//end class
}
