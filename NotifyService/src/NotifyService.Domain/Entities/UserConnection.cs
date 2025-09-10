namespace NotifyService.Domain.Entities;

public class UserConnection
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public bool IsActive { get; set; }
    }