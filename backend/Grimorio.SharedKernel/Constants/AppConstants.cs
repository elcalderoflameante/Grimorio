namespace Grimorio.SharedKernel.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin = "Administrador";
    }

    public static class Claims
    {
        public const string UserId = "UserId";
        public const string BranchId = "BranchId";
        public const string Permissions = "permissions";
        public const string FirstName = "FirstName";
        public const string LastName = "LastName";
        public const string Email = "email";
        public const string MicrosoftRole = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        public const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    }

    public static class Hubs
    {
        public const string TableServicePath = "/hubs/table-service";
        public const string KitchenPath = "/hubs/kitchen";
    }

    public static class Scheduling
    {
        public const string DefaultFreeDayColor = "#E8E8E8";
    }
}
