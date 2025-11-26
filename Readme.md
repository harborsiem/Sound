# Welcome to the .NET Sound Library!

This is an implementation of the OpenJDK Java Sound Library ( > Version 8) for .NET Framework 4.6.2 and .NET8.
*.NET Sound* is available on NuGet, with Package Id *Sound*. For using the library Sound.dll you have two choices:

1. You have to copy the native C and C++ components CSound.dll to your application *.exe folder to additional folders x86 and x64. You have to do this by copying the NuGet Package folders lib\dotnet_version\x86  and  lib\dotnet_version\x64.
> !Note:  
> dotnet_version is 4.6.2 for .NET Framework 4.6.2
> dotnet_version is 8.0 for .NET8.
>
> This choice will be the best solution

2. You have to install SoundLib.msi first. The SoundLib.msi one can find in the Nuget package folder "lib". In the SoundLib.msi are x86 and x64 components in native C and C++ code which are used by the .NET Sound library.

### Using the .NET Sound

If you need low level functions for Audio or Midi like they are supported in the OpenJDK Java, then you are in the right project. You can use the documentation of the Java Sound Library (see links below). The differences to the Java Sound Library are shown later. The namespaces in the .NET Sound-library are ***SystemX.Sound.Sampled***, ***SystemX.Sound.Midi*** and ***SystemX.Media.Sound*** which are similar to the Java packages ***javax.sound.sampled***, ***javax.sound.midi*** and ***com.sun.media.sound***.

### Some links to Java Sound:

[Java Sound Programmer Guide (Oracle)](https://docs.oracle.com/javase/8/docs/technotes/guides/sound/programmer_guide/contents.html)

[Java Platform, Standard Edition 8
API Specification](https://docs.oracle.com/javase/8/docs/api/index.html)

[Java Sound Technology](https://docs.oracle.com/javase/8/docs/technotes/guides/sound/index.html)

[Java Sound API: Java Sound Demo](https://www.oracle.com/technetwork/java/index-139508.html)

[Dick Baldwin Programming Tutorials](http://www.dickbaldwin.com)

[The Midi Association](https://www.midi.org/)



### Differences to the Java Sound Library

Differences to the Java Sound Library are in naming of interfaces (midi, sampled, common, addon):

| Java Name:                     | .NET Name                |
| ------------------------------ | ------------------------ |
| ControllerEventListener        | IControllerEventListener |
| MetaEventListener	         | IMetaEventListener       |
| MidiChannel		         | IMidiChannel             |
| MidiDevice		         | IMidiDevice              |
| Receiver		         | IReceiver                |
| Sequencer		         | ISequencer               |
| Soundbank		         | ISoundbank               |
| Synthesizer		         | ISynthesizer             |
| Transmitter		         | ITransmitter             |
|                                |                          |
| Clip                           | IClip                    |
| DataLine                       | IDataLine                |
| Line                           | ILine                    |
| LineListener                   | ILineListener            |
| Mixer                          | IMixer                   |
| Port                           | IPort                    |
| SourceDataLine                 | ISourceDataLine          |
| TargetDataLine                 | ITargetDataLine          |
|                                |                          |
| AutoClosingClip                | IAutoClosingClip         |
| AutoConnectSequencer           | IAutoConnectSequencer    |
| LineMonitor in EventDispatcher | ILineMonitor in EventDispatcher |
| ReferenceCountingDevice        | IReferenceCountingDevice |
|                                |                          |
| AudioClip                      | IAudioClip               |
| EventListener                  | IEventListener           |
| Runnable                       | IRunnable                |
|                                |                          |

Differences in Naming of methods (maybe there are some more):

| Class Name:                    | Java Name:          | .NET Name              |
| ------------------------------ | --------------------| ---------------------- |
| DLSSampleLoop                  | getType()           | getLoopType()          |
| DLSSampleLoop                  | setType(...)        | setLoopType(...)       |
| RIFFReader                     | getType()           | getRiffType()          |
| RIFFReader                     | readByte            | readSByte              |
| RIFFReader                     | read()              | ReadByte()             |
| RIFFReader                     | read(...)           | Read(...)              |
| RIFFReader                     | close()             | Close()                |
| RIFFWriter                     | writeByte(...)      | WriteSByte(...)        |
| RIFFWriter                     | write(...)          | Write(...)             |
| RIFFWriter                     | write(...)          | Write(...)             |
| RIFFWriter                     | write(...)          | Write(...)             |
| RIFFWriter                     | close()             | Close()                |


### Necessary installations for building:

Visual Studio 2022 Community Edition (C++ must be installed !)
WiX Toolset v3.14 (WIX 3.14)
WiX Toolset Visual Studio 2022 Extension

Project Sound is build with Visual Studio 2022 Community Edition C#
Project CSound is build with Visual Studio 2022 Community Edition C++
