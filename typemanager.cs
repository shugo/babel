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
        protected Hashtable builtinTypes;
        protected Hashtable builtinTypeNames;
        protected ArrayList modules;
        protected Hashtable classes;
        protected Hashtable parentsTable;
        protected Hashtable ancestorsTable;
        protected Hashtable methodsTable;
        protected Hashtable constructorsTable;
        protected Hashtable parametersTable;
        protected Hashtable customAttributesTable;

        public TypeManager()
        {
            builtinTypes = new Hashtable();
            builtinTypeNames = new Hashtable();
            modules = new ArrayList();
            classes = new Hashtable();
            parentsTable = new Hashtable();
            ancestorsTable = new Hashtable();
            methodsTable = new Hashtable();
            constructorsTable = new Hashtable();
            parametersTable = new Hashtable();
            customAttributesTable = new Hashtable();
            InitBuiltinTypes();
        }

        protected virtual void InitBuiltinTypes()
        {
            AddBuiltinType("$OB", typeof(object));
            AddBuiltinType("BOOL", typeof(bool));
            AddBuiltinType("INT", typeof(int));
            AddBuiltinType("FLT", typeof(float));
            AddBuiltinType("CHAR", typeof(char));
            AddBuiltinType("STR", typeof(string));
        }

        protected virtual void AddBuiltinType(string name, Type type)
        {
            builtinTypes.Add(name, type);
            builtinTypeNames.Add(type, name);
        }

        public virtual Type GetPredefinedType(string name)
        {
            Type type = (Type) builtinTypes[name];
            if (type != null)
                return type;
            return Type.GetType(name);
        }

        public virtual Type GetTypeFromModules(string name)
        {
            foreach (Module module in modules) {
                Type type = (Type) module.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }

        public virtual Type GetType(string name)
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

        public virtual Type GetReferenceType(Type type)
        {
            string refTypeName = type.FullName + "&";
            return GetType(refTypeName);
        }

        public virtual void AddModule(Module module)
        {
            modules.Add(module);
        }

        public virtual void AddClass(ClassDefinition cls)
        {
            classes.Add(cls.Name, cls);
        }

        public virtual ClassDefinition GetClass(string name)
        {
            return (ClassDefinition) classes[name];
        }

        public void AddType(Type type, Type[] parents, Type[] ancestors)
        {
            parentsTable.Add(type, parents);
            ancestorsTable.Add(type, ancestors);
        }

        public virtual Type[] GetParents(Type type)
        {
            Type[] parents = (Type[]) parentsTable[type];
            if (parents != null)
                return parents;
            return type.GetInterfaces();
        }

        public virtual Type[] GetAncestors(Type type)
        {
            Type[] ancestors = (Type[]) ancestorsTable[type];
            if (ancestors != null)
                return ancestors;
            return ExtractAncestors(GetParents(type));
        }

        public virtual Type[] ExtractAncestors(Type[] parents)
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

        public virtual bool IsSubtype(Type type, Type supertype)
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

        public virtual string GetTypeName(Type type)
        {
            if (type == null)
                return "_";
            string name = (string) builtinTypeNames[type];
            if (name != null)
                return name;
            return type.Name;
        }

        public virtual void AddMethod(Type type, MethodInfo method)
        {
            ArrayList methods = (ArrayList) methodsTable[type];
            if (methods == null)
                methodsTable[type] = methods = new ArrayList();
            methods.Add(method);
        }

        public virtual MethodInfo[] GetMethods(Type type)
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

        public virtual void AddConstructor(Type type,
                                           ConstructorInfo constructor)
        {
            ArrayList constructors = (ArrayList) constructorsTable[type];
            if (constructors == null)
                constructorsTable[type] = constructors = new ArrayList();
            constructors.Add(constructor);
        }

        public virtual ConstructorInfo[] GetConstructors(Type type)
        {
            if (type is TypeBuilder) {
                ArrayList constructors = (ArrayList) constructorsTable[type];
                if (constructors == null)
                    return new ConstructorInfo[0];
                ConstructorInfo[] result =
                    new ConstructorInfo[constructors.Count];
                constructors.CopyTo(result);
                return result;
            }
            else {
                return type.GetConstructors(BindingFlags.Instance |
                                            BindingFlags.Public |
                                            BindingFlags.NonPublic);
            }
        }

        public virtual void AddParameters(MethodBase method,
                                          ParameterInfo[] parameters)
        {
            parametersTable[method] = parameters;
        }

        public virtual ParameterInfo[] GetParameters(MethodBase method)
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

        public virtual void
        AddCustomAttribute(ICustomAttributeProvider provider,
                           Attribute attribute)
        {
            object[] attrs = (object[]) customAttributesTable[provider];
            if (attrs == null) {
                customAttributesTable[provider] = new object[] { attribute };
            }
            else {
                object[] newAttrs = new object[attrs.Length + 1];
                Array.Copy(attrs, newAttrs, attrs.Length);
                newAttrs[attrs.Length] = attribute;
                customAttributesTable[provider] = newAttrs;
            }
        }

        public virtual object[]
        GetCustomAttributes(ICustomAttributeProvider provider,
                            Type type)
        {
            object[] attrs = (object[]) customAttributesTable[provider];
            if (attrs == null) {
                try {
                    return provider.GetCustomAttributes(type, false);
                }
                catch (NotSupportedException exception) {
                    return null;
                }
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

        public virtual string GetSatherName(ICustomAttributeProvider provider)
        {
            object[] attrs =
                GetCustomAttributes(provider, typeof(SatherNameAttribute));
            if (attrs == null || attrs.Length == 0)
                return null;
            else
                return ((SatherNameAttribute) attrs[0]).Name;
        }

        public virtual Type GetIterReturnType(ICustomAttributeProvider provider)
        {
            object[] attrs =
                GetCustomAttributes(provider,
                                    typeof(IterReturnTypeAttribute));
            if (attrs == null || attrs.Length == 0)
                return null;
            else
                return ((IterReturnTypeAttribute) attrs[0]).ReturnType;
        }

        public virtual ArgumentMode
        GetArgumentMode(ICustomAttributeProvider provider)
        {
            object[] attrs =
                GetCustomAttributes(provider,
                                    typeof(ArgumentModeAttribute));
            if (attrs == null || attrs.Length == 0)
                return ArgumentMode.In;
            else
                return ((ArgumentModeAttribute) attrs[0]).Mode;
        }

        public virtual string GetMethodName(MethodInfo method)
        {
            string name = GetSatherName(method);
            if (name == null)
                return method.Name;
            else 
                return name;
        }

        public virtual Type GetReturnType(MethodInfo method)
        {
            Type returnType = GetIterReturnType(method);
            if (returnType == null)
                return method.ReturnType;
            else 
                return returnType;
        }

        public virtual string GetMethodInfo(Type receiverType,
                                            string name,
                                            TypedNodeList arguments,
                                            Type returnType)
        {
            string methodInfo = GetTypeName(receiverType) +
                "::" + name;
            if (arguments.Length > 0) {
                methodInfo += "(";
                foreach (TypedNode arg in arguments) {
                    if (arg != arguments.First)
                        methodInfo += ",";
                    methodInfo += GetTypeName(arg.NodeType);
                }
                methodInfo += ")";
            }
            if (returnType != null && returnType != typeof(void))
                methodInfo += ":" + GetTypeName(returnType);
            return methodInfo;
        }

        public virtual string GetMethodInfo(MethodInfo method)
        {
            string methodInfo = GetTypeName(method.DeclaringType);
            string name = GetSatherName(method);
            if (name == null)
                name = method.Name;
            methodInfo += "::" + name;
            ParameterInfo[] parameters = GetParameters(method);
            if (parameters.Length > 0) {
                methodInfo += "(";
                bool first = true;
                foreach (ParameterInfo param in parameters) {
                    if (first)
                        first = false;
                    else
                        methodInfo += ",";
                    methodInfo += GetTypeName(param.ParameterType);
                }
                methodInfo += ")";
            }
            Type returnType = GetReturnType(method);
            if (returnType != typeof(void))
                methodInfo += ":" + GetTypeName(returnType);
            return methodInfo;
        }

        public virtual ArrayList GetAncestorMethods(Type type)
        {
            Type[] ancestors = GetAncestors(type);
            ArrayList result = new ArrayList();
            foreach (Type ancestor in ancestors) {
                MethodInfo[] methods = GetMethods(ancestor);
                result.AddRange(methods);
            }
            return result;
        }
    }
}
