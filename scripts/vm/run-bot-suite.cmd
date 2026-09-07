@echo off
REM Wrapper so the bot suite runs regardless of PowerShell execution policy / Mark-of-the-Web.
REM Usage:  scripts\vm\run-bot-suite.cmd  [-StartApi] [-ApiPort 5999] ...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-bot-suite.ps1" %*
