using NUnit.Framework;
using TAP.Core;

namespace TAP.Tests
{
    /// <summary>The home planet's clock and calendar (FND-01): 6-hour solar days and a year that follows Tellus's orbit.</summary>
    public class CalendarTests
    {
        private static CelestialSystem Home() => CelestialSystem.LoadFromResources();

        [Test]
        public void TellusDay_IsSixHoursFromNoonToNoon()
        {
            var sys = Home();
            var tellus = sys.Get("tellus");
            Assert.AreEqual(21600, tellus.SolarDay, 0.01, "solar day");
            Assert.Less(tellus.RotationPeriod, tellus.SolarDay, "the sidereal turn is shorter: Tellus moves along its orbit meanwhile");
            // After one solar day the star stands in the same place in Tellus's sky; after one sidereal turn it does not.
            foreach (double ut in new[] { 0.0, 1e6, 4.75e6 })
            {
                Vector3d noon = tellus.InertialToBodyFixed(sys.SunDirectionFrom(tellus, Vector3d.zero, ut), ut);
                double solar = ut + tellus.SolarDay, sidereal = ut + tellus.RotationPeriod;
                Vector3d next = tellus.InertialToBodyFixed(sys.SunDirectionFrom(tellus, Vector3d.zero, solar), solar);
                Vector3d turn = tellus.InertialToBodyFixed(sys.SunDirectionFrom(tellus, Vector3d.zero, sidereal), sidereal);
                Assert.Less((next - noon).magnitude, 1e-6, $"sun in the sky one day after UT {ut}");
                Assert.Greater((turn - noon).magnitude, 1e-2, $"sun in the sky one sidereal turn after UT {ut}");
            }
        }

        [Test]
        public void Clock_StartsAtYear1Day1()
        {
            var cal = Home().Calendar;
            Assert.AreEqual("Y1 D001 00:00:00", cal.Format(0));
            Assert.AreEqual("Y1 D001 01:01:01", cal.Format(3661));
            Assert.AreEqual("Y1 D001 05:59:59", cal.Format(21599.5));
            Assert.AreEqual("Y1 D002 00:00:00", cal.Format(21600));
            Assert.AreEqual("Y1 D001 00:00:00", cal.Format(-5), "negative times clamp to the start");
        }

        [Test]
        public void CalendarYears_FollowTheOrbit()
        {
            var sys = Home();
            var cal = sys.Calendar;
            double year = sys.Get("tellus").Orbit.Period;
            Assert.AreEqual(year, cal.YearLength, 1e-6, "a year is Tellus's orbital period");
            Assert.AreEqual(439.87, cal.DaysPerYear, 0.01);
            for (long n = 1; n <= 60; n++)
            {
                long start = cal.YearStartDay(n);
                long length = start - cal.YearStartDay(n - 1);
                Assert.That(length == 439 || length == 440, $"year {n} has {length} days");
                Assert.AreEqual(n * year, start * cal.DayLength, 0.5 * cal.DayLength, $"year {n + 1} begins within half a day of the orbital anniversary");
                double newYear = start * cal.DayLength;
                Assert.AreEqual($"Y{n + 1} D001 00:00:00", cal.Format(newYear + 0.5));
                // The day before is the last of the previous year, and the time of day runs on without a jump.
                Assert.AreEqual($"Y{n} D{length:000} 05:59:59", cal.Format(newYear - 0.5));
            }
        }

        [Test]
        public void Durations_CountTellusDays()
        {
            var cal = Home().Calendar;
            Assert.AreEqual("1d 01h 00m", cal.FormatDuration(21600 + 3600));
            Assert.AreEqual("5h 59m 59s", cal.FormatDuration(21599));
            Assert.AreEqual("-2m 05s", cal.FormatDuration(-125));
            Assert.AreEqual("+12.5s", cal.FormatDuration(12.5, true));
            Assert.AreEqual("439d 05h 14m", cal.FormatDuration(cal.YearLength));
        }

        [Test]
        public void DebugSystem_HasItsOwnCalendar()
        {
            var sys = Galaxy.LoadFromResources().LoadSystem("debug");
            var world = sys.HomeBody;
            Assert.AreEqual(world.SolarDay, sys.Calendar.DayLength, 1e-9);
            Assert.AreEqual(world.Orbit.Period, sys.Calendar.YearLength, 1e-6);
        }
    }
}
