/*
 * type.cs: Sather types
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
    public abstract class TypeData {
        protected TypeManager typeManager;
        protected Type rawType;
        protected ArrayList parents;
        protected ArrayList ancestors;

        public TypeData(TypeManager typeManager, Type rawType)
        {
            this.typeManager = typeManager;
            this.rawType = rawType;
        }

        public virtual Type RawType {
            get { return rawType; }
            set { rawType = value; }
        }
        
        public virtual string Name {
            get { return rawType.Name; }
        }
        
        public virtual string FullName {
            get { return rawType.FullName.Replace(".", "::"); }
        }

        public virtual bool IsVoid {
            get { return rawType == typeof(void); }
        }

        public virtual bool IsSubtypeOf(TypeData supertype)
        {
            if (supertype == null)
                return false;
            if (supertype.RawType == rawType)
                return true;
            if (supertype.RawType == typeof(object))
                return true;
            if (Ancestors.Contains(supertype))
                return true;
            Type adapter =
                typeManager.GetSupertypingAdapter(supertype.RawType, rawType);
            return adapter != null;
        }

        public virtual bool IsAbstract {
            get { return rawType.IsInterface; }
        }

        public virtual ArrayList Parents {
            get {
                if (parents == null) {
                    Type[] ifaces = RawType.GetInterfaces();
                    parents = new ArrayList();
                    foreach (Type iface in ifaces) {
                        parents.Add(typeManager.GetTypeData(iface));
                    }
                }
                return parents;
            }

            set
            {
                parents = value;
            }
        }

        public virtual ArrayList Ancestors {
            get {
                if (ancestors == null)
                    ancestors = ExtractAncestors(Parents);
                return ancestors;
            }
        }

        public virtual ArrayList ExtractAncestors(ArrayList parents)
        {
            ArrayList ancestors = new ArrayList();
            foreach (TypeData parent in parents) {
                foreach (TypeData anc in parent.Ancestors) {
                    if (!ancestors.Contains(anc))
                        ancestors.Add(anc);
                }
                if (!ancestors.Contains(parent))
                    ancestors.Add(parent);
            }
            return ancestors;
        }

        public virtual Type[] ParentRawTypes
        {
            get { return GetRawTypes(Parents); }
        }

        public virtual Type[] AncestorRawTypes
        {
            get { return GetRawTypes(Parents); }
        }

        protected virtual Type[] GetRawTypes(ArrayList types)
        {
            Type[] rawTypes = new Type[types.Count];
            for (int i = 0; i < rawTypes.Length; i++) {
                rawTypes[i] = ((TypeData) types[i]).RawType;
            }
            return rawTypes;
        }

        public virtual bool IsValueType {
            get {
                return rawType.IsValueType;
            }
        }

        public virtual bool IsByRef {
            get {
                return rawType.IsByRef;
            }
        }

        public virtual TypeData ElementType {
            get {
                return typeManager.GetTypeData(rawType.GetElementType());
            }
        }

        public virtual TypeData ReferenceType {
            get {
                string refTypeName = rawType.FullName + "&";
                return typeManager.GetType(refTypeName);
            }
        }

        public abstract ArrayList Methods {
            get;
        }

        public virtual ArrayList AncestorMethods {
            get {
                ArrayList result = new ArrayList();
                foreach (TypeData ancestor in Ancestors) {
                    ArrayList methods = ancestor.Methods;
                    result.AddRange(methods);
                }
                return result;
            }
        }
    }

    public class PredefinedTypeData : TypeData {
        protected ArrayList methods;

        public PredefinedTypeData(TypeManager typeManager, Type rawType)
            : base(typeManager, rawType)
        {
            methods = null;
        }

        public override ArrayList Methods {
            get {
                if (methods == null) {
                    MethodInfo[] methodInfos =
                        rawType.GetMethods(BindingFlags.Instance |
                                           BindingFlags.Static |
                                           BindingFlags.Public |
                                           BindingFlags.NonPublic);
                    methods = new ArrayList();
                    foreach (MethodInfo m in methodInfos) {
                        methods.Add(new PredefinedMethodData(typeManager, m));
                    }
                }
                return methods;
            }
        }
    }

    public class UserDefinedTypeData : TypeData {
        protected ArrayList methods;

        public UserDefinedTypeData(TypeManager typeManager,
                                   TypeBuilder typeBuilder)
            : base(typeManager, typeBuilder)
        {
            methods = new ArrayList();
        }

        public override ArrayList Methods {
            get { return methods; }
        }
    }

    public class BuiltinTypeData : PredefinedTypeData {
        protected string name;
        protected Type builtinMethodContainer;

        public BuiltinTypeData(TypeManager typeManager,
                               Type rawType, string name)
            : base(typeManager, rawType)
        {
            this.name = name.ToUpper();
            this.builtinMethodContainer = null;
        }
        
        public override string Name {
            get { return name; }
        }
        
        public override string FullName {
            get { return name; }
        }
        
        public virtual Type BuiltinMethodContainer {
            get { return builtinMethodContainer; }
            set { builtinMethodContainer = value; }
        }

        public override ArrayList Methods {
            get {
                if (methods == null) {
                    methods = base.Methods;
                    if (builtinMethodContainer != null) {
                        MethodInfo[] methodInfos =
                            builtinMethodContainer.
                            GetMethods(BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.Public |
                                       BindingFlags.NonPublic);
                        foreach (MethodInfo m in methodInfos) {
                            methods.Add(new BuiltinMethodData(typeManager, m));
                        }
                    }
                }
                return methods;
            }
        }
    }

    public class TypeParameterData : TypeData {
        protected TypeData constrainingType;

        public TypeParameterData(TypeManager typeManager,
                                 GenericTypeParameterBuilder typeBuilder,
                                 TypeData constrainingType)
            : base(typeManager, typeBuilder)
        {
            this.constrainingType = constrainingType;
        }

        public override string FullName {
            get { return Name; }
        }

        public override ArrayList Methods {
            get {
                return constrainingType.Methods;
            }
        }
    }
}
