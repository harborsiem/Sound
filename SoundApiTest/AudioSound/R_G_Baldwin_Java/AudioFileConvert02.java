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
import java.io.*;
import javax.sound.sampled.*;
public class AudioFileConvert02{
  public static void main(String[] args){
    if(args.length != 2){
      System.out.println(
                "Usage: java AudioFileConvert02 "
                       + "inputFile outputFile");
      System.exit(0);
    }//end if
    AudioFileFormat.Type outputType =
         getTargetType(args[1].substring(args[1].
                          lastIndexOf(".") + 1));
    if(outputType == null){
      System.out.println(
                   "Output type not supported.");
      System.exit(0);
    }//end else
    File inputFileObj = new File(args[0]);
    AudioInputStream audioInputStream = null;
    try{
      audioInputStream = AudioSystem.
               getAudioInputStream(inputFileObj);
      AudioSystem.write(audioInputStream,
                              outputType,
                              new File(args[1]));
    }catch (Exception e){
      e.printStackTrace();
      System.exit(0);
    }//end catch
  }//end main
  //-------------------------------------------//
  private static AudioFileFormat.Type
                 getTargetType(String extension){
    AudioFileFormat.Type[] typesSupported =
                 AudioSystem.getAudioFileTypes();
    for(int i = 0; i < typesSupported.length;
                                            i++){
      if(typesSupported[i].getExtension().
                              equals(extension)){
        return typesSupported[i];
      }//end if
    }//end for loop
    return null;//no match
  }//end getTargetType
  //-------------------------------------------//
}//end class
