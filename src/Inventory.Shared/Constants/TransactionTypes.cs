namespace Inventory.Shared.Constants;

public static class TransactionTypes
{
    public const string In = "IN";
    public const string Out = "OUT";
    public const string Adjustment = "ADJUSTMENT";
    public const string Transfer = "TRANSFER";
    public const string Return = "RETURN";
    public const string Damage = "DAMAGE";
    
    public static readonly string[] All = { In, Out, Adjustment, Transfer, Return, Damage };
    
    public static bool IsValid(string transactionType)
    {
        return All.Contains(transactionType);
    }
}
