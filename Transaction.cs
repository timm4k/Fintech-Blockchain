namespace FintechBlockchain;

public sealed record Transaction(
    Guid Id,
    string From,
    string To,
    decimal Amount,
    string Asset,
    DateTime TimeStamp);
