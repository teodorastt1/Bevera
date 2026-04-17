namespace Bevera.Helpers
{
    public static class PurchaseOrderStates
    {
        public const string Draft = "Draft";                       // чернова при админ
        public const string SentToDistributor = "SentToDistributor"; // изпратена към дистрибутор
        public const string InPreparation = "InPreparation";       // дистрибуторът я подготвя
        public const string SentToAdmin = "SentToAdmin";           // върната към админ с крайна цена
        public const string Paid = "Paid";                         // платена и заредена
        public const string Cancelled = "Cancelled";
    }
}