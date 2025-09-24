@echo off
setlocal
set REPO=D:\Projects\Climbing Game\Tetherfall

where git >nul 2>&1 || (echo Git is not in PATH. Install Git and reopen. & pause & exit /b 1)
cd /d "%REPO%" || (echo Can't cd to "%REPO%". Fix REPO path in this .bat. & pause & exit /b 1)

echo --- STATUS BEFORE ---
git status -sb

echo --- STAGE ALL ---
git add -A

echo --- COMMIT (ok if nothing to commit) ---
git commit -m "auto: Josh push %date% %time%" || echo (Nothing to commit)

echo --- PULL (rebase) ---
git pull --rebase --autostash origin main || (echo Resolve conflicts, then rerun. & pause & exit /b 1)

echo --- LFS PULL ---
git lfs pull

echo --- PUSH ---
git push origin main

echo --- DONE ---
pause
