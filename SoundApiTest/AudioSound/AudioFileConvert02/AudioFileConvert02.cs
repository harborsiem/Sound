/*File AudioFileConvert02.java
Copyright 2003, R.G.Baldwin
This program demonstrates the ability to write a
Java program to convert one audio file type to a
different audio file type.  This is an updated
version of AudioFileConvert01 in which all
unnecessary code has been removed.
Usage: java AudioFileConvert02
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
Tested using SDK 1.4.1 under WinXP
************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using SystemX.Sound.Sampled;

namespace AudioFileConvert02 {
    class AudioFileConvert02 {
        public static void Main0(string[] args) {
            if (args.Length != 2) {
                Console.WriteLine(
                        "Usage: java AudioFileConvert02 "
                               + "inputFile outputFile");
                Environment.Exit(0);
            }//end if
            AudioFileFormat.Type outputType =
                 getTargetType(args[1].Substring(args[1].
                                  LastIndexOf(".") + 1));
            if (outputType == null) {
                Console.WriteLine(
                             "Output type not supported.");
                Environment.Exit(0);
            }//end else
            FileInfo inputFileObj = new FileInfo(args[0]);
            AudioInputStream audioInputStream = null;
            try {
                audioInputStream = AudioSystem.
                         getAudioInputStream(inputFileObj);
                AudioSystem.write(audioInputStream,
                                        outputType,
                                        new FileInfo(args[1]));
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
                Environment.Exit(0);
            }//end catch
        }//end main
        //-------------------------------------------//
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
        //-------------------------------------------//
    }//end class
}
