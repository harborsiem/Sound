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

If you need low level functions for Audio or Midi like they are supported in the OpenJDK Java, then you are in the right project. You can use the documentation of the Java Sound Library (see links below). The differences to the Java Sound Library are shown later. The namespaces in the .NET Sound-library are ***SystemX.Sound.Sampled***, ***SystemX.Sound.Midi*** and ***SystemX.Media.Sound*** which are similar to the Java packages ***javax.sound.sampled***, ***javax.sound.midi*** and ***com.sun.media.sound***. Don't use classes from namespace ***SystemX.Media.Sound*** directly.

