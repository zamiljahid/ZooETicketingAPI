using Npgsql;

namespace ZooTicketing.API.Repositories;

public abstract class BaseRepository
{
    protected readonly string ConnStr;
    protected BaseRepository(IConfiguration cfg) => ConnStr = cfg.GetConnectionString("DefaultConnection")!;
    protected NpgsqlConnection Conn() => new(ConnStr);
}
