@echo off

echo ע�⡮�������ơ������ '*.exe'�Ĳ��

set SvcName=Easyman.Quartz��������
echo Service state: %SvcName%
sc query %SvcName%

echo.
pause