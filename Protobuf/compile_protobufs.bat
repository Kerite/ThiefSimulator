@echo off
protoc --csharp_out=../Models Inventory.proto House.proto Level.proto
pause