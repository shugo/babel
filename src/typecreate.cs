/*
 * typecreate.cs: create types
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babell.Compiler {
    public class TypeCreatingVisitor : AbstractNodeVisitor {
        protected Program program;
        protected TypeManager typeManager;
        protected Report report;
        protected SourceFile currentSouceFile;
        protected Hashtable visitingClasses;
        protected int adapterCount;

        public TypeCreatingVisitor(Report report)
        {
            this.report = report;
        }

        public override void VisitProgram(Program program)
        {
            this.program = program;
            typeManager = program.TypeManager;
            visitingClasses = new Hashtable();
            adapterCount = 0;
            program.Children.Accept(this);
        }

        public override void VisitSourceFile(SourceFile sourceFile)
        {
            currentSouceFile = sourceFile;
            sourceFile.Children.Accept(this);
        }

        public override void VisitClass(ClassDefinition cls)
        {
            bool error = false;
            if (cls.TypeBuilder != null)
                return;
            TypeData type = typeManager.GetPredefinedType(cls.Name);
            if (type != null) {
                report.Error(cls.Location,
                             "redefinition of class {0}", cls.Name);
                return;
            }
            visitingClasses.Add(cls, cls);
            try {
                cls.Supertypes.Accept(this);
                cls.TypeData = new UserDefinedTypeData(typeManager, null);
                cls.TypeData.Parents = new ArrayList();
                foreach (TypeSpecifier supertype in cls.Supertypes) {
                    TypeData anc = supertype.NodeType;
                    if (anc == null)
                        return;
                    if (!anc.IsAbstract) {
                        report.Error(supertype.Location,
                                     "supertype {0} is not abstract",
                                     supertype.Name);
                        return;
                    }
                    cls.TypeData.Parents.Add(anc);
                }
                TypeAttributes attrs = TypeAttributes.Public;
                Type parent;
                if (cls.Kind == ClassKind.Abstract) {
                    attrs |= TypeAttributes.Abstract;
                    attrs |= TypeAttributes.Interface;
                    parent = null;
                }
                else {
                    attrs |= TypeAttributes.Class;
                    parent = typeof(object);
                }
                cls.TypeBuilder =
                    program.Module.DefineType(cls.Name, attrs,
                                              parent,
                                              cls.TypeData.AncestorRawTypes);
                cls.TypeData.RawType = cls.TypeBuilder;
                typeManager.AddType(cls.TypeData);
                if (cls.Kind == ClassKind.Reference) {
                    cls.Constructor = 
                        cls.TypeBuilder.
                        DefineDefaultConstructor(MethodAttributes.Public);
                    UserDefinedConstructorData constructorData =
                        typeManager.AddConstructor(cls.TypeBuilder,
                                                   cls.Constructor);
                    typeManager.AddParameters(cls.Constructor,
                                              new ParameterInfo[0]);
                    constructorData.Parameters = new ArrayList();
                    cls.StaticConstructor = 
                        cls.TypeBuilder.
                        DefineConstructor(MethodAttributes.Static,
                                          CallingConventions.Standard,
                                          Type.EmptyTypes);
                }
                if (cls.Subtypes != null) {
                    cls.Subtypes.Accept(this);
                    foreach (TypeSpecifier subtype in cls.Subtypes) {
                        if (subtype.NodeType == null)
                            return;
                        SupertypingAdapter adapter =
                            new SupertypingAdapter(subtype.NodeType);
                        adapter.TypeData =
                            new UserDefinedTypeData(typeManager, null);
                        adapter.TypeData.Parents = new ArrayList();
                        adapter.TypeData.Parents.Add(cls.TypeData);
                        adapter.TypeBuilder =
                            program.Module.
                            DefineType("__adapter" + adapterCount,
                                       TypeAttributes.Class |
                                       TypeAttributes.Public,
                                       typeof(object),
                                       adapter.TypeData.AncestorRawTypes);
                        adapter.TypeData.RawType = adapter.TypeBuilder;
                        typeManager.AddType(adapter.TypeData);
                        cls.Adapters.Add(adapter);
                        typeManager.AddSupertypingAdapter(cls.TypeBuilder,
                                                      adapter.AdapteeType,
                                                      adapter.TypeBuilder);
                        adapterCount++;
                    }
                }
            }
            finally {
                visitingClasses.Remove(cls);
            }
        }

        public override void VisitTypeSpecifier(TypeSpecifier typeSpecifier)
        {
            if (typeSpecifier.Kind == TypeKind.Same) {
                report.Error(typeSpecifier.Location,
                             "SAME cannot appear in subtyping clause");
                return;
            }
            TypeData type =
                typeManager.GetType(typeSpecifier.Name,
                                    currentSouceFile.ImportedNamespaces);
            if (type != null) {
                typeSpecifier.NodeType = type;
                return;
            }
            ClassDefinition cls = typeManager.GetClass(typeSpecifier.Name);
            if (cls == null) {
                report.Error(typeSpecifier.Location,
                             "there is no class named {0}", typeSpecifier);
                return;
            }
            if (visitingClasses.ContainsKey(cls)) {
                report.Error(cls.Location,
                             "subtype cycle detected involving {0}",
                             cls.Name);
                return;
            }
            VisitClass(cls);
            typeSpecifier.NodeType = typeManager.GetTypeData(cls.TypeBuilder);
        }
    }
}