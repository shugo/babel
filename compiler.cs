/*
 * compiler.cs: compiler for Sather
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.IO;
using System.Collections;
using System.Text;

namespace Babel.Sather.Compiler
{
    public class Compiler
    {
        protected string[] inputFiles;
        protected string baseName;

        public void ParseArguments(string[] args)
        {
            if (args.Length == 0) {
                Console.Error.WriteLine("no input files");
                Environment.Exit(1);
            }
            inputFiles = args;
            string fileName = Path.GetFileName(args[0]);
            baseName = Path.GetFileNameWithoutExtension(fileName);
        }

        public virtual void Run()
        {
            Program program = new Program(baseName);
            Report report = new Report();

            foreach (string fileName in inputFiles) {
                StreamReader reader = new StreamReader(fileName);
                Parser parser = new Parser(program, reader, fileName, report);
                parser.Parse();
            }
            if (report.Errors > 0)
                Environment.Exit(1);

            ArrayList visitors = new ArrayList();
            visitors.Add(new TypeCreatingVisitor(report));
            visitors.Add(new TypeElementCreatingVisitor(report));
            visitors.Add(new TypeCheckingVisitor(report));
            visitors.Add(new CodeGeneratingVisitor(report));
            foreach (NodeVisitor visitor in visitors) {
                program.Accept(visitor);
                if (report.Errors > 0)
                    Environment.Exit(1);
            }

            program.Assembly.Save(baseName + ".exe");
        }

        protected virtual void Usage()
        {
            Console.Error.WriteLine("usage: bsc.exe filename...");
        }

        public static void Main(string[] args)
        {
            Compiler compiler = new Compiler();
            compiler.ParseArguments(args);
            compiler.Run();
        }
    }
}
