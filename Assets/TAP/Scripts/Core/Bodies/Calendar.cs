using System;

namespace TAP.Core
{
    /// <summary>
    /// The clock and calendar players read. A day is the home planet's solar day (noon to noon, not its sidereal turn)
    /// and a year is its orbit around the star. A year is not a whole number of days, so calendar years have a day more
    /// or less (439 or 440 days for Tellus) and each new year starts within half a day of the orbital anniversary; the
    /// time of day always follows the sun.
    /// </summary>
    public sealed class Calendar
    {
        /// <summary>Seconds in a day: the home planet's solar day.</summary>
        public readonly double DayLength;
        /// <summary>Seconds in a year: the home planet's orbital period around the star.</summary>
        public readonly double YearLength;

        public Calendar(double dayLength, double yearLength)
        {
            DayLength = dayLength > 0 && !double.IsInfinity(dayLength) ? dayLength : 21600;
            YearLength = yearLength > DayLength && !double.IsInfinity(yearLength) ? yearLength : 426 * DayLength;
        }

        /// <summary>The calendar of the system this session plays in.</summary>
        public static Calendar Current => CelestialSystem.Default.Calendar;

        /// <summary>
        /// The calendar of a system's home planet: its solar day, and the orbital period of the planet (or, for a moon,
        /// of the planet it circles) around the star. Without a star the year is 426 days.
        /// </summary>
        public static Calendar Of(CelestialSystem sys)
        {
            var home = sys.HomeBody;
            var b = home;
            while (b.Parent != null && !b.Parent.IsStar) b = b.Parent;
            double year = b.Parent != null && b.Orbit != null && b.Orbit.IsElliptic ? b.Orbit.Period : 0;
            return new Calendar(home.SolarDay, year);
        }

        /// <summary>Days in a year on average (the orbital period in days).</summary>
        public double DaysPerYear => YearLength / DayLength;

        /// <summary>The day (counted from 0 at UT 0) on which year <paramref name="year"/> (0 = the first) begins.</summary>
        public long YearStartDay(long year) => (long)Math.Floor(year * DaysPerYear + 0.5);

        /// <summary>Splits a universal time into the year and the day of the year (both from 0) and the seconds into the day.</summary>
        public void Split(double ut, out long year, out long dayOfYear, out double secondOfDay)
        {
            if (ut < 0 || double.IsNaN(ut)) ut = 0;
            long day = (long)Math.Floor(ut / DayLength);
            secondOfDay = Math.Max(0, ut - day * DayLength);
            year = (long)Math.Ceiling((day + 0.5) / DaysPerYear) - 1;
            if (year < 0) year = 0;
            // Guard the rounding at a year boundary: the day must fall inside the year.
            while (year > 0 && YearStartDay(year) > day) year--;
            while (YearStartDay(year + 1) <= day) year++;
            dayOfYear = day - YearStartDay(year);
        }

        /// <summary>A date and time as "Y1 D001 00:00:00" (years and days count from 1).</summary>
        public string Format(double ut)
        {
            Split(ut, out long y, out long d, out double sec);
            long s = (long)sec;
            return $"Y{y + 1} D{d + 1:000} {s / 3600:00}:{s / 60 % 60:00}:{s % 60:00}";
        }

        /// <summary>A duration in days of this calendar, hours, minutes and seconds ("2d 03h 15m", "4m 05s").</summary>
        public string FormatDuration(double seconds, bool showSign = false)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--";
            string sign = seconds < 0 ? "-" : (showSign ? "+" : "");
            double abs = Math.Abs(seconds);
            long d = (long)Math.Floor(abs / DayLength);
            long total = (long)Math.Floor(abs - d * DayLength);
            long h = total / 3600; total %= 3600;
            long m = total / 60; long sec = total % 60;
            if (d > 0) return $"{sign}{d}d {h:00}h {m:00}m";
            if (h > 0) return $"{sign}{h}h {m:00}m {sec:00}s";
            if (m > 0) return $"{sign}{m}m {sec:00}s";
            return $"{sign}{abs:0.0}s";
        }
    }
}
