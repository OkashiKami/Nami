@echo off
for /F "tokens=2" %%i in ('date /t') do set mydate=%%i
set mytime=%time%

git add *
git commit --all -m "Auto Push %mydate%"
git push
echo closeing in 5s
sleep 5000