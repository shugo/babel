/*
 * lexer.cs: lexical analyzer
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Babel.Compiler {
    public class LexicalAnalyzer : yyParser.yyInput {
        const int EOF = -1;
        const int EMPTY = -2;

        protected TextReader reader;
        protected string fileName;
        protected Report report;

        protected int line;
        protected int column;
        protected int prevColumn;

        protected int nextChar;
        protected int pushedbackChar;
        protected Object val;

        protected int nextToken;

        protected Hashtable keywords;
        protected Hashtable bangKeywords;

        public LexicalAnalyzer(TextReader reader, string fileName,
                               Report report)
        {
            this.reader = reader;
            this.fileName = fileName;
            this.report = report;
            line = 1;
            column = 0;
            prevColumn = 0;
            nextChar = EMPTY;
            pushedbackChar = EMPTY;
            val = null;
            nextToken = Token.NONE;
            keywords = GetKeywords();
            bangKeywords = GetBangKeywords();
        }

        protected virtual Hashtable GetKeywords()
        {
            Hashtable keywords = new Hashtable();
            keywords.Add("abstract", Token.ABSTRACT);
            keywords.Add("and", Token.AND);
            keywords.Add("any", Token.ANY);
            keywords.Add("assert", Token.ASSERT);
            keywords.Add("attr", Token.ATTR);
            keywords.Add("bind", Token.BIND);
            keywords.Add("builtin", Token.BUILTIN);
            keywords.Add("case", Token.CASE);
            keywords.Add("class", Token.CLASS);
            keywords.Add("clusters", Token.CLUSTERS);
            keywords.Add("cohort", Token.COHORT);
            keywords.Add("const", Token.CONST);
            keywords.Add("else", Token.ELSE);
            keywords.Add("elsif", Token.ELSIF);
            keywords.Add("end", Token.END);
            keywords.Add("exception", Token.EXCEPTION);
            keywords.Add("external", Token.EXTERNAL);
            keywords.Add("false", Token.FALSE);
            keywords.Add("far", Token.FAR);
            keywords.Add("fork", Token.FORK);
            keywords.Add("guard", Token.GUARD);
            keywords.Add("if", Token.IF);
            keywords.Add("immutable", Token.IMMUTABLE);
            keywords.Add("import", Token.IMPORT);
            keywords.Add("inout", Token.INOUT);
            keywords.Add("include", Token.INCLUDE);
            keywords.Add("initial", Token.INITIAL);
            keywords.Add("is", Token.IS);
            keywords.Add("ITER", Token.ITER);
            keywords.Add("lock", Token.LOCK);
            keywords.Add("loop", Token.LOOP);
            keywords.Add("namespace", Token.NAMESPACE);
            keywords.Add("near", Token.NEAR);
            keywords.Add("new", Token.NEW);
            keywords.Add("once", Token.ONCE);
            keywords.Add("or", Token.OR);
            keywords.Add("out", Token.OUT);
            keywords.Add("par", Token.PAR);
            keywords.Add("parloop", Token.PARLOOP);
            keywords.Add("post", Token.POST);
            keywords.Add("pre", Token.PRE);
            keywords.Add("private", Token.PRIVATE);
            keywords.Add("protect", Token.PROTECT);
            keywords.Add("quit", Token.QUIT);
            keywords.Add("raise", Token.RAISE);
            keywords.Add("readonly", Token.READONLY);
            keywords.Add("result", Token.RESULT);
            keywords.Add("return", Token.RETURN);
            keywords.Add("ROUT", Token.ROUT);
            keywords.Add("F_ROUT", Token.F_ROUT);
            keywords.Add("SAME", Token.SAME);
            keywords.Add("self", Token.SELF);
            keywords.Add("shared", Token.SHARED);
            keywords.Add("sync", Token.SYNC);
            keywords.Add("then", Token.THEN);
            keywords.Add("true", Token.TRUE);
            keywords.Add("typecase", Token.TYPECASE);
            keywords.Add("unlock", Token.UNLOCK);
            keywords.Add("void", Token.VOID);
            keywords.Add("when", Token.WHEN);
            keywords.Add("with", Token.WITH);
            keywords.Add("yield", Token.YIELD);
            return keywords;
        }

        protected virtual Hashtable GetBangKeywords()
        {
            Hashtable keywords = new Hashtable();
            keywords.Add("break", Token.BREAK_BANG);
            keywords.Add("clusters", Token.CLUSTERS_BANG);
            keywords.Add("until", Token.UNTIL_BANG);
            keywords.Add("while", Token.WHILE_BANG);
            return keywords;
        }

        public virtual Location Location {
            get { return new Location(fileName, line, column); }
        }

        protected virtual int NextChar()
        {
            if (nextChar == EMPTY) {
                if (pushedbackChar == EMPTY) {
                    nextChar = reader.Read();
                }
                else {
                    nextChar = pushedbackChar;
                    pushedbackChar = EMPTY;
                }
            }
            return nextChar;
        }

        protected virtual int GetChar()
        {
            int c;

            if (nextChar == EMPTY) {
                if (pushedbackChar == EMPTY) {
                    c = reader.Read();
                }
                else {
                    c = pushedbackChar;
                    pushedbackChar = EMPTY;
                }
            }
            else {                
                c = nextChar;
                nextChar = EMPTY;
            }
            switch (c) {
            case '\t':
                prevColumn = column;
                column = column + 8 - (column - 1) % 8;
                break;
            case '\n':
                line++;
                prevColumn = column;
                column = 0;
                break;
            default:
                column++;
                break;
            }
            return c;
        }

        protected virtual void Pushback(int c)
        {
            pushedbackChar = nextChar;
            nextChar = c;
            switch (c) {
            case '\t':
                column = prevColumn;
                break;
            case '\n':
                line--;
                column = prevColumn;
                break;
            default:
                column--;
                break;
            }
        }

        protected virtual void SkipComment()
        {
            int n = 1;
            for (int c = GetChar(); c != EOF; c = GetChar()) {
                if (c == '(' && NextChar() == '*') {
                    GetChar();
                    n++;
                }
                else if (c == '*' && NextChar() == ')') {
                    GetChar();
                    n--;
                    if (n == 0)
                        break;
                }
            }
        }

        protected virtual bool IsIdentifierStartChar(int c)
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                Char.IsLetter((char) c);
        }

        protected virtual int Identifier(int startChar)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append((char) startChar);
            for (int c = GetChar(); c != EOF; c = GetChar()) {
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    Char.IsLetter((char) c) ||
                    c == '_' ||
                    (c >= '0' && c <= '9')) {
                    buf.Append((char) c);
                }
                else {
                    Pushback(c);
                    break;
                }
            }
            string s = buf.ToString();
            Object token = bangKeywords[s];
            if (token != null && NextChar() == '!') {
                GetChar();
                return (int) token;
            }
            token = keywords[s];
            if (token != null)
                return (int) token;
            val = s;
            return Token.IDENTIFIER;
        }

        protected virtual int UnescapeChar()
        {
            int result, c = GetChar();

            switch (c) {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
                result = c - (int) '0';
                c = NextChar();
                while (c >= '0' && c <= '7') {
                    int n = c - (int) '0';
                    result = result * 8 + n;
                    GetChar();
                    c = NextChar();
                }
                break;
            case 'a':
                result = '\a';
                break;
            case 'b':
                result = '\b';
                break;
            case 'f':
                result = '\f';
                break;
            case 'n':
                result = '\n';
                break;
            case 'r':
                result = '\r';
                break;
            case 't':
                result = '\t';
                break;
            case 'v':
                result = '\v';
                break;
            case '\\':
                result = '\\';
                break;
            case '\'':
                result = '\'';
                break;
            case '\"':
                result = '\"';
                break;
            default:
                result = c;
                break;
            }
            return result;
        }

        protected virtual int CharLiteral()
        {
            int c = GetChar();
            if (c == '\\')
                c = UnescapeChar();
            if (c == EOF) {
                report.Error(Location, "malformed CHAR literal");
                return Token.ERROR;
            }
            val = (char) c;
            c = GetChar();
            if (c != '\'') {
                report.Error(Location, "unterminated CHAR literal");
                return Token.ERROR;
            }
            return Token.CHAR_LITERAL;
        }
        
        protected virtual int StrLiteral()
        {
            StringBuilder buf = new StringBuilder();
            int c = GetChar();
            while (c != '"' && c != '\n' && c != EOF) {
                if (c == '\\')
                    c = UnescapeChar();
                if (c == EOF) {
                    report.Error(Location, "malformed STR literal");
                    return Token.ERROR;
                }
                buf.Append((char) c);
                c = GetChar();
            }
            if (c != '"') {
                report.Error(Location, "unterminated STR literal");
                return Token.ERROR;
            }
            val = buf.ToString();
            return Token.STR_LITERAL;
        }

        protected virtual int GetInt(int startChar, int baseNumber)
        {
            int result = 0;
            int x;

            for (int c = startChar; c != EOF; c = GetChar()) {
                if (c == '_')
                    continue;
                if (c >= '0' && c <= '9') {
                    x = c - '0';
                }
                else if (c >= 'a' && c <= 'z') {
                    x = c - 'a' + 10;
                }
                else if (c >= 'A' && c <= 'Z') {
                    x = c - 'A' + 10;
                }
                else {
                    Pushback(c);
                    break;
                }
                if (x >= baseNumber) {
                    Pushback(c);
                    break;
                }
                result = result * baseNumber + x;
            }
            return result;
        }

        protected virtual int Number(int startChar)
        {
            int baseNumber = 10;
            int c;

            if (startChar == '0') {
                c = NextChar();
                switch (c) {
                case 'b':
                    GetChar();
                    baseNumber = 2;
                    break;
                case 'o':
                    GetChar();
                    baseNumber = 8;
                    break;
                case 'x':
                    GetChar();
                    baseNumber = 16;
                    break;
                }
                c = GetChar();
            }
            else {
                c = startChar;
            }
            int x = GetInt(c, baseNumber);
            val = x;
            // ToDo: supports float.
            return Token.INT_LITERAL;
        }

        public virtual bool advance()
        {
            return NextChar() != EOF;
        }

        public virtual int token()
        {
            bool whitespace = false;

            if (nextToken != Token.NONE) {
                int t = nextToken;
                nextToken = Token.NONE;
                return t;
            }

            for (int c = GetChar(); c != EOF; c = GetChar()) {
                switch (c) {
                case ' ':
                case '\t':
                case '\v':
                case '\b':
                case '\r':
                case '\f':
                    whitespace = true;
                    break;
                case '\n':
		    return Token.NL;
                case '(':
                    if (NextChar() == '*') {
                        GetChar();
                        SkipComment();
                        whitespace = true;
                        break;
                    }
                    return Token.LPAREN;
                case ')':
                    return Token.RPAREN;
                case '[':
                    return Token.LBRACKET;
                case ']':
                    return Token.RBRACKET;
                case '{':
                    return Token.LBRACE;
                case '}':
                    return Token.RBRACE;
                case ',':
                    return Token.COMMA;
                case '.':
                    return Token.DOT;
                case ';':
                    return Token.SEMI;
                case '$':
                    if (!IsIdentifierStartChar(NextChar())) {
                        report.Error(Location, "'$' wihtout class name");
                        return Token.ERROR;
                    }
                    Identifier(c);
                    return Token.ABSTRACT_CLASS_NAME;
                case '+':
                    return Token.PLUS;
                case '-':
                    if (NextChar() == '-') {
                        GetChar();
                        for (int ch = GetChar(); ch != EOF; ch = GetChar()) {
                            if (ch == '\n')
                                break;
                        }
                        whitespace = true;
                        break;
                    }
                    else if (NextChar() == '>') {
                        GetChar();
                        return Token.TRANSFORM;
                    }
                    else {
                        return Token.MINUS;
                    }
                case '*':
                    return Token.TIMES;
                case '#':
                    return Token.SHARP;
                case '^':
                    return Token.POW;
                case '%':
                    return Token.MOD;
                case '|':
                    return Token.VBAR;
                case '!':
                    if (whitespace)
                        return Token.BANG;
                    else
                        return Token.ITER_BANG;
                case '_':
                    return Token.UNDER;
                case '=':
                    return Token.IS_EQ;
                case ':':
                    c = NextChar();
                    switch (c) {
                    case ':':
                        GetChar();
                        c = NextChar();
                        if (c == '=') {
                            Pushback(':');
                            return Token.COLON;
                        }
                        else {
                            return Token.DCOLON;
                        }
                    case '=':
                        GetChar();
                        return Token.ASSIGN;
                    default:
                        return Token.COLON;
                    }
                case '/':
                    if (NextChar() == '=') {
                        GetChar();
                        return Token.IS_NEQ;
                    }
                    else {
                        return Token.QUOTIENT;
                    }
                case '<':
                    if (NextChar() == '=') {
                        GetChar();
                        return Token.IS_LEQ;
                    }
                    else {
                        return Token.IS_LT;
                    }
                case '>':
                    if (NextChar() == '=') {
                        GetChar();
                        return Token.IS_GEQ;
                    }
                    else {
                        return Token.IS_GT;
                    }
                case '~':
                    return Token.NOT;
                case '\'':
                    return CharLiteral();
                case '"':
                    return StrLiteral();
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return Number(c);
                default:
                    if (!IsIdentifierStartChar(c)) {
                        report.Error(Location,
                                     "unknown character '{0}'", (char) c);
                        return Token.ERROR;
                    }
                    return Identifier(c);
                }
            }
            return Token.EOF;
        }

        public virtual Object value()
        {
            return val;
        }

        public virtual int lookahead()
        {
            if (nextToken != Token.NONE)
                return nextToken;
            nextToken = token();
            return nextToken;
        }
    }
}
