FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Domain/Predictathon.Domain.csproj Domain/
COPY Application/Predictathon.Application.csproj Application/
COPY Infrastructure/Predictathon.Infrastructure.csproj Infrastructure/
COPY WebApi/Predictathon.WebApi.csproj WebApi/
RUN dotnet restore WebApi/Predictathon.WebApi.csproj

COPY Domain/ Domain/
COPY Application/ Application/
COPY Infrastructure/ Infrastructure/
COPY WebApi/ WebApi/
RUN dotnet publish WebApi/Predictathon.WebApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:7046
EXPOSE 7046
ENTRYPOINT ["dotnet", "Predictathon.WebApi.dll"]
