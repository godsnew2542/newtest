using Blazored.LocalStorage;
using Blazored.SessionStorage;
using LoanApp.DatabaseModel.LoanEntities;
using LoanApp.DatabaseModel.ModelsEntitiesCentral;
using LoanApp.IServices;
using LoanApp.Model.Helper;
using LoanApp.Model.Settings;
using LoanApp.Services;
using LoanApp.Services.IServices;
using LoanApp.Services.IServices.LoanDb;
using LoanApp.Services.Services;
using LoanApp.Services.Services.LoanDb;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
//string connString = builder.Configuration.GetConnectionString("DefaultConnection")

#region Set connString
string connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new NullReferenceException(nameof(connString));

IConfiguration configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

int configCount = configuration.AsEnumerable().Count();

if (configCount == 0)
{
    string? envFilename = $"appsettings.Docker.json";
    
    var data = builder.Configuration
        .SetBasePath(Environment.CurrentDirectory)
        .AddJsonFile(envFilename, optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new NullReferenceException(nameof(connString));
}
else
{
    connString = connString.Replace("#host", configuration["host"]!);
    connString = connString.Replace("#port", configuration["port"]!);
    connString = connString.Replace("#service_name", configuration["service_name"]!);
    connString = connString.Replace("#user", configuration["user"]!);
    connString = connString.Replace("#PW", configuration["password"]!);
}


#endregion

#region Add services to the container.
builder.Services.AddAntDesign();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddBlazoredLocalStorage();
#endregion

#region Configure dir to Settings
builder.Services.Configure<AppSettings>
    (builder.Configuration.GetSection("AppSettings"));

builder.Services.Configure<EmailPsu>
    (builder.Configuration.GetSection("EmailPsu"));

builder.Services.Configure<FileUploadSetting>
    (builder.Configuration.GetSection("FileUploadSetting"));

builder.Services.Configure<ThaIdSettings>
    (builder.Configuration.GetSection("ThaIdSettings"));

builder.Services.Configure<DelAuth>
    (builder.Configuration.GetSection("DelAuth"));
#endregion 

#region AddScoped && AddHttpClient
builder.Services.AddHttpClient<IPsuoAuth2Services, PsuoAuth2Services>();
builder.Services.AddScoped<IUtilityServer, UtilityServer>();
builder.Services.AddScoped<IGeneratePdfService, GeneratePdfService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDateService, DateService>();
builder.Services.AddScoped<IEntitiesCentralService, EntitiesCentralService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IImportPaymentService, ImportPaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISaveFileAndImgService, SaveFileAndImgService>();
builder.Services.AddScoped<LoanApp.Services.IServices.LoanDb.IPsuLoan, LoanApp.Services.Services.LoanDb.PsuLoan>();
builder.Services.AddScoped<WordDocument.IServices.IWordOptions, WordDocument.Services.WordOptions>();
builder.Services.AddScoped<IMSWordService, MSWordService>();
builder.Services.AddScoped<IPsuCentral, PsuCentral>();

#endregion

#region AddDbContext
builder.Services.AddDbContext<ModelContext>
    (options =>
    {
        options.EnableServiceProviderCaching(true);
        options.UseOracle(connString,
            options => options.UseOracleSQLCompatibility("11"));
    });

builder.Services.AddDbContext<ModelContextCentral>
    (options =>
    {
        options.EnableServiceProviderCaching(true);
        options.UseOracle(connString,
            options => options.UseOracleSQLCompatibility("11"));
    });
#endregion

#region Up Size JavaScript interop calls
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 64 * 1024);

#endregion 

#region AddSignalR
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

#endregion

#region Cookie
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
{
    options.Cookie.Name = "myauth";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
#endregion


var app = builder.Build();

#region Set time zone
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(new CultureInfo(Utility.DateLanguage_EN))
});
#endregion

#region Check OS
bool isOsWindows = Utility.CheckOSisWindows();
var directorySeparatorChar = (isOsWindows ? Path.DirectorySeparatorChar : Path.AltDirectorySeparatorChar);
#endregion

#region folder ที่เก็บ File
string filesPath = (isOsWindows ?
    builder.Configuration.GetValue<string>("FileUploadSetting:Windows:PhysicalFilePath") :
    builder.Configuration.GetValue<string>("FileUploadSetting:Linux:PhysicalFilePath"));

string userManual_DIR = (isOsWindows ?
    builder.Configuration.GetValue<string>("FileUploadSetting:Windows:UserManual") :
    builder.Configuration.GetValue<string>("FileUploadSetting:Linux:UserManual"));

string RequestPath = builder.Configuration.GetValue<string>("AppSettings:RequestFilePath");
//string filesPath = builder.Configuration.GetValue<string>("AppSettings:PhysicalFilePath")
//string userManual_DIR = builder.Configuration.GetValue<string>("AppSettings:UserManual")
#endregion

#region check folder
Utility.CheckFolder(filesPath);
Utility.CheckFolder($"{filesPath}{directorySeparatorChar}{Utility.Files_DIR}");
Utility.CheckFolder($"{filesPath}{directorySeparatorChar}{Utility.DOC_DIR}");
Utility.CheckFolder($"{filesPath}{directorySeparatorChar}{Utility.TEMPLATE_DIR}");
//Utility.CheckFolder($"{filesPath}\\{Utility.Files_DIR}")
//Utility.CheckFolder($"{filesPath}\\{Utility.DOC_DIR}")
//Utility.CheckFolder($"{filesPath}\\{Utility.TEMPLATE_DIR}")
Utility.CheckFolder(userManual_DIR);
#endregion

#region UseFileServer
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(filesPath),
    RequestPath = new PathString(RequestPath),
    EnableDirectoryBrowsing = false
});
#endregion

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

#region Add Authentication
app.UseAuthentication();
app.UseAuthorization();
#endregion

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
