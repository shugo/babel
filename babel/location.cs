/*
 * location.cs: location in source files
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

namespace Babel.Sather.Compiler {
    public struct Location
    {
        string fileName;
        int line;
        int column;
        static Location nullValue;

        static Location()
        {
            nullValue = new Location(null, 0, 0);
        }

        public static Location Null
        {
            get { return nullValue; }
        }

        public Location(string fileName, int line, int column)
        {
            this.fileName = fileName;
            this.line = line;
            this.column = column;
        }

        public string FileName
        {
            get { return fileName; }
        }

        public int Line
        {
            get { return line; }
        }

        public int Column
        {
            get { return column; }
        }
    }
}
