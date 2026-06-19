using System.Data;
using Dapper;

namespace ZooTicketing.API.Repositories;

// Dapper 2.1.x has no built-in support for System.DateOnly / System.TimeOnly as
// command parameters, so any query that passes one throws
//   "The member <x> of type System.DateOnly cannot be used as a parameter value".
// These handlers teach Dapper to convert them to DateTime/TimeSpan on the way in
// and back again on the way out. Registered once at startup (see Program.cs);
// Dapper reuses them for the Nullable<T> variants automatically.

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateTime dt => DateOnly.FromDateTime(dt),
        DateOnly d => d,
        _ => DateOnly.Parse(value.ToString()!)
    };
}

public sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    public override TimeOnly Parse(object value) => value switch
    {
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        DateTime dt => TimeOnly.FromDateTime(dt),
        TimeOnly t => t,
        _ => TimeOnly.Parse(value.ToString()!)
    };
}
