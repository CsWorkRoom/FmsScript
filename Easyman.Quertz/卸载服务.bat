@echo off

echo 注意‘服务名称’与服务 '*.exe'的差别
set SvcName=Easyman.Quartz.exe

echo 卸载服务%SvcName%
%~dp0\%SvcName% uninstall

echo.
pause 