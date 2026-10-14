using System.Data.Common;
using System.Text.Json;
using AutoMapper;
using Grimorio.Application.DTOs;
using Grimorio.Application.Features.Auth.Commands;
using Grimorio.Application.Features.Auth.Queries;
using Grimorio.Application.Features.Employees.Commands;
using Grimorio.Application.Features.Users.Commands;
using Grimorio.Application.Features.Users.Queries;
using Grimorio.Domain.Entities.Auth;
using Grimorio.Domain.Entities.Organization;
using Grimorio.Infrastructure.Features.Auth.Commands;
using Grimorio.Infrastructure.Features.Auth.Queries;
using Grimorio.Infrastructure.Features.Employees.Commands;
using Grimorio.Infrastructure.Features.Users.Commands;
using Grimorio.Infrastructure.Features.Users.Queries;
using Grimorio.Infrastructure.Mappings;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

var connection = Environment.GetEnvironmentVariable("EMPLOYEE_LINK_CHECKS_CONNECTION")
    ?? throw new Exception("Set EMPLOYEE_LINK_CHECKS_CONNECTION to the isolated checks database.");
var target = new NpgsqlConnectionStringBuilder(connection);
if (target.Host != "127.0.0.1" || target.Port != 15432 || target.Database != "checks" || target.Username != "checks")
    throw new Exception("These checks require the dedicated localhost:15432 checks database.");
