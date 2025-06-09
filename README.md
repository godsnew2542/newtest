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

```cs
public class FarmerSvc(IDbContextFactory<ModelContext> dbContextFactory) : IFarmerSvc
{
    public async Task<List<VRubberFarmProduct>> GetListVRubberFarmProduct(int userId, DateTime? rubberSellDate)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();

        var query = context.VRubberFarmProduct
            .Where(c => c.UserId == userId)
            .AsQueryable();

        if (rubberSellDate != null)
        {
            query = query.Where(c => c.RubberSellDate!.Value.Year == rubberSellDate!.Value.Year && c.RubberSellDate!.Value.Month == rubberSellDate!.Value.Month && c.RubberSellDate!.Value.Day == rubberSellDate!.Value.Day);
        }

        return await query
            .OrderByDescending(x => x.RubberSellDate)
            .ThenBy(x => x.RubberCateId)
            .ToListAsync();
    }

    public async Task<bool> AddPointToFarmer(Users user)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        var tran = await context.Database.BeginTransactionAsync();
        try
        {
            var _user = await context.Users
          .Where(c => c.UserId == user.UserId).FirstOrDefaultAsync();

            if (_user != null)
            {
                _user.TotalPoint = _user.TotalPoint + 5;
                _user.UpdatedAt = DateTime.Now;

                context.UserRewardActivity.Add(new()
                {
                    ActivityTimestamp = DateTime.Now,
                    Description = "เพิ่มคะแนนจากการบันทึกผลผลิต",
                    PointsTransaction = 5,
                    UserId = _user.UserId,
                });

                await context.SaveChangesAsync();

                await tran.CommitAsync();
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
            await tran.RollbackAsync();
            return false;
        }
    }
}
```

```cs
builder.Services.AddDbContextFactory<ModelContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(true);

});
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<LogService>();
```

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    opt.OperationFilter<AuthorizeCheckOperationFilter>();

    opt.EnableAnnotations();
}
);

builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<TMDConfig>(builder.Configuration.GetSection("TMDAPI"));
builder.Services.AddDbContextFactory<ModelContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(true);

});

var TMDAPI = builder.Configuration.GetSection("TMDAPI");
builder.Services.AddHttpClient<IWeatherService, WeatherService>((opt) =>
{
    opt.BaseAddress = new Uri(TMDAPI["BaseAddress"]);
    opt.DefaultRequestHeaders.Add("Authorization", $"Bearer {TMDAPI["Token"]}");
});

var jwt = builder.Configuration.GetSection("JWT");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddJwtBearer(options =>
           {
               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuerSigningKey = true,
                   IssuerSigningKey = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(jwt["SecretKey"])),
                   ValidateIssuer = true,
                   ValidIssuer = jwt["Issuer"],
                   ValidateAudience = true,
                   ValidAudience = jwt["Audience"],
                   ValidateLifetime = true,
                   ClockSkew = TimeSpan.Zero
               };
               options.Events = new()
               {
                   OnAuthenticationFailed = context =>
                   {
                       return Task.CompletedTask;
                   },
                   OnTokenValidated = context =>
                   {
                       // Add breakpoint here to see why authentication failed
                       return Task.CompletedTask;
                   }
               };
           }
           );

var MyAllowSpecificOrigins = "*";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                      });
});


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```