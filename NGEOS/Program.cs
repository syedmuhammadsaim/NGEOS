using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NGEOS.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;

using Microsoft.AspNetCore.Authentication.Google;

using Microsoft.EntityFrameworkCore;

using NGEOS.Data;

using NGEOS.Helpers;

using NGEOS.Services;

using NGEOS.Services.Ai;

using NGEOS.Services.Ai.Grounding;

using NGEOS.Services.Ai.Orchestration;

using NGEOS.Services.Ai.Providers;
using NGEOS.Services.Payments.Configuration;
using NGEOS.Services.Payments.Core;
using NGEOS.Services.Payments.Gateways;
using NGEOS.Services.Payments.Orchestration;



// Load environment variables securely from root-level .env before builder initialization

DotEnvLoader.Load();



var builder = WebApplication.CreateBuilder(args);



// Add services to the container.

builder.Services.AddControllersWithViews();



// Register EF Core 10 DbContext with SQL Server

builder.Services.AddDbContext<ApplicationDbContext>(options =>

    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// Authentication & Session

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];

var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];



builder.Services.AddAuthentication(options =>

{

    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

})

.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>

{

    options.LoginPath = "/Account/Login";

    options.LogoutPath = "/Account/Logout";

    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromDays(7);

    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;

    options.Cookie.Name = "NGEOS_AuthSession";

})

.AddCookie("External", options =>

{

    options.Cookie.Name = "NGEOS_ExternalAuth";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);

})

.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>

{

    options.SignInScheme = "External";

    options.ClientId = !string.IsNullOrWhiteSpace(googleClientId) ? googleClientId : "placeholder-client-id";

    options.ClientSecret = !string.IsNullOrWhiteSpace(googleClientSecret) ? googleClientSecret : "placeholder-client-secret";

    options.CallbackPath = "/signin-google";

});



builder.Services.AddAuthorization(options =>

{

    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

});



builder.Services.AddSession(options =>

{

    options.IdleTimeout = TimeSpan.FromMinutes(60);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

});



builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSignalR();



// Email and Password Reset Services Configuration

