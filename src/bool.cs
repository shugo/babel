/*
 * bool.cs: routines for BOOL
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU LGPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Core {
    public class BOOL {
        public static bool not(bool self)
        {
            return !self;
        }

        public static bool xnor(bool self, bool b)
        {
            return self == b;
        }

        public static bool is_eq(bool self, bool b)
        {
            return self == b;
        }

        public static bool is_eq(bool self, object o)
        {
            if (o is bool) {
                bool b = (bool) o;
                return is_eq(self, b);
            }
            else {
                return false;
            }
        }

        public static bool xor(bool self, bool b)
        {
            return self != b;
        }

        public static bool nand(bool self, bool b)
        {
            return !(self && b);
        }

        public static bool nor(bool self, bool b)
        {
            return !(self || b);
        }

        public static bool implies(bool self, bool b)
        {
            return !self || b;
        }

        public static bool and_rout(bool self, bool b)
        {
            return self && b;
        }

        public static bool or_rout(bool self, bool b)
        {
            return self || b;
        }

        public static bool and_not(bool self, bool b)
        {
            return self && !b;
        }

        public static bool or_not(bool self, bool b)
        {
            return self || !b;
        }

        public static bool nand_not(bool self, bool b)
        {
            return !self || b;
        }

        public static bool nor_not(bool self, bool b)
        {
            return !self && b;
        }

        public static int @int(bool self)
        {
            return self ? 1 : 0;
        }

        public static string str(bool self)
        {
            return self ? "true" : "false";
        }

        public static bool from_str(bool self, string s)
        {
            if (s == "true" || s == "t" ||
                s == "True" || s == "T" ||
                s == "TRUE") {
                return true;
            }
            else if (s == "false" || s == "f" ||
                     s == "False" || s == "F" ||
                     s == "FALSE") {
                return false;
            }
            else {
                throw new Exception("can't interpret bool value: " + s);
            }
        }
    }
}
