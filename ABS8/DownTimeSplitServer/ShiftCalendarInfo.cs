using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DownTimeSplitService
{
    public class ShiftCalendarInfo
    {
        private static ShiftCalendarInfo instance;
        private DateTime dtInitDate;
        private ShiftCalendarInfo()
        {
            dtInitDate = new DateTime(2014, 12, 26, 7, 0, 0);
        }

        public static ShiftCalendarInfo GetInstance()
        {
            if (instance == null)
            {
                instance = new ShiftCalendarInfo();
            }
            return instance;
        }
        public string GetShiftInfo(DateTime dt)
        {
            int hour = dt.Hour;
            string info = "";
            if (hour < 7)
            {
                info = GetNightShift(dt.AddDays(-1));
            }

            if (hour >= 7 && hour < 19)
            {
                info = GetDayShift(dt);
            }
            else if (hour >= 19)
            {
                info = GetNightShift(dt);
            }

            return info;
        }

        public string GetDayShift(DateTime dt)
        {
            int days = Convert.ToInt32(dt.Subtract(dtInitDate).TotalDays);
            string shiftA = GetShiftAByDay(days);
            string shiftB = GetShiftBByDay(days);
            string shiftC = GetShiftCByDay(days);
            if (shiftA == "D")
            {
                return "ShiftA";
            }
            if (shiftB == "D")
            {
                return "ShiftB";
            }

            if (shiftC == "D")
            {
                return "ShiftC";
            }
            return "";



        }
        public string GetNightShift(DateTime dt)
        {
            int days = Convert.ToInt32(dt.Subtract(dtInitDate).TotalDays);
            string shiftA = GetShiftAByDay(days);
            string shiftB = GetShiftBByDay(days);
            string shiftC = GetShiftCByDay(days);
            if (shiftA == "N")
            {
                return "ShiftA";
            }
            if (shiftB == "N")
            {
                return "ShiftB";
            }

            if (shiftC == "N")
            {
                return "ShiftC";
            }
            return "";
        }



        public string GetShiftAByDay(int days)
        {
            int day = days % 12;
            string ret = "";
            switch (day)
            {
                case 0:
                    ret = "R";
                    break;
                case 1:
                    ret = "D";
                    break;

                case 2:
                    ret = "D";
                    break;
                case 3:
                    ret = "D";
                    break;
                case 4:
                    ret = "D";
                    break;
                case 5:
                    ret = "R";
                    break;
                case 6:
                    ret = "R";
                    break;
                case 7:
                    ret = "N";
                    break;
                case 8:
                    ret = "N";
                    break;
                case 9:
                    ret = "N";
                    break;
                case 10:
                    ret = "N";
                    break;
                case 11:
                    ret = "R";
                    break;

            }
            return ret;

        }

        public string GetShiftBByDay(int days)
        {
            int day = days % 12;
            string ret = "";
            switch (day)
            {
                case 0:
                    ret = "N";
                    break;
                case 1:
                    ret = "N";
                    break;
                case 2:
                    ret = "N";
                    break;
                case 3:
                    ret = "R";
                    break;
                case 4:
                    ret = "R";
                    break;
                case 5:
                    ret = "D";
                    break;
                case 6:
                    ret = "D";
                    break;
                case 7:
                    ret = "D";
                    break;
                case 8:
                    ret = "D";
                    break;
                case 9:
                    ret = "R";
                    break;
                case 10:
                    ret = "R";
                    break;
                case 11:
                    ret = "N";
                    break;

            }
            return ret;

        }

        public string GetShiftCByDay(int days)
        {
            int day = days % 12;
            string ret = "";
            switch (day)
            {
                case 0:
                    ret = "D";
                    break;
                case 1:
                    ret = "R";
                    break;

                case 2:
                    ret = "R";
                    break;
                case 3:
                    ret = "N";
                    break;
                case 4:
                    ret = "N";
                    break;
                case 5:
                    ret = "N";
                    break;
                case 6:
                    ret = "N";
                    break;
                case 7:
                    ret = "R";
                    break;
                case 8:
                    ret = "R";
                    break;
                case 9:
                    ret = "D";
                    break;
                case 10:
                    ret = "D";
                    break;
                case 11:
                    ret = "D";
                    break;

            }
            return ret;

        }
    }
}
