# You can also pull these images from DockerHub amazon/aws-lambda-dotnet:8
FROM mcr.microsoft.com/dotnet/sdk:9.0

# Set the image's internal work directory
WORKDIR /var/task
  
# Copy function code to Lambda-defined environment variable
COPY "bin/Release/lambda-publish"  .
  
# Set the entrypoint to the bootstrap
ENTRYPOINT ["/usr/bin/dotnet", "exec", "/var/task/Elastic.Documentation.Lambda.LinkIndexUploader.dll"]