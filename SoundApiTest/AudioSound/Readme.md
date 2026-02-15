# Welcome to the AudioSound!

In this directory are some example and test projects which are using .NET Sound library.
The programs are originally written by [R.G.Baldwin](https://www.dickbaldwin.com/) as Java programs to demonstrate different features of the Sound library. I have done the conversion to CSharp.

To prepare these examples for running you have to compile the Sound library for Debug mode first. After this you have to compile the examples you like.
Don't forget the additional copy or install for using the library Sound.dll. 
Easily you can call the Copy_C_Dlls.bat file.
Or you have two choices:

1. Download Nuget-Package *Sound* and unzip the package (e.g. by 7-Zip).
You have to copy the native C and C++ components CSound.dll to your application ".exe" folder to additional folders x86 and x64. You have to do this by copying the NuGet Package folders lib\dotnet_version\x86  and  lib\dotnet_version\x64.
> !Note:  
> dotnet_version is net462 for .NET Framework 4.6.2
> dotnet_version is net8.0 for .NET8.
>
> This choice will be the best solution

2. You have to install SoundLib.msi first. The SoundLib.msi one can find in the Nuget package folder "lib" or at the Release page of this repository. In the SoundLib.msi are x86 and x64 components in native C and C++ code which are used by the .NET Sound library.

Visual Studio 2022 Community Edition
 or
Visual Studio 2026 Community Edition
are needed to compiling the sources.

### AudioCapture01
WinForms program

### AudioCapture02
WinForms program

### AudioEvents01
WinForms program

### AudioFileConvert01
Console program

### AudioFileConvert02
Console program

### AudioPlayer02
WinForms program

### AudioRecorder02
WinForms program

### AudioRecorder03
WinForms program

### AudioSynth01
WinForms program

### AudioUlawEncodeDecode02
Console program

