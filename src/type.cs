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

using Babel.Sather.Base;

namespace Babel.Sather.Compiler {
    public class TypeData {
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
            get {
                return rawType.Name;
            }
        }
        
        public virtual string FullName {
            get {
                return rawType.FullName;
            }
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
            Type[] ancestors = typeManager.GetAncestors(rawType);
            if (((IList) ancestors).Contains(supertype.RawType))
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
    }

    public class BuiltinTypeData : TypeData {
        protected string name;

        public BuiltinTypeData(TypeManager typeManager,
                               Type rawType, string name)
            : base(typeManager, rawType)
        {
            this.name = name;
        }
        
        public override string Name
        {
            get {
                return name;
            }
        }
        
        public override string FullName
        {
            get {
                return name;
            }
        }
    }

    public class PredefinedTypeData : TypeData {
        public PredefinedTypeData(TypeManager typeManager, Type rawType)
            : base(typeManager, rawType)
        {
        }
    }

    public class UserDefinedTypeData : TypeData {
        public UserDefinedTypeData(TypeManager typeManager,
                                   TypeBuilder typeBuilder)
            : base(typeManager, typeBuilder)
        {
        }
    }
}
