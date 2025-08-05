    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using BlogSite.Data;
    using BlogSite.Models;

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false; // Optional: for easier dev
        })
        .AddRoles<IdentityRole>() // Add support for roles
        .AddEntityFrameworkStores<ApplicationDbContext>();
    
    builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
    
    // ✅ Register IHttpClientFactory here
    builder.Services.AddHttpClient();

    var app = builder.Build();
    
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Create "Admin" role if not exists
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        // Create an admin user
        var adminEmail = builder.Configuration["AdminUser:Email"];
        var adminPassword = builder.Configuration["AdminUser:Password"];
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    
    app.UseAuthentication(); // Add this!
    app.UseAuthorization();

    app.MapStaticAssets();
    
    app.MapControllerRoute(
        name: "blogDetail",
        pattern: "blog/{slug}",
        defaults: new { controller = "Blog", action = "Detail" });
    
    app.MapControllerRoute(
            name: "default", 
            pattern: "{controller=Blog}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapRazorPages()
        .WithStaticAssets();

    await app.RunAsync(); // instead of app.Run()