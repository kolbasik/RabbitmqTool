@echo off

SET RABBITMQTOOL_ENV=localhost
SET RABBITMQTOOL_HOST=http://%RABBITMQTOOL_ENV%
SET RABBITMQTOOL_PORT=15672
SET RABBITMQTOOL_VHOST=/
SET RABBITMQTOOL_USERNAME=guest
SET RABBITMQTOOL_PASSWORD=guest

@echo on

run.bat

pause