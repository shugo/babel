/*
 * typemanager.cs: type manager
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babel.Sather.Base;

namespace Babel.Sather.Compiler
{
    public class TypeManager
    {
        Hashtable builtinTypes;
        Hashtable builtinTypeNames;
        ArrayList modules;
        Hashtable classes;
        Hashtable parentsTable;
        Hashtable ancestorsTable;
        Hashtable methodsTable;
        Hashtable parametersTable;
        Hashtable customAttributesTable;

        public TypeManager()
        {
            builtinTypes = new Hashtable();
            builtinTypeNames = new Hashtable();
            modules = new ArrayList();
            classes = new Hashtable();
            parentsTable = new Hashtable();
            ancestorsTable = new Hashtable();
            methodsTable = new Hashtable();
            parametersTable = new Hashtable();
            customAttributesTable = new Hashtable();
            InitBuiltinTypes();
        }

        protected void InitBuiltinTypes()
        {
            AddBuiltinType("$OB", typeof(object));
            AddBuiltinType("BOOL", typeof(bool));
            AddBuiltinType("INT", typeof(int));
            AddBuiltinType("FLT", typeof(float));
            AddBuiltinType("CHAR", typeof(char));
            AddBuiltinType("STR", typeof(string));
        }

        protected void AddBuiltinType(string name, Type type)
        {
            builtinTypes.Add(name, type);
            builtinTypeNames.Add(type, name);
        }

        public Type GetPredefinedType(string name)
        {
            Type type = (Type) builtinTypes[name];
            if (type != null)
                return type;
            return Type.GetType(name);
        }

        public Type GetTypeFromModules(string name)
        {
            foreach (Module module in modules) {
                Type type = (Type) module.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }

        public Type GetType(string name)
        {
            ClassDefinition cls = GetClass(name);
            if (cls != null && cls.TypeBuilder != null)
                return cls.TypeBuilder;
            Type type = GetTypeFromModules(name);
            if (type != null)
                return type;
            type = GetPredefinedType(name);
            if (type != null)
                return type;
            return null;
        }

        public Type GetReferenceType(Type type)
        {
            string refTypeName = type.FullName + "&";
            return GetType(refTypeName);
        }

        public void AddModule(Module module)
        {
            modules.Add(module);
        }

        public void AddClass(ClassDefinition cls)
        {
            classes.Add(cls.Name, cls);
        }

        public ClassDefinition GetClass(string name)
        {
            return (ClassDefinition) classes[name];
        }

        public void AddType(Type type, Type[] parents, Type[] ancestors)
        {
            parentsTable.Add(type, parents);
            ancestorsTable.Add(type, ancestors);
        }

        public Type[] GetParents(Type type)
        {
            Type[] parents = (Type[]) parentsTable[type];
            if (parents != null)
                return parents;
            return type.GetInterfaces();
        }

        public Type[] GetAncestors(Type type)
        {
            Type[] ancestors = (Type[]) ancestorsTable[type];
            if (ancestors != null)
                return ancestors;
            return ExtractAncestors(GetParents(type));
        }

        public Type[] ExtractAncestors(Type[] parents)
        {
            Hashtable tbl = new Hashtable();
            foreach (Type parent in parents) {
                tbl[parent] = parent;
                foreach (Type anc in GetAncestors(parent)) {
                    tbl[anc] = anc;
                }
            }
            Type[] ancestors = new Type[tbl.Count];
            tbl.Values.CopyTo(ancestors, 0);
            return ancestors;
        }

        public bool IsSubtype(Type type, Type supertype)
        {
            if (type == null)
                return false;
            if (type == supertype)
                return true;
            if (supertype == typeof(object))
                return true;
            Type[] ancestors = GetAncestors(type);
            return ((IList) ancestors).Contains(supertype);
        }

        public string GetTypeName(Type type)
        {
            if (type == null)
                return "_";
            string name = (string) builtinTypeNames[type];
            if (name != null)
                return name;
            return type.Name;
        }

        public void AddMethod(Type type, MethodInfo method)
        {
            ArrayList methods = (ArrayList) methodsTable[type];
            if (methods == null)
                methodsTable[type] = methods = new ArrayList();
            methods.Add(method);
        }

        public MethodInfo[] GetMethods(Type type)
        {
            if (type is TypeBuilder) {
                ArrayList methods = (ArrayList) methodsTable[type];
                if (methods == null)
                    return new MethodInfo[0];
                MethodInfo[] result = new MethodInfo[methods.Count];
                methods.CopyTo(result);
                return result;
            }
            else {
                return type.GetMethods(BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.Public |
                                       BindingFlags.NonPublic);
            }
        }

        public void AddParameters(MethodInfo method, ParameterInfo[] parameters)
        {
            parametersTable[method] = parameters;
        }

        public ParameterInfo[] GetParameters(MethodInfo method)
        {
            ParameterInfo[] parameters =
                (ParameterInfo[]) parametersTable[method];
            if (parameters == null) {
                if (method is MethodBuilder)
                    return new Parameter[0];
                parametersTable[method] = parameters = method.GetParameters();
            }
            return parameters;
        }

        public void AddCustomAttribute(ICustomAttributeProvider provider,
                                       Attribute attribute)
        {
            object[] attrs = (object[]) customAttributesTable[provider];
            if (attrs == null) {
                customAttributesTable[provider] = new object[] { attribute };
            }
            else {
                object[] newAttrs = new object[attrs.Length + 1];
                Array.Copy(attrs, newAttrs, attrs.Length);
                customAttributesTable[provider] = newAttrs;
            }
        }

        public object[] GetCustomAttributes(ICustomAttributeProvider provider,
                                            Type type)
        {
            object[] attrs = (object[]) customAttributesTable[provider];
            if (attrs == null) {
                return provider.GetCustomAttributes(type, false);
            }
            else {
                ArrayList list = new ArrayList();
                foreach (object attr in attrs) {
                    if (type.IsInstanceOfType(attr))
                        list.Add(attr);
                }
                object[] attributes = new object[list.Count];
                list.CopyTo(attributes);
                return attributes;
            }
        }

        public Type GetIterReturnType(ICustomAttributeProvider provider)
        {
            object[] attrs =
                GetCustomAttributes(provider,
                                    typeof(IterReturnTypeAttribute));
            if (attrs == null || attrs.Length == 0)
                return null;
            return ((IterReturnTypeAttribute) attrs[0]).Type;
        }
    }
}
