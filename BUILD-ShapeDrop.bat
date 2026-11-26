@echo off
chcp 65001 >nul
title Shape Drop - Build
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.10f1\Editor\Unity.exe"
set "PROJ=%~dp0."
if not exist "%UNITY%" (
  echo Khong tim thay Unity 6.6 tai: %UNITY%
  pause & exit /b 1
)
echo ==========================================================
echo   Dang build Shape Drop bang Unity (chay ngam).
echo   Lan dau mat khoang 5-15 phut. Vui long KHONG dong cua so nay.
echo ==========================================================
"%UNITY%" -batchmode -quit -projectPath "%PROJ%" -executeMethod ShapeDropBuild.BuildAll -logFile "%~dp0unity-build.log"
echo.
if exist "%PROJ%\Builds\build-report.txt" type "%PROJ%\Builds\build-report.txt"
if exist "%PROJ%\Builds\Windows\ShapeDrop.exe" (
  echo.
  echo XONG! Game nam trong: %PROJ%\Builds\Windows\ShapeDrop.exe
  start "" "%PROJ%\Builds\Windows"
) else (
  echo.
  echo Build chua thanh cong. Xem file unity-build.log.
)
pause
