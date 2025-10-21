namespace ct.backend.Domain.Enum
{
    // User and Role
    public enum UserRole { admin, owner, staff, client }
    public enum UserStatus { active, inactive, suspended }

    // Brand, Store, Floor, Room, Machine
    public enum BrandStatus { active, inactive }
    public enum StoreStatus { active, inactive }
    public enum FloorStatus { active, inactive, hidden }
    public enum RoomStatus { active, maintenance, hidden }
    public enum RoomType { normal, vip, streaming }
    public enum MachineStatus { available, busy, down, maintenance }
    public enum PricingStatus
    {
        Active,
        Inactive
    }

    // Booking
    public enum BookingStatus { reserved, checked_in, completed, cancelled, no_show }

    // Order
    public enum OrderStatus { pending, preparing, delivering, done, cancelled }

    // Invoice and Payment
    public enum InvoiceStatus { open, paid, @void }
    public enum PaymentMethod { cash, card, qr, wallet }
    public enum PaymentStatus { pending, captured, failed, refunded }

    // Revieew and Rating
    public enum ReviewVisibility { @public, hidden }
    public enum ReviewStatus { pending, approved, rejected }

    // Issue and Ticketing
    public enum IssueScope { store, system }
    public enum IssueCategory { machine, room, billing, booking, food, other }
    public enum IssuePriority { low, medium, high, urgent }
    public enum IssueStatus { open, in_progress, resolved, closed, rejected }

    // Chat and Notification
    public enum MessageType { text, system, alert }
    public enum MessageStatus { sent, delivered, read }
    public enum NotificationType { system, booking, order, payment, issue }
    public enum NotificationChannel { inapp, email, sms, push }
    public enum NotificationStatus { queued, sending, sent, failed, read }
    public enum Gender { male, female, other }

    public enum VoucherStatus
    {
        Active,
        Inactive,
        Expired
    }
}
