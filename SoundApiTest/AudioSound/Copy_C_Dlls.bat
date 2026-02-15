@echo off
if not exist x86\CSound.dll goto error1
if not exist x64\CSound.dll goto error2
xcopy sound.config AudioCapture01\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioCapture01\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture01\bin\Debug\net462\x64\ /y
xcopy sound.config AudioCapture01\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioCapture01\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture01\bin\Release\net462\x64\ /y
xcopy sound.config AudioCapture01\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioCapture01\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture01\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioCapture01\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioCapture01\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture01\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioCapture02\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioCapture02\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture02\bin\Debug\net462\x64\ /y
xcopy sound.config AudioCapture02\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioCapture02\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture02\bin\Release\net462\x64\ /y
xcopy sound.config AudioCapture02\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioCapture02\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture02\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioCapture02\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioCapture02\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioCapture02\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioEvents01\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioEvents01\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioEvents01\bin\Debug\net462\x64\ /y
xcopy sound.config AudioEvents01\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioEvents01\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioEvents01\bin\Release\net462\x64\ /y
xcopy sound.config AudioEvents01\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioEvents01\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioEvents01\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioEvents01\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioEvents01\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioEvents01\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioFileConvert01\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioFileConvert01\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert01\bin\Debug\net462\x64\ /y
xcopy sound.config AudioFileConvert01\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioFileConvert01\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert01\bin\Release\net462\x64\ /y
xcopy sound.config AudioFileConvert01\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioFileConvert01\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert01\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioFileConvert01\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioFileConvert01\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert01\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioFileConvert02\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioFileConvert02\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert02\bin\Debug\net462\x64\ /y
xcopy sound.config AudioFileConvert02\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioFileConvert02\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert02\bin\Release\net462\x64\ /y
xcopy sound.config AudioFileConvert02\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioFileConvert02\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert02\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioFileConvert02\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioFileConvert02\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioFileConvert02\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioPlayer02\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioPlayer02\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioPlayer02\bin\Debug\net462\x64\ /y
xcopy sound.config AudioPlayer02\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioPlayer02\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioPlayer02\bin\Release\net462\x64\ /y
xcopy sound.config AudioPlayer02\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioPlayer02\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioPlayer02\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioPlayer02\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioPlayer02\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioPlayer02\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioRecorder02\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioRecorder02\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder02\bin\Debug\net462\x64\ /y
xcopy sound.config AudioRecorder02\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioRecorder02\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder02\bin\Release\net462\x64\ /y
xcopy sound.config AudioRecorder02\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioRecorder02\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder02\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioRecorder02\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioRecorder02\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder02\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioRecorder03\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioRecorder03\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder03\bin\Debug\net462\x64\ /y
xcopy sound.config AudioRecorder03\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioRecorder03\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder03\bin\Release\net462\x64\ /y
xcopy sound.config AudioRecorder03\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioRecorder03\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder03\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioRecorder03\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioRecorder03\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioRecorder03\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioSynth01\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioSynth01\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioSynth01\bin\Debug\net462\x64\ /y
xcopy sound.config AudioSynth01\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioSynth01\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioSynth01\bin\Release\net462\x64\ /y
xcopy sound.config AudioSynth01\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioSynth01\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioSynth01\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioSynth01\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioSynth01\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioSynth01\bin\x86\Release\net462\x64\ /y

xcopy sound.config AudioUlawEncodeDecode02\bin\Debug\net462\ /y
xcopy x86\CSound.dll AudioUlawEncodeDecode02\bin\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioUlawEncodeDecode02\bin\Debug\net462\x64\ /y
xcopy sound.config AudioUlawEncodeDecode02\bin\Release\net462\ /y
xcopy x86\CSound.dll AudioUlawEncodeDecode02\bin\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioUlawEncodeDecode02\bin\Release\net462\x64\ /y
xcopy sound.config AudioUlawEncodeDecode02\bin\x86\Debug\net462\ /y
xcopy x86\CSound.dll AudioUlawEncodeDecode02\bin\x86\Debug\net462\x86\ /y
xcopy x64\CSound.dll AudioUlawEncodeDecode02\bin\x86\Debug\net462\x64\ /y
xcopy sound.config AudioUlawEncodeDecode02\bin\x86\Release\net462\ /y
xcopy x86\CSound.dll AudioUlawEncodeDecode02\bin\x86\Release\net462\x86\ /y
xcopy x64\CSound.dll AudioUlawEncodeDecode02\bin\x86\Release\net462\x64\ /y
goto end
:error1
@echo x86\CSound.dll does not exist
pause
goto end
:error2
@echo x64\CSound.dll does not exist
pause
:end
