using Microsoft.EntityFrameworkCore;
using ZooTicketing.API.Models;

namespace ZooTicketing.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role>         Roles         { get; set; }
    public DbSet<User>         Users         { get; set; }
    public DbSet<TicketType>   TicketTypes   { get; set; }
    public DbSet<SalesControl> SalesControls { get; set; }
    public DbSet<Gate>         Gates         { get; set; }
    public DbSet<Order>        Orders        { get; set; }
    public DbSet<Ticket>       Tickets       { get; set; }
    public DbSet<ScanLog>      ScanLogs      { get; set; }
}
