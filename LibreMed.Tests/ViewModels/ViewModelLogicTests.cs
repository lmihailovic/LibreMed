using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibreMed.Data;
using LibreMed.Services;
using LibreMed.ViewModels;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LibreMed.Tests.ViewModels;

public sealed class ViewModelLogicTests
{
    [Fact]
    public void PatientWindow_OpenVisitsCommand_CallsWindowServiceWithPatientId()
    {
        using var scope = TestDbScope.CreateAndSeed();
        var patientId = scope.GetAnyPatientId();

        var windows = new Mock<IWindowService>(MockBehavior.Strict);
        windows.Setup(x => x.ShowVisitsWindow(patientId));

        // VM koristi DbBootstrapper koji sada pokazuje na privremenu test bazu
        var vm = new PatientWindowViewModel(patientId, windows.Object);

        vm.OpenVisitsCommand.Execute(null);

        windows.Verify(x => x.ShowVisitsWindow(patientId), Times.Once);
        windows.VerifyNoOtherCalls();
    }

    [Fact]
    public void VisitsWindow_OpenDiagnosisCommand_CallsWindowServiceWithVisitId()
    {
        using var scope = TestDbScope.CreateAndSeed();
        var patientId = scope.GetAnyPatientId();
        var visitId = scope.GetAnyVisitIdForPatient(patientId);

        var windows = new Mock<IWindowService>(MockBehavior.Strict);
        windows.Setup(x => x.ShowDiagnosisWindow(visitId));

        var vm = new VisitsWindowViewModel(patientId, windows.Object);

        vm.OpenDiagnosisCommand.Execute(visitId);

        windows.Verify(x => x.ShowDiagnosisWindow(visitId), Times.Once);
        windows.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatientWindow_DeletePatientAsync_WhenPatientMissing_ClosesActiveWindow()
    {
        using var scope = TestDbScope.CreateAndSeed();
        var patientId = scope.GetAnyPatientId();

        var windows = new Mock<IWindowService>(MockBehavior.Strict);
        windows.Setup(x => x.CloseActiveWindow());

        var vm = new PatientWindowViewModel(patientId, windows.Object);

        // delete patient directly to simulate "already deleted elsewhere"
        using (var db = scope.CreateDbContextForTests())
        {
            var p = await db.Patients.FirstAsync(x => x.Id == patientId);
            db.Patients.Remove(p);
            await db.SaveChangesAsync();
        }

        await vm.DeletePatientCommand.ExecuteAsync(null);

        windows.Verify(x => x.CloseActiveWindow(), Times.Once);
        windows.VerifyNoOtherCalls();
    }

    private sealed class TestDbScope : IDisposable
    {
        private readonly string _tempFolder;
        private readonly string? _oldXdgDataHome;

        private TestDbScope(string tempFolder, string? oldXdgDataHome)
        {
            _tempFolder = tempFolder;
            _oldXdgDataHome = oldXdgDataHome;
        }

        public static TestDbScope CreateAndSeed()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrWhiteSpace(home))
                throw new InvalidOperationException("HOME is not set; cannot place test DB under $HOME/.tmp/test/");

            var oldXdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

            var tempBaseDir = Path.Combine(home, ".tmp", "test", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempBaseDir);

            var xdgDataHome = Path.Combine(tempBaseDir, "xdg-data-home");
            Directory.CreateDirectory(xdgDataHome);

            Environment.SetEnvironmentVariable("XDG_DATA_HOME", xdgDataHome);

            // Make absolutely sure the DB directory exists before SQLite tries to open the file
            var dbPath = DbBootstrapper.GetDatabasePath();
            var dbDir = Path.GetDirectoryName(dbPath);
            if (string.IsNullOrWhiteSpace(dbDir))
                throw new InvalidOperationException($"Could not determine DB directory from path '{dbPath}'");

            Directory.CreateDirectory(dbDir);

            Console.WriteLine($"DB path: {dbPath}");
            Console.WriteLine($"DB dir : {dbDir}");

            DbBootstrapper.EnsureCreated();

            return new TestDbScope(tempBaseDir, oldXdgDataHome);
        }

        public AppDbContext CreateDbContextForTests()
        {
            var dbPath = Path.Combine(_tempFolder, "LibreMed", "libremed.db");
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            return new AppDbContext(options);
        }

        public int GetAnyPatientId()
        {
            using var db = CreateDbContextForTests();
            return db.Patients.AsNoTracking().Select(p => p.Id).First();
        }

        public int GetAnyVisitIdForPatient(int patientId)
        {
            using var db = CreateDbContextForTests();
            var visitId = db.Visits.AsNoTracking().Where(v => v.PatientId == patientId).Select(v => v.Id).FirstOrDefault();
            return visitId != 0 ? visitId : db.Visits.AsNoTracking().Select(v => v.Id).First();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", _oldXdgDataHome);
            try { Directory.Delete(_tempFolder, recursive: true); } catch { }
        }
    }
}
