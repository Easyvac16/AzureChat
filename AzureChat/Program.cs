using AzureChat;
using Microsoft.EntityFrameworkCore;
using SignalRChat.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Logging.AddConsole();

/*builder.Services.AddDbContext<ChatContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FreeSQL")));*/

builder.Services.AddDbContext<ChatContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("FreeSQL"),
        new MySqlServerVersion(new Version(5, 5, 62)) // ����� ������ MySQL �������
    )
);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");



app.Run();
