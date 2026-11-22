@echo off
setlocal
set "install=G:\DarksoulsMod\Tools\Smithbox-2.2.6-FenceFix"
if not exist "%install%\Smithbox.Vulkan.exe" goto :failed
copy /Y "%~dp0Smithbox.Release\Output\Libs\Smithbox.Program.dll" "%install%\Libs\Smithbox.Program.dll" || goto :failed
copy /Y "%~dp0README.md" "%install%\README.md" || goto :failed
echo Deployment complete.
exit /b 0

:failed
echo Deployment failed. Check the paths and close Smithbox before retrying.
pause
exit /b 1
