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
using System.Reflection;

namespace Babel.Sather.Compiler
{
    public class Compiler
    {
        protected Program program;
        protected Report report;
        protected ArrayList inputFiles;
        protected ArrayList references;
        protected ArrayList linkPaths;
        protected string baseName;

        public Compiler()
        {
            report = new Report();
            inputFiles = new ArrayList();
            references = new ArrayList();
            linkPaths = new ArrayList();
        }

        public void ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++) {
                string arg = (string) args[i];
                if (arg[0] == '-') {
                    string[] vals = arg.Split(':');
                    string name, value;
                    name = vals[0];
                    if (vals.Length == 2)
                        value = vals[1];
                    else
                        value = "";
                    switch (name) {
                    case "-reference":
                    case "-r":
                        references.Add(value);
                        break;
                    case "-lib":
                        string[] dirs = value.Split(',');
                        foreach (string dir in dirs) {
                            linkPaths.Add(value);
                        }
                        break;
                    case "-help":
                    case "-h":
                        Usage();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.Error.WriteLine("unkown option: `{0}'", name);
                        Environment.Exit(1);
                        break;
                    }
                }
                else {
                    inputFiles.Add(args[i]);
                }
            }
            if (inputFiles.Count == 0) {
                Console.Error.WriteLine("no input files");
                Environment.Exit(1);
            }
            string fileName = Path.GetFileName((string) inputFiles[0]);
            baseName = Path.GetFileNameWithoutExtension(fileName);
            program = new Program(baseName);
            foreach (string reference in references) {
                LoadAssembly(reference, true);
            }
        }

        public virtual void Run()
        {
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
            Console.Write(
"usage: bsc [options] source-files\n" +
"   -lib:PATH1,PATH2   Adds the paths to the assembly link path\n" +
"   -reference:ASS     References the specified assembly (-r:ASS)\n" +
"   -help              Print this message\n");
        }

        protected virtual void LoadAssembly(string assembly, bool soft)
        {
            TypeManager typeManager = program.TypeManager;
            Assembly a;
            string totalLog = "";

            try {
                char[] path_chars = { '/', '\\', '.' };

                if (assembly.IndexOfAny(path_chars) != -1) {
                    a = Assembly.LoadFrom(assembly);
                } else {
                    a = Assembly.Load(assembly);
                }
                typeManager.AddAssembly(a);
            } catch (FileNotFoundException){
                foreach (string dir in linkPaths){
                    string full_path = Path.Combine (dir, assembly);
                    if (!assembly.EndsWith (".dll"))
                        full_path += ".dll";

                    try {
                        a = Assembly.LoadFrom (full_path);
                        typeManager.AddAssembly (a);
                        return;
                    } catch (FileNotFoundException ff) {
                        totalLog += ff.FusionLog;
                        continue;
                    }
                }
                if (!soft) {
                    Console.Error.WriteLine("cannot find assembly `{0}'",
                                            assembly);
                    Console.Error.WriteLine("Log: {0}\n" + totalLog);
                    Environment.Exit(1);
                }
            } catch (BadImageFormatException f) {
                Console.Error.WriteLine("cannot load assembly " +
                                        "(bad file format): {0}",
                                        f.FusionLog);
                Environment.Exit(1);
            } catch (FileLoadException f){
                Console.Error.WriteLine("cannot load assembly {0}: {1}",
                                        assembly, f.FusionLog);
                Environment.Exit(1);
            }
        }

        public static void Main(string[] args)
        {
            Compiler compiler = new Compiler();
            compiler.ParseArguments(args);
            compiler.Run();
        }
    }
}
