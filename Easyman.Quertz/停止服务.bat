@echo off

echo 注意‘服务名称’与服务 '*.exe'的差别
set SvcName=Easyman.Quartz.exe

echo 停止服务%SvcName%
net stop %SvcName%

echo.
pause