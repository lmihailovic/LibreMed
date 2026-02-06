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
        // Arrange
        using var _ = TestDbScope.CreateAndSeed();

        var patientId = TestDbScope.GetAnyPatientId();

        var windows = new Mock<IWindowService>(MockBehavior.Strict);
        windows.Setup(x => x.ShowVisitsWindow(patientId));

        // Constructor triggers RefreshAsync() (DB), which is why we seed above.
        var vm = new PatientWindowViewModel(patientId, windows.Object);

        // Act
        vm.OpenVisitsCommand.Execute(null);

        // Assert
        windows.Verify(x => x.ShowVisitsWindow(patientId), Times.Once);
        windows.VerifyNoOtherCalls();
    }

    [Fact]
    public void VisitsWindow_OpenDiagnosisCommand_CallsWindowServiceWithVisitId()
    {
        // Arrange
        using var _ = TestDbScope.CreateAndSeed();

        var patientId = TestDbScope.GetAnyPatientId();
        var visitId = TestDbScope.GetAnyVisitIdForPatient(patientId);

        var windows = new Mock<IWindowService>(MockBehavior.Strict);
        windows.Setup(x => x.ShowDiagnosisWindow(visitId));

        // Constructor triggers RefreshAsync() (DB).
        var vm = new VisitsWindowViewModel(patientId, windows.Object);

        // Act
        vm.OpenDiagnosisCommand.Execute(visitId);

        // Assert
        windows.Verify(x => x.ShowDiagnosisWindow(visitId), Times.Once);
        windows.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PatientWindow_DeletePatientAsync_WhenPatientMissing_ClosesActiveWindow()
    {
        // Arrange
        using var _ = TestDbScope.CreateAndSeed();

        var nonExistentPatientId = int.MaxValue;

        var windows = new Mock<IWindowService>(MockBehavior.Strict);
        windows.Setup(x => x.CloseActiveWindow());

        var vm = new PatientWindowViewModel(nonExistentPatientId, windows.Object);

        // Act
        await vm.DeletePatientCommand.ExecuteAsync(null);

        // Assert
        windows.Verify(x => x.CloseActiveWindow(), Times.Once);
        windows.VerifyNoOtherCalls();
    }

    private sealed class TestDbScope : IDisposable
    {
        private readonly string _tempHome;
        private readonly string? _oldHome;

        private TestDbScope(string tempHome, string? oldHome)
        {
            _tempHome = tempHome;
            _oldHome = oldHome;
        }

        public static TestDbScope CreateAndSeed()
        {
            var oldHome = Environment.GetEnvironmentVariable("HOME");
            var tempHome = Path.Combine(Path.GetTempPath(), "LibreMed.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempHome);

            // DbBootstrapper.GetDatabasePath() uses LocalApplicationData, which on Linux derives from HOME.
            Environment.SetEnvironmentVariable("HOME", tempHome);

            DbBootstrapper.EnsureCreated();

            return new TestDbScope(tempHome, oldHome);
        }

        public static int GetAnyPatientId()
        {
            using var db = CreateDbContext();
            return db.Patients.AsNoTracking().Select(p => p.Id).First();
        }

        public static int GetAnyVisitIdForPatient(int patientId)
        {
            using var db = CreateDbContext();

            // If seed data doesn't include a visit for the chosen patient, pick any visit.
            var forPatient = db.Visits.AsNoTracking().Where(v => v.PatientId == patientId).Select(v => v.Id).FirstOrDefault();
            if (forPatient != 0)
                return forPatient;

            return db.Visits.AsNoTracking().Select(v => v.Id).First();
        }

        private static AppDbContext CreateDbContext()
        {
            var dbPath = DbBootstrapper.GetDatabasePath();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            return new AppDbContext(options);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HOME", _oldHome);

            // Best-effort cleanup
            try
            {
                Directory.Delete(_tempHome, recursive: true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
