/*
 * str.cs: routines for STR
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU LGPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Sather.Base {
    public class STR {
        public static string create(string self)
        {
            return "";
        }

        public static string create(string self, string s)
        {
            return s;
        }

        public static string create(string self, char c)
        {
            return char.ToString(c);
        }

        public static char aget(string self, int i)
        {
            return self[i];
        }

        public static int size(string self)
        {
            if (self == null) {
                return 0;
            }
            else {
                return self.Length;
            }
        }

        public static string plus(string self, string s)
        {
            return self + s;
        }

        public static string plus(string self, char c)
        {
            return self + c;
        }

        public static string plus(string self, int i)
        {
            return self + i;
        }

        public static string str(string self)
        {
            return self;
        }

        public static bool is_empty(string self)
        {
            return self.Length == 0;
        }

        public static bool is_eq(string self, string s)
        {
            if (self == null) {
                return s == null || s.Length == 0;
            }
            else if (s == null) {
                return self.Length == 0;
            }
            else {
                return self == s;
            }
        }

        public static bool is_eq(string self, object o)
        {
            if (o is string) {
                return is_eq(self, (string) o);
            }
            else {
                return false;
            }
        }
    }
}
