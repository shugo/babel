/*
 * int.cs: routines for INT
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU LGPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Sather.Base
{
    public class INT
    {
        public static int plus(int self, int i)
        {
            return self + i;
        }

        public static int minus(int self, int i)
        {
            return self - i;
        }

        public static int negate(int self)
        {
            return -self;
        }

        public static int times(int self, int i)
        {
            return self * i;
        }

        public static int div(int self, int i)
        {
            return self / i;
        }

        public static int mod(int self, int i)
        {
            return self % i;
        }

        public static bool is_eq(int self, int i)
        {
            return self == i;
        }

        public static bool is_eq(int self, object o)
        {
            if (o is int) {
                int i = (int) o;
                return is_eq(self, i);
            }
            else {
                return false;
            }
        }

        public static bool is_lt(int self, int i)
        {
            return self < i;
        }

        public static int max(int self, int i)
        {
            return Math.Max(self, i);
        }

        public static int min(int self, int i)
        {
            return Math.Min(self, i);
        }

        public static int at_least(int self, int i)
        {
            return max(self, i);
        }

        public static int at_most(int self, int i)
        {
            return min(self, i);
        }

        public static int within(int self, int x, int y)
        {
            return min(max(self, x), y);
        }

        public static int create(int self, int x)
        {
            return x;
        }

        public static int create(int self, string s)
        {
            return int.Parse(s);
        }

        public static int @int(int self)
        {
            return self;
        }
        
        public static int from_int(int self, int i)
        {
            return i;
        }
        
        public static bool @bool(int self)
        {
            return self != 0;
        }
        
        public static char @char(int self)
        {
            return (char) self;
        }
        
        public static int abs(int self)
        {
            return Math.Abs(self);
        }
        
        public static int square(int self)
        {
            return self * self;
        }
        
        public static int cube(int self)
        {
            return self * self * self;
        }
        
        public static int pow(int self, int i)
        {
            int r;

            switch (i) {
            case 0:
                return 1;
            case 1:
                return self;
            case 2:
                return self * self;
            case 3:
                return self * self * self;
            case 4:
                r = self * self;
                return r * r;
            case 5:
                r = self * self;
                return self * r * r;
            case 6:
                r = self * self;
                return r * r * r;
            default:
                int x = self;
                r = 1;
                while (true) {
                    if (i % 2 != 0)
                        r = r * x;
                    i = i >> 1;
                    if (i == 0)
                        break;
                    x = x * x;
                }
                return r;
            }
        }

        public static int sqrt(int self)
        {
            double d = (double) self;
            if (((int) Math.Floor(d)) == self) {
                return (int) Math.Floor(Math.Sqrt(d));
            }
            else {
                int q = 1;
                int r = self;
                while (q <= r) {
                    q = 4 * q;
                }
                while (q != 1) {
                    q = q / 4;
                    int h = r + q;
                    r = r / 2;
                    if (h <= r) {
                        r = r - h;
                        r = r + q;
                    }
                }
                return r;
            }
        }

        public static bool is_even(int self)
        {
            return self % 2 == 0;
        }

        public static bool is_odd(int self)
        {
            return self % 2 != 0;
        }

        public static bool is_pos(int self)
        {
            return self > 0;
        }

        public static bool is_neg(int self)
        {
            return self < 0;
        }

        public static bool is_zero(int self)
        {
            return self == 0;
        }

        public static bool is_non_zero(int self)
        {
            return self != 0;
        }

        public static bool is_non_neg(int self)
        {
            return self >= 0;
        }

        public static bool is_non_pos(int self)
        {
            return self <= 0;
        }

        [SatherNameAttribute("up!")]
        [IterTypeAttribute(typeof(__itertype_up))]
        public static int __iter_up_int(int self)
        {
            return 0;
        }

        public class __itertype_up
        {
            protected int current;

            public __itertype_up(int self)
            {
                this.current = self - 1;
            }

            public virtual bool MoveNext()
            {
                current++;
                return true;
            }

            public int GetCurrent()
            {
                return current;
            }
        }

        [SatherNameAttribute("upto!")]
        [IterTypeAttribute(typeof(__itertype_upto))]
        public static int __iter_upto(int self,
                                      [ArgumentModeAttribute(ArgumentMode.Once)]
                                      int i)
        {
            return 0;
        }

        public class __itertype_upto
        {
            protected int limit;
            protected int current;

            public __itertype_upto(int self, int i)
            {
                this.limit = i;
                this.current = self - 1;
            }

            public virtual bool MoveNext()
            {
                current++;
                return current <= limit;
            }

            public int GetCurrent()
            {
                return current;
            }
        }

        [SatherNameAttribute("times!")]
        [IterTypeAttribute(typeof(__itertype_times))]
        public static void __iter_times(int self)
        {
        }

        [SatherNameAttribute("times!")]
        [IterTypeAttribute(typeof(__itertype_times))]
        public static int __iter_times_int(int self)
        {
            return 0;
        }

        public class __itertype_times : __itertype_upto
        {
            public __itertype_times(int self)
                : base(0, self - 1) {}
        }

        [SatherNameAttribute("for!")]
        [IterTypeAttribute(typeof(__itertype_for))]
        public static int __iter_for(int self,
                                     [ArgumentModeAttribute(ArgumentMode.Once)]
                                     int i)
        {
            return 0;
        }

        public class __itertype_for : __itertype_upto
        {
            public __itertype_for(int self, int i)
                : base(self, self + i - 1) {}
        }
    }
}
