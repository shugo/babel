/*
 * typecreate.cs: create types
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Sather.Compiler
{
    public class TypeCreatingVisitor : AbstractNodeVisitor
    {
        Program program;
        TypeManager typeManager;
        Report report;
        Hashtable visitingClasses;

        public TypeCreatingVisitor(Report report)
        {
            this.report = report;
        }

        public override void VisitProgram(Program program)
        {
            this.program = program;
            typeManager = program.TypeManager;
            visitingClasses = new Hashtable();
            program.Children.Accept(this);
        }

        public override void VisitSourceFile(SourceFile sourceFile)
        {
            sourceFile.Children.Accept(this);
        }

        public override void VisitClass(ClassDefinition cls)
        {
            bool error = false;
            if (cls.TypeBuilder != null)
                return;
            Type type = typeManager.GetPredefinedType(cls.Name);
            if (type != null) {
                report.Error(cls.Location,
                             "redefinition of class {0}", cls.Name);
                return;
            }
            visitingClasses.Add(cls, cls);
            cls.Supertypes.Accept(this);
            Type[] parents = new Type[cls.Supertypes.Length];
            int i = 0;
            foreach (TypeSpecifier supertype in cls.Supertypes) {
                Type t = supertype.NodeType;
                if (t == null) {
                    error = true;
                    break;
                }
                if (!t.IsInterface) {
                    report.Error(supertype.Location,
                                 "supertype {0} is not abstract",
                                 supertype.Name);
                    error = true;
                    break;
                }
                parents[i++] = t;
            }
            if (!error) {
                Type[] ancestors = typeManager.ExtractAncestors(parents);
                TypeAttributes attrs = TypeAttributes.Public;
                if (cls.Kind == ClassKind.Abstract)
                    attrs |= TypeAttributes.Interface;
                cls.TypeBuilder =
                    program.Module.DefineType(cls.Name, attrs,
                                              typeof(object), ancestors);
                typeManager.AddType(cls.TypeBuilder, parents, ancestors);
                if (cls.Kind == ClassKind.Reference) {
                    cls.Constructor = 
                        cls.TypeBuilder.
                        DefineDefaultConstructor(MethodAttributes.Public);
                    typeManager.AddConstructor(cls.TypeBuilder,
                                               cls.Constructor);
                    cls.StaticConstructor = 
                        cls.TypeBuilder.
                        DefineConstructor(MethodAttributes.Static,
                                          CallingConventions.Standard,
                                          Type.EmptyTypes);
                }
            }
            visitingClasses.Remove(cls);
        }

        public override void VisitTypeSpecifier(TypeSpecifier typeSpecifier)
        {
            if (typeSpecifier.Kind == TypeKind.Same) {
                report.Error(typeSpecifier.Location,
                             "SAME cannot appear in subtyping clause");
                return;
            }
            Type type = typeManager.GetType(typeSpecifier.Name);
            if (type != null) {
                typeSpecifier.NodeType = type;
                return;
            }
            ClassDefinition cls = typeManager.GetClass(typeSpecifier.Name);
            if (cls == null) {
                report.Error(typeSpecifier.Location,
                             "there is no class named {0}", typeSpecifier.Name);
                return;
            }
            if (visitingClasses.ContainsKey(cls)) {
                report.Error(cls.Location,
                             "subtype cycle detected involving {0}",
                             cls.Name);
                return;
            }
            VisitClass(cls);
            typeSpecifier.NodeType = cls.TypeBuilder;
        }
    }
}
