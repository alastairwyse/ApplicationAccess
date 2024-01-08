FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY ./ ./
RUN chmod +x ApplicationAccessComponentLauncher.sh
ENTRYPOINT ["/app/ApplicationAccessComponentLauncher.sh", "DistributedOperationCoordinatorNode"]