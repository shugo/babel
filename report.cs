/*
 * report.cs: error report
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;

namespace Babel.Sather.Compiler
{
    public class Report
    {
        int errors;
        int warnings;

        public Report()
        {
            errors = 0;
            warnings = 0;
        }

        public int Errors
        {
            get { return errors; }
        }

        public int Warnings
        {
            get { return warnings; }
        }

        public void Error(Location location, string msg, params object[] args)
        {
            errors++;
            WriteLocation(location);
            Console.Error.WriteLine(msg, args);
        }

        public void Error(Location location, string msg)
        {
            errors++;
            WriteLocation(location);
            Console.Error.WriteLine(msg);
        }

        public void Warning(Location location, string msg, params object[] args)
        {
            warnings++;
            WriteLocation(location);
            Console.Error.WriteLine(msg, args);
        }

        public void Warning(Location location, string msg)
        {
            warnings++;
            WriteLocation(location);
            Console.Error.WriteLine(msg);
        }

        void WriteLocation(Location location)
        {
            Console.Error.Write("{0}:{1}:{2}: ",
                                location.FileName,
                                location.Line, location.Column);
        }
    }
}