var options = new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).Options;
GrimorioDbContext Db() => new(options);
var checks = 0;
void Check(bool value, string reason)
{
    if (!value) throw new Exception(reason);
    checks++;
}
async Task Reject(Func<Task> action, string message)
{
    try { await action(); }
    catch (InvalidOperationException ex) when (ex.Message.Contains(message)) { checks++; return; }
    throw new Exception($"Expected rejection: {message}.");
}
var mapper = new MapperConfiguration(config => config.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();
var hashing = new PasswordHashingService();
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["JwtSettings:SecretKey"] = "local-checks-only-secret-with-more-than-32-characters",
    ["JwtSettings:Issuer"] = "checks", ["JwtSettings:Audience"] = "checks",
}).Build();
var jwt = new JwtService(config);
var branch = new Branch { Name = "Checks", Code = "CHECKS" }; branch.BranchId = branch.Id;
var other = new Branch { Name = "Other", Code = "OTHER" }; other.BranchId = other.Id;
var position = new Position { Name = "Mesera", BranchId = branch.Id, BranchIdParent = branch.Id };
var otherPosition = new Position { Name = "Mesera", BranchId = other.Id, BranchIdParent = other.Id };
Employee Staff(string name, bool active = true, bool otherBranch = false) => new()
{
    BranchId = otherBranch ? other.Id : branch.Id, PositionId = otherBranch ? otherPosition.Id : position.Id,
    FirstName = name, LastName = "Checks", IdentificationNumber = Guid.NewGuid().ToString("N")[..10],
    Email = $"{name.ToLowerInvariant()}@example.test", IsActive = active, HireDate = DateTime.UtcNow,
};
var employee = Staff("Maria");
var second = Staff("Ana");
var inactive = Staff("Inactive", false);
var foreign = Staff("Foreign", otherBranch: true);
var atomic = Staff("Atomic");
var race = Staff("Race");
var deactivateRace = Staff("DeactivateRace");
string previousMigration;
var legacyId = Guid.NewGuid();
await using (var db = Db())
{
    var migrations = db.Database.GetMigrations().ToList(); previousMigration = migrations[^2];
    await db.GetService<IMigrator>().MigrateAsync(previousMigration);
    await db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO auth."Users" ("Id", "BranchId", "Email", "PasswordHash", "FirstName", "LastName", "CreatedBy")
        VALUES ({legacyId}, {branch.Id}, 'legacy@example.test', 'checks', 'Legacy', 'Checks', {Guid.Empty})
        """);
    await db.Database.MigrateAsync();
    Check(!db.Database.HasPendingModelChanges(), "Migration snapshot does not match the model.");
    var legacy = await db.Users.SingleAsync(u => u.Id == legacyId);
    Check(legacy.EmployeeId == null && legacy.Email == "legacy@example.test", "Migration must preserve existing accounts without auto-linking.");
    db.AddRange(branch, other, position, otherPosition, employee, second, inactive, foreign, atomic, race, deactivateRace);
    await db.SaveChangesAsync();
}
async Task<UserDto> Create(string email, Guid? employeeId = null, Guid? branchId = null)
{
    await using var db = Db();
    return await new CreateUserCommandHandler(db, hashing).Handle(new CreateUserCommand
    {
        BranchId = branchId ?? branch.Id,
        Dto = new CreateUserDto { Email = email, FirstName = "User", LastName = "Checks", Password = "checks-pass-123", EmployeeId = employeeId },
    }, default);
}
async Task<UserDto> Update(UserDto user, UpdateUserDto data)
{
    await using var db = Db();
    return await new UpdateUserCommandHandler(db).Handle(new UpdateUserCommand { BranchId = branch.Id, UserId = user.Id, Dto = data }, default);
}
async Task<EmployeeDto> SetEmployee(Employee staff, bool active)
{
    await using var db = Db();
    return await new UpdateEmployeeCommandHandler(db, mapper).Handle(new UpdateEmployeeCommand
    {
        EmployeeId = staff.Id, BranchId = staff.BranchId, FirstName = staff.FirstName, LastName = staff.LastName,
        Email = staff.Email, IdentificationNumber = staff.IdentificationNumber, PositionId = staff.PositionId,
        IsActive = active, WeeklyMinHours = 40, WeeklyMaxHours = 40,
    }, default);
}
var linked = await Create("linked@example.test", employee.Id);
Check(linked.EmployeeId == employee.Id && linked.EmployeeName == "Maria Checks", "Linked user DTO is incorrect.");
var technical = await Create("technical@example.test");
Check(technical.EmployeeId == null && technical.IsActive, "Independent accounts must still work.");
await Reject(() => Create("duplicate@example.test", employee.Id), "ya tiene un usuario");
await Reject(() => Create("foreign@example.test", foreign.Id), "otra sucursal");
await Reject(() => Create("inactive@example.test", inactive.Id), "inactivo");
await Reject(() => Create("missing@example.test", Guid.NewGuid()), "no existe");
await using (var db = Db())
{
    Check(!await db.Users.AnyAsync(u => u.Email == "duplicate@example.test" || u.Email == "foreign@example.test"), "Rejected creations left account records behind.");
    var choices = await new GetUserEmployeeOptionsHandler(db).Handle(new GetUserEmployeeOptionsQuery { BranchId = branch.Id }, default);
    Check(choices.All(e => e.Id != foreign.Id) && choices.Single(e => e.Id == employee.Id).LinkedUserId == linked.Id, "Employee options must be scoped and indicate existing links.");
    db.Users.Add(new User { BranchId = branch.Id, EmployeeId = employee.Id, Email = "bypass@example.test", PasswordHash = "checks", FirstName = "Bypass", LastName = "Checks" });
    try { await db.SaveChangesAsync(); throw new Exception("The unique employee index was bypassed."); }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "IX_Users_EmployeeId" }) { checks++; }
}
var legacyPayload = JsonSerializer.Deserialize<UpdateUserDto>("""{"firstName":"Updated","lastName":"Checks","isActive":true,"employeeLinkProvided":true}""", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
Check(!legacyPayload.EmployeeLinkProvided, "Clients must not spoof the link-presence flag.");
var updated = await Update(linked, legacyPayload);
Check(updated.EmployeeId == employee.Id, "Older clients must preserve an omitted link.");
technical = await Update(technical, new UpdateUserDto { FirstName = "Technical", LastName = "Checks", IsActive = true, EmployeeId = second.Id });
Check(technical.EmployeeId == second.Id, "Existing accounts cannot be linked.");
technical = await Update(technical, new UpdateUserDto { FirstName = "Technical", LastName = "Checks", IsActive = true, EmployeeId = null });
Check(technical.EmployeeId == null, "Explicit null must unlink an account.");
await using (var db = Db())
{
    var users = await new GetUsersQueryHandler(db).Handle(new GetUsersQuery { BranchId = other.Id }, default);
    Check(users.Count == 0, "User lists must stay inside their branch.");
    var role = new Role { BranchId = branch.Id, Name = AppConstants.Roles.Waiter, Description = "Checks" };
    db.Roles.Add(role); db.UserRoles.Add(new UserRole { BranchId = branch.Id, UserId = linked.Id, RoleId = role.Id });
    (await db.Users.SingleAsync(u => u.Id == linked.Id)).KdsPinHash = hashing.HashPassword("1234");
    await db.SaveChangesAsync();
}
AuthResponse response;
await using (var db = Db())
{
    response = await new LoginUserCommandHandler(db, jwt, hashing, config).Handle(new LoginUserCommand { Email = linked.Email, Password = "checks-pass-123" }, default);
    Check(await UserAccessPolicy.HasActiveAccountAsync(db, linked.Id, branch.Id, default), "Active linked accounts must pass the live access check.");
}
var deactivated = await SetEmployee(employee, false);
Check(deactivated.UserId == linked.Id && deactivated.UserIsActive == false, "Employee deactivation must disable its linked account atomically.");
await using (var db = Db())
{
    Check(!await UserAccessPolicy.HasActiveAccountAsync(db, linked.Id, branch.Id, default), "Existing JWT requests must stop after deactivation.");
    try { await new LoginUserCommandHandler(db, jwt, hashing, config).Handle(new LoginUserCommand { Email = linked.Email, Password = "checks-pass-123" }, default); throw new Exception("Disabled employee logged in."); }
    catch (UnauthorizedAccessException) { checks++; }
    try { await new KdsLoginCommandHandler(db, jwt, hashing, config).Handle(new KdsLoginCommand { BranchId = branch.Id, UserId = linked.Id, Pin = "1234", RequireWaitstaffRole = true }, default); throw new Exception("Disabled employee used PIN login."); }
    catch (UnauthorizedAccessException) { checks++; }
    Check(!(await new GetWaitstaffUsersQueryHandler(db).Handle(new GetWaitstaffUsersQuery { BranchId = branch.Id }, default)).Any(u => u.Id == linked.Id), "Inactive linked staff must disappear from mobile login choices.");
}
await Reject(() => Update(linked, new UpdateUserDto { FirstName = "User", LastName = "Checks", IsActive = true }), "inactivo");
await SetEmployee(employee, true);
await using (var db = Db()) Check(!await UserAccessPolicy.HasActiveAccountAsync(db, linked.Id, branch.Id, default), "Rehiring must not silently re-enable the account.");
await Update(linked, new UpdateUserDto { FirstName = "User", LastName = "Checks", IsActive = true });
await using (var db = Db()) Check(await UserAccessPolicy.HasActiveAccountAsync(db, linked.Id, branch.Id, default), "Manual reactivation failed.");
var atomicUser = await Create("atomic@example.test", atomic.Id);
await using (var db = Db()) await db.Database.ExecuteSqlRawAsync("ALTER TABLE auth.\"Users\" ADD CONSTRAINT checks_atomic_user CHECK (\"Email\" <> 'atomic@example.test' OR \"IsActive\")");
try { await SetEmployee(atomic, false); throw new Exception("Expected a forced database failure."); }
catch (DbUpdateException) { checks++; }
await using (var db = Db())
{
    Check((await db.Employees.SingleAsync(e => e.Id == atomic.Id)).IsActive && (await db.Users.SingleAsync(u => u.Id == atomicUser.Id)).IsActive, "A failure left employee/account states inconsistent.");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE auth.\"Users\" DROP CONSTRAINT checks_atomic_user");
}
async Task<bool> RaceCreate(string email)
{
    try { await Create(email, race.Id); return true; }
    catch (InvalidOperationException ex) when (ex.Message.Contains("ya tiene un usuario")) { return false; }
}
var simultaneous = await Task.WhenAll(RaceCreate("race-one@example.test"), RaceCreate("race-two@example.test"));
Check(simultaneous.Count(ok => ok) == 1, "Concurrent account creation produced duplicate links.");
await using (var db = Db()) Check(await db.Users.CountAsync(u => u.EmployeeId == race.Id) == 1, "Concurrent links were persisted twice.");
var barrier = new EmployeeLockBarrier();
await using (var db = new GrimorioDbContext(new DbContextOptionsBuilder<GrimorioDbContext>().UseNpgsql(connection).AddInterceptors(barrier).Options))
{
    var creating = new CreateUserCommandHandler(db, hashing).Handle(new CreateUserCommand
    {
        BranchId = branch.Id, Dto = new CreateUserDto { Email = "deactivate-race@example.test", FirstName = "Race", LastName = "Checks", Password = "checks-pass-123", EmployeeId = deactivateRace.Id },
    }, default);
    await barrier.Locked.Task.WaitAsync(TimeSpan.FromSeconds(30));
    var disabling = SetEmployee(deactivateRace, false);
    barrier.Release.TrySetResult();
    await Task.WhenAll(creating, disabling);
    await using var verify = Db();
    Check(!(await verify.Users.SingleAsync(u => u.Id == creating.Result.Id)).IsActive, "Concurrent employee deactivation missed a newly created account.");
}
await using (var db = Db())
{
    await new DeleteEmployeeCommandHandler(db).Handle(new DeleteEmployeeCommand { BranchId = branch.Id, EmployeeId = atomic.Id }, default);
    var account = await db.Users.SingleAsync(u => u.Id == atomicUser.Id);
    Check(!account.IsActive && account.EmployeeId == atomic.Id, "Employee removal must disable access and retain the audit link.");
}
await using (var db = Db())
{
    await new DeleteUserCommandHandler(db).Handle(new DeleteUserCommand { UserId = linked.Id }, default);
}
var replacement = await Create("replacement@example.test", employee.Id);
Check(replacement.EmployeeId == employee.Id, "A removed account must not permanently block an employee link.");
await using (var db = Db())
{
    Check((await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == linked.Id)).EmployeeId == employee.Id, "Removing a user destroyed its historical employee link.");
    var count = await db.Database.SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM auth.\"Users\"").SingleAsync();
    await db.GetService<IMigrator>().MigrateAsync(previousMigration);
    Check(await db.Database.SqlQuery<int>($"SELECT COUNT(*)::integer AS \"Value\" FROM auth.\"Users\"").SingleAsync() == count, "Migration rollback removed accounts.");
    await db.Database.MigrateAsync();
    Check(await db.Users.IgnoreQueryFilters().CountAsync() == count, "Migration upgrade removed accounts.");
}
Console.WriteLine($"User/employee integration checks passed: {checks}. Isolated PostgreSQL only.");

public class EmployeeLockBarrier : DbCommandInterceptor
{
    public TaskCompletionSource Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _used;
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("FOR UPDATE") && Interlocked.Exchange(ref _used, 1) == 0)
        {
            Locked.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
        return result;
    }
}
