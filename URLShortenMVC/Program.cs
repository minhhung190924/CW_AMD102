// Add this route mapping in the endpoint configuration (after MapControllerRoute for default)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");