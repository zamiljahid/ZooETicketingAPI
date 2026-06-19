using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZooTicketing.API.Models;

public enum OrderChannel  { online, counter }
public enum PayMethod     { cash, card, bkash, nagad }
public enum PayStatus     { paid, pending, refunded, failed }
public enum TicketStatus  { valid, used, cancelled, expired }
public enum ScanResult    { allowed, already_used, wrong_date, gate_inactive, outside_hours, invalid_token }

[Table("roles")]
public class Role {
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    public ICollection<User> Users { get; set; } = [];
}

[Table("users")]
public class User {
    [Key][Column("id")] public int Id { get; set; }
    [Column("role_id")] public int RoleId { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("email")] public string Email { get; set; } = "";
    [Column("phone")] public string? Phone { get; set; }
    [Column("password_hash")] public string PasswordHash { get; set; } = "";
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey("RoleId")] public Role? Role { get; set; }
}

[Table("ticket_types")]
public class TicketType {
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("price")] public decimal Price { get; set; }
}

[Table("sales_control")]
public class SalesControl {
    [Key][Column("id")] public int Id { get; set; }
    [Column("control_date")] public DateOnly ControlDate { get; set; }
    [Column("sales_open")] public bool SalesOpen { get; set; } = true;
    [Column("capacity")] public int? Capacity { get; set; }
    [Column("updated_by")] public int? UpdatedBy { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("gates")]
public class Gate {
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("open_time")] public TimeOnly OpenTime { get; set; }
    [Column("close_time")] public TimeOnly CloseTime { get; set; }
}

[Table("orders")]
public class Order {
    [Key][Column("id")] public int Id { get; set; }
    [Column("user_id")] public int? UserId { get; set; }
    [Column("issued_by_user_id")] public int? IssuedByUserId { get; set; }
    [Column("guest_name")] public string? GuestName { get; set; }
    [Column("guest_phone")] public string? GuestPhone { get; set; }
    [Column("issued_channel")] public string IssuedChannel { get; set; } = "";
    [Column("total_amount")] public decimal TotalAmount { get; set; }
    [Column("payment_status")] public string PaymentStatus { get; set; } = "pending";
    [Column("payment_method")] public string PaymentMethod { get; set; } = "";
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey("UserId")] public User? User { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = [];
}

[Table("tickets")]
public class Ticket {
    [Key][Column("id")] public int Id { get; set; }
    [Column("order_id")] public int OrderId { get; set; }
    [Column("ticket_type_id")] public int TicketTypeId { get; set; }
    [Column("ticket_token")] public Guid TicketToken { get; set; } = Guid.NewGuid();
    [Column("holder_name")] public string? HolderName { get; set; }
    [Column("visit_date")] public DateOnly VisitDate { get; set; }
    [Column("price_paid")] public decimal PricePaid { get; set; }
    [Column("status")] public string Status { get; set; } = "valid";
    [Column("issued_channel")] public string IssuedChannel { get; set; } = "";
    [Column("issued_at")] public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey("OrderId")] public Order? Order { get; set; }
    [ForeignKey("TicketTypeId")] public TicketType? TicketType { get; set; }
}

[Table("scan_logs")]
public class ScanLog {
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_id")] public int TicketId { get; set; }
    [Column("gate_id")] public int GateId { get; set; }
    [Column("scanned_by")] public int? ScannedBy { get; set; }
    [Column("scan_result")] public string ScanResult { get; set; } = "";
    [Column("scanned_at")] public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey("TicketId")] public Ticket? Ticket { get; set; }
    [ForeignKey("GateId")] public Gate? Gate { get; set; }
}
