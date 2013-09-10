call "C:\Program Files\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86
echo ON

SET dstDir=c:\windows\system32
SET CrtDir=%CD%
cd %dstDir%
regasm.exe /unregister PriceRTDServer.dll
cd %CD%
pause