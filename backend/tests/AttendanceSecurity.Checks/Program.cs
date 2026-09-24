using Grimorio.API.Services;

var tickets = new AttendanceRecognitionTickets();
var kiosk = Guid.NewGuid();
var employee = Guid.NewGuid();
void Check(bool value) { if (!value) throw new Exception("Recognition ticket check failed"); }
var first = tickets.Issue(kiosk, employee);
Check(!tickets.Consume(Guid.NewGuid(), employee, first));
Check(!tickets.Consume(kiosk, Guid.NewGuid(), first));
Check(!tickets.Consume(kiosk, employee, null));
Check(!tickets.Consume(kiosk, employee, "invalid"));
Check(tickets.Consume(kiosk, employee, first));
Check(!tickets.Consume(kiosk, employee, first));
var old = tickets.Issue(kiosk, employee);
var current = tickets.Issue(kiosk, employee);
Check(!tickets.Consume(kiosk, employee, old));
var accepted = 0;
Parallel.For(0, 20, _ => { if (tickets.Consume(kiosk, employee, current)) Interlocked.Increment(ref accepted); });
Check(accepted == 1);
Console.WriteLine("Attendance recognition checks passed: binding, missing/invalid tokens, replacement, replay, concurrency.");
