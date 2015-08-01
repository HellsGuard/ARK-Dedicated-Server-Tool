set AWS_DEFAULT_PROFILE=ASMPublisher

aws s3 cp publish\ s3://arkservermanager/release/ --recursive

@set /p xxx="Press enter to quit..."
