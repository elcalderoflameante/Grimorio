namespace Grimorio.Domain.Enums;

public enum PayrollAdjustmentType
{
    Income = 1,
    Deduction = 2
}

public enum PayrollAdjustmentCategory
{
    Overtime50 = 1,
    Overtime100 = 2,
    Bonus = 3,
    OtherIncome = 4,
    OtherDeduction = 5
}

public enum PayrollRoleStatus
{
    Generated = 1,
    Authorized = 2,
    Paid = 3
}

public enum PayrollRoleDetailType
{
    Income = 1,
    Deduction = 2
}
