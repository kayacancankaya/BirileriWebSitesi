namespace BirileriWebSitesi.Models.Enums
{
    public class AprrovalStatus
    {
        public enum ApprovalStatus
        {
            Pending = 0,
            Approved = 1,
            Failed = 2,
            Shipped = 3,
            Recieved = 4,
            BankTransfer = 5,
            Cancelled = 6,
        }
    }
}
