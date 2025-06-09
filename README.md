## appsettings
  "ConnectionStrings": {
    "DefaultConnection": "Host=xxx:[port];Database=xxx;Username=xxx;Password=xxx"
  },

## Scaffold PostgreSQL
Scaffold-DbContext -Connection "Host=xxx;Database=xxx;Username=xxx;Password=xxx" Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir "Entity" -Context ModelContext -Schema "xxx" -DataAnnotations –NoPluralize -f -NoOnConfiguring

## Ex
[AspireApp](https://github.com/godsnew2542/AspireApp1)
