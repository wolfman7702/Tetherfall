@echo off
setlocal

REM === Change this to your repo folder ===
set REPO=D:\Projects\Climbing Game\Tetherfall

where git >nul 2>&1 || (echo Git is not in PATH. Install Git and reopen. & pause & exit /b 1)
if not exist "%REPO%\.git" (echo Repo not found at "%REPO%". Fix REPO path in this .bat. & pause & exit /b 1)

cd /d "%REPO%"

echo.
echo === Pulling latest from origin/main ===
git status -sb
echo.
echo Stashing local changes (if any)...
git stash push -u -m "auto-stash before pull" >nul

git pull --rebase --autostash origin main || (
  echo.
  echo Pull/rebase failed. Resolve conflicts, then run: git rebase --continue
  git status
  pause
  exit /b 1
)

echo.
echo Fetching LFS files...
git lfs pull

echo.
echo Done. Your local is now up to date.
pause
