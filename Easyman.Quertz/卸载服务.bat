@echo off

echo ע�⡮�������ơ������ '*.exe'�Ĳ��
set SvcName=Easyman.Quartz.exe

echo ж�ط���%SvcName%
%~dp0\%SvcName% uninstall

echo.
pause 