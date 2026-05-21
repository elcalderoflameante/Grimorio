namespace Grimorio.SharedKernel.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin = "Administrador";
        public const string Supervisor = "Supervisor";
        public const string Cashier = "Cajero";
        public const string Waiter = "Mesero";
        public const string Kitchen = "Cocina";
        public const string Warehouse = "Bodega";
        public const string Purchases = "Compras";
        public const string Accounting = "Contabilidad";
        public const string HumanResources = "RRHH";
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

    public static class Permissions
    {
        public const string AdminUsersView = "Admin.Users.View";
        public const string AdminUsersCreate = "Admin.Users.Create";
        public const string AdminUsersUpdate = "Admin.Users.Update";
        public const string AdminUsersDelete = "Admin.Users.Delete";
        public const string AdminRolesView = "Admin.Roles.View";
        public const string AdminRolesCreate = "Admin.Roles.Create";
        public const string AdminRolesUpdate = "Admin.Roles.Update";
        public const string AdminRolesDelete = "Admin.Roles.Delete";
        public const string AdminPermissionsView = "Admin.Permissions.View";
        public const string AdminPermissionsManage = "Admin.Permissions.Manage";
        public const string AdminBranchView = "Admin.Branch.View";
        public const string AdminBranchUpdate = "Admin.Branch.Update";

        public const string RrhhEmployeesView = "RRHH.Employees.View";
        public const string RrhhEmployeesCreate = "RRHH.Employees.Create";
        public const string RrhhEmployeesUpdate = "RRHH.Employees.Update";
        public const string RrhhEmployeesDelete = "RRHH.Employees.Delete";
        public const string RrhhPositionsView = "RRHH.Positions.View";
        public const string RrhhPositionsManage = "RRHH.Positions.Manage";
        public const string RrhhSchedulingView = "RRHH.Scheduling.View";
        public const string RrhhSchedulingManage = "RRHH.Scheduling.Manage";
        public const string RrhhPayrollView = "RRHH.Payroll.View";
        public const string RrhhPayrollManage = "RRHH.Payroll.Manage";

        public const string PosOrdersView = "POS.Orders.View";
        public const string PosOrdersCreate = "POS.Orders.Create";
        public const string PosOrdersUpdate = "POS.Orders.Update";
        public const string PosOrdersCancel = "POS.Orders.Cancel";
        public const string PosKitchenView = "POS.Kitchen.View";
        public const string PosKitchenUpdate = "POS.Kitchen.Update";
        public const string PosStationsView = "POS.Stations.View";
        public const string PosStationsManage = "POS.Stations.Manage";
        public const string PosTablesView = "POS.Tables.View";
        public const string PosTablesManage = "POS.Tables.Manage";
        public const string PosTableRequestsView = "POS.TableRequests.View";
        public const string PosTableRequestsUpdate = "POS.TableRequests.Update";

        public const string MenuCategoriesView = "Menu.Categories.View";
        public const string MenuCategoriesManage = "Menu.Categories.Manage";
        public const string MenuItemsView = "Menu.Items.View";
        public const string MenuItemsManage = "Menu.Items.Manage";
        public const string MenuStockConsume = "Menu.StockConsume";

        public const string InventoryConfigView = "Inventory.Config.View";
        public const string InventoryConfigManage = "Inventory.Config.Manage";
        public const string InventoryArticlesView = "Inventory.Articles.View";
        public const string InventoryArticlesManage = "Inventory.Articles.Manage";
        public const string InventoryStockView = "Inventory.Stock.View";
        public const string InventoryMovementsView = "Inventory.Movements.View";
        public const string InventoryMovementsCreate = "Inventory.Movements.Create";

        public const string PurchasesSuppliersView = "Purchases.Suppliers.View";
        public const string PurchasesSuppliersManage = "Purchases.Suppliers.Manage";
        public const string PurchasesOrdersView = "Purchases.Orders.View";
        public const string PurchasesOrdersCreate = "Purchases.Orders.Create";
        public const string PurchasesOrdersUpdate = "Purchases.Orders.Update";
        public const string PurchasesOrdersCancel = "Purchases.Orders.Cancel";
        public const string PurchasesOrdersDelete = "Purchases.Orders.Delete";

        public const string BillingCustomersView = "Billing.Customers.View";
        public const string BillingCustomersManage = "Billing.Customers.Manage";
        public const string BillingCashView = "Billing.Cash.View";
        public const string BillingCashOpen = "Billing.Cash.Open";
        public const string BillingCashClose = "Billing.Cash.Close";
        public const string BillingCashCharge = "Billing.Cash.Charge";
        public const string BillingPaymentMethodsView = "Billing.PaymentMethods.View";
        public const string BillingPaymentMethodsManage = "Billing.PaymentMethods.Manage";
        public const string BillingTaxView = "Billing.Tax.View";
        public const string BillingTaxManage = "Billing.Tax.Manage";
        public const string BillingSriView = "Billing.Sri.View";
        public const string BillingSriManage = "Billing.Sri.Manage";
        public const string BillingSriGenerate = "Billing.Sri.Generate";

        public static readonly IReadOnlyList<PermissionDefinition> All =
        [
            new(AdminUsersView, "Ver usuarios", "Admin"),
            new(AdminUsersCreate, "Crear usuarios", "Admin"),
            new(AdminUsersUpdate, "Editar usuarios y asignar roles", "Admin"),
            new(AdminUsersDelete, "Eliminar usuarios", "Admin"),
            new(AdminRolesView, "Ver roles", "Admin"),
            new(AdminRolesCreate, "Crear roles", "Admin"),
            new(AdminRolesUpdate, "Editar roles y asignar permisos", "Admin"),
            new(AdminRolesDelete, "Eliminar roles", "Admin"),
            new(AdminPermissionsView, "Ver permisos", "Admin"),
            new(AdminPermissionsManage, "Crear, editar o eliminar permisos", "Admin"),
            new(AdminBranchView, "Ver datos de sucursal", "Admin"),
            new(AdminBranchUpdate, "Editar datos de sucursal", "Admin"),

            new(RrhhEmployeesView, "Ver empleados", "RRHH"),
            new(RrhhEmployeesCreate, "Crear empleados", "RRHH"),
            new(RrhhEmployeesUpdate, "Editar empleados", "RRHH"),
            new(RrhhEmployeesDelete, "Eliminar empleados", "RRHH"),
            new(RrhhPositionsView, "Ver posiciones", "RRHH"),
            new(RrhhPositionsManage, "Crear, editar o eliminar posiciones", "RRHH"),
            new(RrhhSchedulingView, "Ver horarios y turnos", "RRHH"),
            new(RrhhSchedulingManage, "Administrar horarios y turnos", "RRHH"),
            new(RrhhPayrollView, "Ver nomina", "RRHH"),
            new(RrhhPayrollManage, "Administrar nomina", "RRHH"),

            new(PosOrdersView, "Ver pedidos", "POS"),
            new(PosOrdersCreate, "Crear pedidos", "POS"),
            new(PosOrdersUpdate, "Editar, confirmar o entregar pedidos", "POS"),
            new(PosOrdersCancel, "Cancelar pedidos", "POS"),
            new(PosKitchenView, "Ver items de cocina", "POS"),
            new(PosKitchenUpdate, "Cambiar estado de items de cocina", "POS"),
            new(PosStationsView, "Ver estaciones", "POS"),
            new(PosStationsManage, "Administrar estaciones", "POS"),
            new(PosTablesView, "Ver mesas QR", "POS"),
            new(PosTablesManage, "Administrar mesas QR", "POS"),
            new(PosTableRequestsView, "Ver solicitudes QR", "POS"),
            new(PosTableRequestsUpdate, "Tomar o cambiar estado de solicitudes QR", "POS"),

            new(MenuCategoriesView, "Ver categorias del menu", "Menu"),
            new(MenuCategoriesManage, "Administrar categorias del menu", "Menu"),
            new(MenuItemsView, "Ver items y recetas", "Menu"),
            new(MenuItemsManage, "Administrar items y recetas", "Menu"),
            new(MenuStockConsume, "Descontar stock por venta", "Menu"),

            new(InventoryConfigView, "Ver configuracion de inventario", "Inventory"),
            new(InventoryConfigManage, "Administrar configuracion de inventario", "Inventory"),
            new(InventoryArticlesView, "Ver articulos", "Inventory"),
            new(InventoryArticlesManage, "Administrar articulos", "Inventory"),
            new(InventoryStockView, "Ver stock y alertas", "Inventory"),
            new(InventoryMovementsView, "Ver movimientos de inventario", "Inventory"),
            new(InventoryMovementsCreate, "Registrar movimientos de inventario", "Inventory"),

            new(PurchasesSuppliersView, "Ver proveedores", "Purchases"),
            new(PurchasesSuppliersManage, "Administrar proveedores", "Purchases"),
            new(PurchasesOrdersView, "Ver compras", "Purchases"),
            new(PurchasesOrdersCreate, "Registrar compras", "Purchases"),
            new(PurchasesOrdersUpdate, "Editar compras", "Purchases"),
            new(PurchasesOrdersCancel, "Anular compras", "Purchases"),
            new(PurchasesOrdersDelete, "Eliminar compras", "Purchases"),

            new(BillingCustomersView, "Ver clientes", "Billing"),
            new(BillingCustomersManage, "Administrar clientes", "Billing"),
            new(BillingCashView, "Ver caja, sesiones, ventas y pagos", "Billing"),
            new(BillingCashOpen, "Abrir caja", "Billing"),
            new(BillingCashClose, "Cerrar caja", "Billing"),
            new(BillingCashCharge, "Cobrar ordenes", "Billing"),
            new(BillingPaymentMethodsView, "Ver medios de pago y bancos", "Billing"),
            new(BillingPaymentMethodsManage, "Administrar medios de pago y bancos", "Billing"),
            new(BillingTaxView, "Ver tarifas fiscales", "Billing"),
            new(BillingTaxManage, "Administrar tarifas fiscales", "Billing"),
            new(BillingSriView, "Ver configuracion y documentos SRI", "Billing"),
            new(BillingSriManage, "Configurar SRI, certificado, SMTP y plantilla", "Billing"),
            new(BillingSriGenerate, "Generar o reintentar documentos electronicos", "Billing"),
        ];
    }
}

public sealed record PermissionDefinition(string Code, string Description, string Category);
