# Seeds "Sample Cup" reference data into the db-seed compose service, after db-migrate has
# published the schema (and, via its Post-Deployment script, the fixed-GUID admin roles).
# Reuses the mssql/server image purely for its bundled sqlcmd18 binary - the same one already
# proven by the db service's own healthcheck - rather than betting on a separate tools image.
FROM mcr.microsoft.com/mssql/server:2022-latest

WORKDIR /scripts
COPY Scripts/Sample/*.sql ./

ENTRYPOINT ["/bin/sh", "-c", "/opt/mssql-tools18/bin/sqlcmd -C -S db,1433 -U sa -P \"$MSSQL_SA_PASSWORD\" -d Predictathon -i 00_RunAll.sql"]