builder.Services.Configure<EmailSettings>(options =>
{
    builder.Configuration.GetSection("Smtp").Bind(options);
    var envHost = builder.Configuration["SMTP_HOST"];
    if (!string.IsNullOrWhiteSpace(envHost)) options.Host = envHost;
    if (int.TryParse(builder.Configuration["SMTP_PORT"], out var envPort)) options.Port = envPort;
    var envFrom = builder.Configuration["SMTP_FROM"];
    if (!string.IsNullOrWhiteSpace(envFrom)) options.From = envFrom;
    var envUser = builder.Configuration["SMTP_USERNAME"];
    if (!string.IsNullOrWhiteSpace(envUser)) options.Username = envUser;
    var envPass = builder.Configuration["SMTP_PASSWORD"];
    if (!string.IsNullOrWhiteSpace(envPass)) options.Password = envPass;
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddScoped<PasswordResetService>();



// AI Chatbot Configuration & Multi-Provider Fallback Registration

builder.Services.Configure<AiChatbotOptions>(options =>

{

    options.PrimaryProvider = builder.Configuration["AI_PRIMARY_PROVIDER"] ?? "Groq";

    options.GroqApiKey = builder.Configuration["GROQ_API_KEY"] ?? string.Empty;

    options.GroqModel = builder.Configuration["GROQ_MODEL"] ?? "llama-3.3-70b-versatile";

    options.GeminiApiKey = builder.Configuration["GEMINI_API_KEY"] ?? string.Empty;

    options.GeminiModel = builder.Configuration["GEMINI_MODEL"] ?? "gemini-2.0-flash";



    if (int.TryParse(builder.Configuration["CHATBOT_MAX_HISTORY_MESSAGES"], out var maxHist))

        options.MaxHistoryMessages = maxHist;



    if (int.TryParse(builder.Configuration["CHATBOT_TIMEOUT_SECONDS"], out var timeoutSec))

        options.TimeoutSeconds = timeoutSec;

});



// Resilient Named HttpClients with Custom Timeouts

var chatbotTimeoutSec = int.TryParse(builder.Configuration["CHATBOT_TIMEOUT_SECONDS"], out var toSec) ? toSec : 15;

builder.Services.AddHttpClient(GroqAiService.HttpClientName, client =>

{

    client.Timeout = TimeSpan.FromSeconds(chatbotTimeoutSec);

});

builder.Services.AddHttpClient(GeminiAiService.HttpClientName, client =>

{

    client.Timeout = TimeSpan.FromSeconds(chatbotTimeoutSec);

});



// Register AI Services

builder.Services.AddScoped<GroqAiService>();

builder.Services.AddScoped<GeminiAiService>();

builder.Services.AddScoped<INgeosKnowledgeService, NgeosKnowledgeService>();

builder.Services.AddScoped<IAiChatbotService, AiFallbackService>();

// Configure Multi-Payment Options from .env / App Configuration
builder.Services.Configure<PaymentOptions>(options =>
{
    options.GlobalEnvironment = builder.Configuration["PAYMENT_GLOBAL_ENVIRONMENT"] ?? "Sandbox";
    options.DefaultCurrency = builder.Configuration["PAYMENT_DEFAULT_CURRENCY"] ?? "PKR";
    if (bool.TryParse(builder.Configuration["PAYMENT_SIMULATION_MODE"], out var pSimMode))
        options.SimulationMode = pSimMode;
    else
        options.SimulationMode = true;
    if (int.TryParse(builder.Configuration["PAYMENT_TIMEOUT_SECONDS"], out var pTimeout))
        options.TimeoutSeconds = pTimeout;

    // Safepay
    options.Safepay.ApiKey = builder.Configuration["SAFEPAY_API_KEY"] ?? string.Empty;
    options.Safepay.SecretKey = builder.Configuration["SAFEPAY_SECRET_KEY"] ?? string.Empty;
    options.Safepay.WebhookSecret = builder.Configuration["SAFEPAY_WEBHOOK_SECRET"] ?? string.Empty;
    options.Safepay.Environment = builder.Configuration["SAFEPAY_ENVIRONMENT"] ?? options.GlobalEnvironment;

    // JazzCash
    options.JazzCash.MerchantId = builder.Configuration["JAZZCASH_MERCHANT_ID"] ?? string.Empty;
    options.JazzCash.Password = builder.Configuration["JAZZCASH_PASSWORD"] ?? string.Empty;
    options.JazzCash.IntegritySalt = builder.Configuration["JAZZCASH_INTEGRITY_SALT"] ?? string.Empty;
    options.JazzCash.Environment = builder.Configuration["JAZZCASH_ENVIRONMENT"] ?? options.GlobalEnvironment;
    options.JazzCash.ReturnUrl = builder.Configuration["JAZZCASH_RETURN_URL"] ?? string.Empty;

    // Easypaisa
    options.Easypaisa.StoreId = builder.Configuration["EASYPAISA_STORE_ID"] ?? string.Empty;
    options.Easypaisa.Username = builder.Configuration["EASYPAISA_USERNAME"] ?? string.Empty;
    options.Easypaisa.Password = builder.Configuration["EASYPAISA_PASSWORD"] ?? string.Empty;
    options.Easypaisa.SecretKey = builder.Configuration["EASYPAISA_SECRET_KEY"] ?? string.Empty;
    options.Easypaisa.Environment = builder.Configuration["EASYPAISA_ENVIRONMENT"] ?? options.GlobalEnvironment;
    options.Easypaisa.PostBackUrl = builder.Configuration["EASYPAISA_POSTBACK_URL"] ?? string.Empty;

    // PayFast
    options.PayFast.MerchantId = builder.Configuration["PAYFAST_MERCHANT_ID"] ?? string.Empty;
    options.PayFast.SecuredKey = builder.Configuration["PAYFAST_SECURED_KEY"] ?? string.Empty;
    options.PayFast.WebhookSecret = builder.Configuration["PAYFAST_WEBHOOK_SECRET"] ?? string.Empty;
    options.PayFast.Environment = builder.Configuration["PAYFAST_ENVIRONMENT"] ?? options.GlobalEnvironment;
});

// Register Named Resilient HttpClients for Payment Gateways
builder.Services.AddHttpClient(SafepayPaymentService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "NGEOS-Payment-Engine/1.0");
});
builder.Services.AddHttpClient(JazzCashPaymentService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "NGEOS-Payment-Engine/1.0");
});
builder.Services.AddHttpClient(EasypaisaPaymentService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "NGEOS-Payment-Engine/1.0");
});
builder.Services.AddHttpClient(PayFastPaymentService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "NGEOS-Payment-Engine/1.0");
});

// Register Multi-Payment Gateway Services & Factory
builder.Services.AddScoped<SafepayPaymentService>();
builder.Services.AddScoped<JazzCashPaymentService>();
builder.Services.AddScoped<EasypaisaPaymentService>();
builder.Services.AddScoped<PayFastPaymentService>();

builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<SafepayPaymentService>());
builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<JazzCashPaymentService>());
builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<EasypaisaPaymentService>());
builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<PayFastPaymentService>());

builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

// Register Orchestration, Verification and Webhook Services
builder.Services.AddScoped<IPaymentOrchestrationService, PaymentOrchestrationService>();
builder.Services.AddScoped<IPaymentVerificationService, PaymentVerificationService>();
builder.Services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();



var app = builder.Build();



// Seed initial database data (Guarded: Only in Development or if explicitly enabled by configuration)

var autoInitializeDb = app.Configuration.GetValue<bool>("DatabaseSettings:AutoInitialize", false);

if (autoInitializeDb)

{

    using (var scope = app.Services.CreateScope())

    {

        var services = scope.ServiceProvider;

        try

        {

            var context = services.GetRequiredService<ApplicationDbContext>();

            DbInitializer.Initialize(context);

        }

        catch (Exception ex)

        {

            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogError(ex, "An error occurred while seeding the NGEOS database.");

        }

    }

}



// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment())

{

    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();

}



if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT")) || 

    app.Configuration.GetValue<bool>("EnableHttpsRedirection", false))

{

    app.UseHttpsRedirection();

}

app.UseStaticFiles();



app.UseRouting();



app.UseSession();

app.UseAuthentication();

app.UseAuthorization();



app.MapHub<NgeosRealtimeHub>("/hubs/ngeos");

app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();

