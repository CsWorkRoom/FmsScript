
@echo off

echo ע�⡮�������ơ������ '*.exe'�Ĳ��
set SvcName=Easyman.Quartz.exe

echo ��װ����%SvcName%
%~dp0\%SvcName% install

echo ��������%SvcName%
%~dp0\%SvcName% start

echo.
pause
