## appsettings
  "ConnectionStrings": {
    "DefaultConnection": "Host=xxx:[port];Database=xxx;Username=xxx;Password=xxx"
  },

## Scaffold PostgreSQL
Scaffold-DbContext -Connection "Host=xxx;Database=xxx;Username=xxx;Password=xxx" Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir "Entity" -Context ModelContext -Schema "xxx" -DataAnnotations –NoPluralize -f -NoOnConfiguring

## Ex
[AspireApp](https://github.com/godsnew2542/AspireApp1) <br/> 
[DataLakeApp](https://github.com/godsnew2542/DataLakeApp) <br/> 
[psu_oracle_backEnd](https://github.com/godsnew2542/psu_oracle_backEnd) <br/> 

Req Nuget Backend
  * Microsoft.EntityFrameworkCore
  * Microsoft.EntityFrameworkCore.Tools
  * Npgsql.EntityFrameworkCore.PostgreSQL

```
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.11" />
<PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.21.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
<PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="7.2.0" />
```
