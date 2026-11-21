@echo off
setlocal
cd /d "%~dp0"
if not exist bin mkdir bin
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /codepage:65001 /target:winexe /optimize+ /win32icon:src\icone.ico /out:bin\VisualizadorEscPos.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll src\*.cs
if errorlevel 1 (
  echo Falha na compilacao.
  exit /b 1
)
echo Gerado: %~dp0bin\VisualizadorEscPos.exe
