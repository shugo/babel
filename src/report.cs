/*
 * report.cs: error report
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;

namespace Babell.Compiler {
    public class Report {
        protected int errors;
        protected int warnings;

        public Report()
        {
            errors = 0;
            warnings = 0;
        }

        public virtual int Errors {
            get { return errors; }
        }

        public virtual int Warnings {
            get { return warnings; }
        }

        public virtual void Error(Location location,
                                  string msg, params object[] args)
        {
            errors++;
            WriteLocation(location);
            Console.Error.WriteLine(msg, args);
        }

        public virtual void Error(Location location, string msg)
        {
            errors++;
            WriteLocation(location);
            Console.Error.WriteLine(msg);
        }

        public virtual void Warning(Location location,
                                    string msg, params object[] args)
        {
            warnings++;
            WriteLocation(location);
            Console.Error.WriteLine(msg, args);
        }

        public virtual void Warning(Location location, string msg)
        {
            warnings++;
            WriteLocation(location);
            Console.Error.WriteLine(msg);
        }

        protected virtual void WriteLocation(Location location)
        {
            Console.Error.Write("{0}:{1}:{2}: ",
                                location.FileName,
                                location.Line, location.Column);
        }
    }
}
