/*
 * typemanager.cs: type manager
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babel.Core;

namespace Babel.Compiler {
    public class TypeManager {
        protected ArrayList assemblies;
        protected ArrayList modules;
        protected Hashtable classes;
        protected Hashtable typeDataTable;
        protected Hashtable parentsTable;
        protected Hashtable ancestorsTable;
        protected Hashtable methodsTable;
        protected Hashtable constructorsTable;
        protected Hashtable parametersTable;
        protected Hashtable customAttributesTable;
        protected Hashtable builtinTypes;
        protected Hashtable builtinTypeNames;
        protected Hashtable builtinMethodContainers;
        protected TypeData obType;
        protected TypeData boolType;
        protected TypeData intType;
        protected TypeData fltType;
        protected TypeData charType;
        protected TypeData strType;
        protected TypeData voidType;
        protected TypeData exceptionType;

        public TypeManager()
        {
            assemblies = new ArrayList();
            modules = new ArrayList();
            classes = new Hashtable();
            typeDataTable = new Hashtable();
            parentsTable = new Hashtable();
            ancestorsTable = new Hashtable();
            methodsTable = new Hashtable();
            constructorsTable = new Hashtable();
            parametersTable = new Hashtable();
            customAttributesTable = new Hashtable();
            builtinTypes = new Hashtable();
            builtinTypeNames = new Hashtable();
            builtinMethodContainers = new Hashtable();
            InitBuiltinTypes();
        }

        public virtual TypeData ObType {
            get { return obType; }
        }

        public virtual TypeData BoolType {
            get { return boolType; }
        }

        public virtual TypeData IntType {
            get { return intType; }
        }

        public virtual TypeData FltType {
            get { return fltType; }
        }

        public virtual TypeData CharType {
            get { return charType; }
        }

        public virtual TypeData StrType {
            get { return strType; }
        }

        public virtual TypeData VoidType {
            get { return voidType; }
        }

        public virtual TypeData ExceptionType {
            get { return exceptionType; }
        }

        protected virtual void InitBuiltinTypes()
        {
            obType = AddBuiltinType("$ob", typeof(object));
            boolType = AddBuiltinType("bool", typeof(bool),
                                      typeof(Babel.Core.BOOL));
            intType = AddBuiltinType("int", typeof(int),
                                     typeof(Babel.Core.INT));
            fltType = AddBuiltinType("flt", typeof(float));
            charType = AddBuiltinType("char", typeof(char));
            strType = AddBuiltinType("str", typeof(string),
                                     typeof(Babel.Core.STR));
	    voidType = new PredefinedTypeData(this, typeof(void));
	    exceptionType = new PredefinedTypeData(this,
						   typeof(System.Exception));
        }

        protected virtual BuiltinTypeData AddBuiltinType(string name, Type type)
        {
            BuiltinTypeData typeData = new BuiltinTypeData(this, type, name);
            builtinTypes.Add(name, typeData);
            builtinTypeNames.Add(type, name);
            typeDataTable.Add(type, typeData);
            return typeData;
        }

        protected virtual BuiltinTypeData
            AddBuiltinType(string name, Type type,
                           Type builtinMethodContainer)
        {
            BuiltinTypeData typeData = AddBuiltinType(name, type);
            typeData.BuiltinMethodContainer = builtinMethodContainer;
            TypeData container = GetTypeData(builtinMethodContainer);
            builtinMethodContainers.Add(typeData, container);
            return typeData;
        }

        public virtual TypeData GetBuiltinMethodContainer(TypeData typeData)
        {
            return (TypeData) builtinMethodContainers[typeData];
        }

        public virtual TypeData GetTypeData(Type type)
        {
            if (type == null)
                return null;
            TypeData typeData = (TypeData) typeDataTable[type];
            if (typeData == null) {
                if (type is TypeBuilder) {
                    typeData = new UserDefinedTypeData(this,
                                                       (TypeBuilder) type);
                }
                else {
                    typeData = new PredefinedTypeData(this, type);
                }
                typeDataTable[type] = typeData;
            }
            return typeData;
        }

        public virtual TypeData GetPredefinedType(string name)
        {
            TypeData typeData = (TypeData) builtinTypes[name.ToLower()];
            if (typeData != null)
                return typeData;
            foreach (Assembly assembly in assemblies) {
                Type type = (Type) assembly.GetType(name, false, true);
                if (type != null)
                    return GetTypeData(type);
            }
            return GetTypeData(Type.GetType(name, false, true));
        }

        public virtual TypeData GetTypeFromModules(string name)
        {
            foreach (Module module in modules) {
                Type type = (Type) module.GetType(name, false, true);
                if (type != null)
                    return GetTypeData(type);
            }
            return null;
        }

        public virtual TypeData GetType(string name)
        {
            ClassDefinition cls = GetClass(name);
            if (cls != null && cls.TypeBuilder != null)
                return GetTypeData(cls.TypeBuilder);
            TypeData typeData = GetTypeFromModules(name);
            if (typeData != null)
                return typeData;
            typeData = GetPredefinedType(name);
            if (typeData != null)
                return typeData;
            return null;
        }

        public virtual TypeData GetType(string name, ArrayList namespaces)
        {
            TypeData typeData = GetType(name);
            if (typeData != null)
                return typeData;
            foreach (string ns in namespaces) {
                typeData = GetType(ns + Type.Delimiter + name);
                if (typeData != null)
                    return typeData;
            }
            return null;
        }

        public virtual TypeData GetType(TypeSpecifier typeSpecifier,
                                        ArrayList namespaces)
        {
            TypeData typeData = GetType(typeSpecifier.Name);
            if (typeData == null) {
                foreach (string ns in namespaces) {
                    typeData = GetType(ns + Type.Delimiter +
                                       typeSpecifier.Name);
                    if (typeData != null)
                        break;
                }
            }
            if (typeData == null)
                return null;
            TypedNodeList typeParameters = typeSpecifier.TypeParameters;
            if (typeParameters.Length == 0) {
                return typeData;
            }
            else {
                TypeData genericTypeDef = typeData.GetGenericTypeDefinition();
                return genericTypeDef.BindGenericParameters(typeParameters);
            }
        }

        public virtual void AddAssembly(Assembly assembly)
        {
            assemblies.Add(assembly);
        }

        public virtual void AddModule(Module module)
        {
            modules.Add(module);
        }

        public virtual void AddClass(ClassDefinition cls)
        {
            classes.Add(cls.Name.ToLower(), cls);
        }

        public virtual ClassDefinition GetClass(string name)
        {
            return (ClassDefinition) classes[name.ToLower()];
        }

        public void AddType(TypeData type)
        {
            typeDataTable[type.RawType] = type;
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
            ArrayList ancestors = new ArrayList();
            foreach (Type parent in parents) {
                foreach (Type anc in GetAncestors(parent)) {
                    if (!ancestors.Contains(anc))
                        ancestors.Add(anc);
                }
                if (!ancestors.Contains(parent))
                    ancestors.Add(parent);
            }
            Type[] result = new Type[ancestors.Count];
            ancestors.CopyTo(result, 0);
            return result;
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
            if (((IList) ancestors).Contains(supertype))
                return true;
            return GetSupertypingAdapter(supertype, type) != null;
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

        public virtual UserDefinedMethodData
            AddMethod(Type type, MethodBuilder method)
        {
            ArrayList methods = (ArrayList) methodsTable[type];
            if (methods == null)
                methodsTable[type] = methods = new ArrayList();
            methods.Add(method);

            TypeData typeData = GetTypeData(type);
            UserDefinedMethodData methodData =
                new UserDefinedMethodData(this, method);
            typeData.Methods.Add(methodData);
            return methodData;
        }

        public virtual MethodInfo[] GetMethods(TypeData typeData)
        {
            Type type = typeData.RawType;

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

        public virtual UserDefinedConstructorData
            AddConstructor(Type type, ConstructorBuilder constructor)
        {
            ArrayList constructors = (ArrayList) constructorsTable[type];
            if (constructors == null)
                constructorsTable[type] = constructors = new ArrayList();
            constructors.Add(constructor);

            TypeData typeData = GetTypeData(type);
            UserDefinedConstructorData constructorData =
                new UserDefinedConstructorData(this, constructor);
            typeData.Constructors.Add(constructorData);
            return constructorData;
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
                if (provider is Type &&
                    ((Type) provider).IsGenericInstance)
                    return null;
                if (provider is MethodBase &&
                    ((MethodBase) provider).DeclaringType.IsGenericInstance)
                    return null;
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

        public void AddBabelName(MethodBuilder methodBuilder,
                                  string babelName)
        {
            Type[] paramTypes = new Type[] { typeof(string) };
            ConstructorInfo constructor =
                typeof(BabelNameAttribute).GetConstructor(paramTypes);
            CustomAttributeBuilder attrBuilder =
                new CustomAttributeBuilder(constructor,
                                           new object[] { babelName });
            methodBuilder.SetCustomAttribute(attrBuilder);
            Attribute attr = new BabelNameAttribute(babelName);
            AddCustomAttribute(methodBuilder, attr);
        }

        public virtual string GetBabelName(ICustomAttributeProvider provider)
        {
            object[] attrs =
                GetCustomAttributes(provider, typeof(BabelNameAttribute));
            if (attrs == null || attrs.Length == 0)
                return null;
            else
                return ((BabelNameAttribute) attrs[0]).Name;
        }

        public void AddIterReturnType(MethodBuilder methodBuilder,
                                      TypeData returnType)
        {
            Type[] paramTypes = new Type[] { typeof(Type) };
            ConstructorInfo constructor =
                typeof(IterReturnTypeAttribute).GetConstructor(paramTypes);
            CustomAttributeBuilder attrBuilder =
                new CustomAttributeBuilder(constructor,
                                           new object[] { returnType.RawType });
            methodBuilder.SetCustomAttribute(attrBuilder);
            Attribute attr =
                new IterReturnTypeAttribute(returnType.RawType);
            AddCustomAttribute(methodBuilder, attr);
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

        public void AddSupertypingAdapter(TypeBuilder typeBuilder,
                                      TypeData adapteeType,
                                      Type adapterType)
        {
            Type[] paramTypes = new Type[] { typeof(Type), typeof(Type) };
            ConstructorInfo constructor =
                typeof(SupertypingAdapterAttribute).GetConstructor(paramTypes);
            object[] parameters = new object [] {
                adapteeType.RawType,
                adapterType
            };
            CustomAttributeBuilder attrBuilder =
                new CustomAttributeBuilder(constructor, parameters);
            typeBuilder.SetCustomAttribute(attrBuilder);
            Attribute attr =
                new SupertypingAdapterAttribute(adapteeType.RawType,
                                                adapterType);
            AddCustomAttribute(typeBuilder, attr);
        }

        public virtual Type GetSupertypingAdapter(Type type, Type subtype)
        {
            object[] attrs =
                GetCustomAttributes(type, typeof(SupertypingAdapterAttribute));
            if (attrs == null)
                return null;
            foreach (SupertypingAdapterAttribute attr in attrs) {
                if (attr.AdapteeType == subtype)
                    return attr.AdapterType;
            }
            return null;
        }

        public virtual string GetMethodName(MethodInfo method)
        {
            string name = GetBabelName(method);
            if (name == null)
                return method.Name;
            else 
                return name;
        }

        public virtual TypeData GetReturnType(MethodInfo method)
        {
            Type returnType = GetIterReturnType(method);
            if (returnType == null)
                return GetTypeData(method.ReturnType);
            else 
                return GetTypeData(returnType);
        }

        public virtual string GetMethodInfo(TypeData receiverType,
                                            string name,
                                            TypedNodeList arguments,
                                            TypeData returnType)
        {
            string methodInfo = receiverType.FullName + "::" + name;
            if (arguments.Length > 0) {
                methodInfo += "(";
                foreach (TypedNode arg in arguments) {
                    if (arg != arguments.First)
                        methodInfo += ",";
                    methodInfo += arg.NodeType.FullName;
                }
                methodInfo += ")";
            }
            if (returnType != null && !returnType.IsVoid)
                methodInfo += ":" + returnType.Name;
            return methodInfo;
        }

        public virtual string GetMethodInfo(MethodInfo method)
        {
            string methodInfo = GetTypeName(method.DeclaringType);
            string name = GetBabelName(method);
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
            TypeData returnType = GetReturnType(method);
            if (!returnType.IsVoid)
                methodInfo += ":" + returnType.Name;
            return methodInfo;
        }

        public virtual ArrayList GetAncestorMethods(TypeData type)
        {
            ArrayList ancestors = type.Ancestors;
            ArrayList result = new ArrayList();
            foreach (TypeData ancestor in ancestors) {
                MethodInfo[] methods = GetMethods(ancestor);
                result.AddRange(methods);
            }
            return result;
        }
    }
}
