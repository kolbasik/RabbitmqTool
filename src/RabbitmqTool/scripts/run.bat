@echo off
For /f "tokens=2-4 delims=/ " %%a in ('date /t') do (SET TIMESTAMP_DATE=%%c%%a%%b)
For /f "tokens=1-3 delims=/:/ " %%a in ('time /t') do (SET TIMESTAMP_TIME=%%a%%b%%c)
set TIMESTAMP_TIME=%TIMESTAMP_TIME: =%
SET TIMESTAMP=%TIMESTAMP_DATE%T%TIMESTAMP_TIME%
SET RABBITMQTOOL_JSON_FILE=%RABBITMQTOOL_ENV%_%TIMESTAMP%.json
@echo on

..\RabbitmqTool.exe --subject=schema --command=fetch > %RABBITMQTOOL_JSON_FILE%
type %RABBITMQTOOL_JSON_FILE%
..\RabbitmqTool.exe --subject=masstransit --command=validate -v
rem type %RABBITMQTOOL_JSON_FILE% | ..\RabbitmqTool.exe --subject=schema --command=restore -v