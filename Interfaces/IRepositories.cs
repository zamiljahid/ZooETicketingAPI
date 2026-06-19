using ZooTicketing.API.DTOs;

namespace ZooTicketing.API.Interfaces;

public interface IAuthRepository {
    Task<LoginResponse?> LoginAsync(LoginRequest req);
    Task<bool>           RegisterAsync(RegisterRequest req);
    Task<UserLookupDto?> FindByContactAsync(string query);
}

public interface ITicketTypeRepository {
    Task<IEnumerable<TicketTypeDto>> GetAllAsync();
    Task<TicketTypeDto?>             GetByIdAsync(int id);
    Task<TicketTypeDto>              CreateAsync(TicketTypeRequest req);
    Task<bool>                       UpdateAsync(int id, TicketTypeRequest req);
    Task<bool>                       DeleteAsync(int id);
}

public interface IGateRepository {
    Task<IEnumerable<GateDto>> GetAllAsync();
    Task<GateDto?>             GetByIdAsync(int id);
    Task<GateDto>              CreateAsync(GateRequest req);
    Task<bool>                 UpdateAsync(int id, GateRequest req);
    Task<bool>                 ToggleActiveAsync(int id, bool isActive);
    Task<bool>                 DeleteAsync(int id);
}

public interface ISalesControlRepository {
    Task<IEnumerable<SalesControlDto>> GetAllAsync();
    Task<SalesControlDto?>             GetByDateAsync(DateOnly date);
    Task<SalesControlDto>              UpsertAsync(SalesControlRequest req, int adminId);
    Task<bool>                         ToggleSalesAsync(DateOnly date, bool open, int adminId);
    Task<CapacityStatus?>              GetTodayCapacityAsync();
}

public interface IOrderRepository {
    Task<IEnumerable<OrderSummary>> GetAllAsync();
    Task<IEnumerable<OrderSummary>> GetByUserAsync(int userId);
    Task<OrderDetail?>              GetByIdAsync(int id);
    Task<OrderDetail>               CreateAsync(CreateOrderRequest req, int? issuedByUserId);
}

public interface ITicketRepository {
    Task<TicketDto?> GetByIdAsync(int id);
    Task<TicketDto?> GetByTokenAsync(Guid token);
    Task<bool>       CancelAsync(int id);
}

public interface IScanRepository {
    Task<ScanResponse>              ScanAsync(ScanRequest req);
    Task<IEnumerable<GateActivity>> GetActivityAsync(DateOnly? date);
}

public interface IAnalyticsRepository {
    Task<IEnumerable<DailySales>>   GetDailySalesAsync(DateOnly? from, DateOnly? to);
    Task<IEnumerable<GateActivity>> GetGateActivityAsync(DateOnly? date);
    Task<CapacityStatus?>           GetCapacityAsync(DateOnly date);
}
