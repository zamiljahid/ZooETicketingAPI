namespace ZooTicketing.API.DTOs;

// Auth
public record LoginRequest(string Email, string Password);
public record LoginResponse(int UserId, string Name, string Email, string Role, string Token);
public record RegisterRequest(string Name, string Email, string Password, string? Phone);
public record UserLookupDto(int UserId, string Name, string Email, string? Phone);

// Ticket Types
public record TicketTypeDto(int Id, string Name, decimal Price);
public record TicketTypeRequest(string Name, decimal Price);

// Gates
public record GateDto(int Id, string Name, bool IsActive, string OpenTime, string CloseTime);
public record GateRequest(string Name, bool IsActive, string OpenTime, string CloseTime);

// Sales Control
public record SalesControlDto(int Id, DateOnly ControlDate, bool SalesOpen, int? Capacity, DateTime UpdatedAt);
public record SalesControlRequest(DateOnly ControlDate, bool SalesOpen, int? Capacity);
public record CapacityStatus(DateOnly Date, bool SalesOpen, int? MaxCapacity, int Sold, int? Remaining);
public record ToggleRequest(bool SalesOpen);

// Orders & Tickets
public record TicketLine(int TicketTypeId, string HolderName);
public record CreateOrderRequest(
    int? UserId, string? GuestName, string? GuestPhone,
    string IssuedChannel, string PaymentMethod,
    DateOnly VisitDate, List<TicketLine> Tickets);
public record TicketDto(int Id, Guid Token, string HolderName, string Type, decimal Price, DateOnly VisitDate, string Status, string Channel);
public record OrderSummary(int Id, string Channel, decimal Total, string PayStatus, string PayMethod, DateTime CreatedAt, int TicketCount);
public record OrderDetail(int Id, string? CustomerName, string? GuestName, string? GuestPhone, string Channel, decimal Total, string PayStatus, string PayMethod, DateTime CreatedAt, List<TicketDto> Tickets);

// Scan
public record ScanRequest(string TicketToken, int GateId, int? ScannedBy);
public record ScanResponse(bool Allowed, string Result, string? HolderName, string? TicketType, DateOnly? VisitDate, string? Gate, string Message);

// Analytics
public record DailySales(DateOnly Date, string TicketType, string Channel, int Count, decimal Revenue);
public record GateActivity(string Gate, string Result, int Total, DateOnly Date);
