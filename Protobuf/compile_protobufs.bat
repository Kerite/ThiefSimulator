@echo off
protoc --csharp_out=../Models ./*.proto
pause