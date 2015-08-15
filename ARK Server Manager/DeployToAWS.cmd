powershell -ExecutionPolicy Bypass -File MakeLatestVersionTxt.ps1 -srcXml "publish\ARK Server Manager.application" -destFile "publish\latest.txt"
set AWS_DEFAULT_PROFILE=ASMPublisher

aws s3 cp publish\ s3://arkservermanager/release/ --recursive

@set /p xxx="Press enter to quit..."
