using System;

namespace CompanyNine.Voxel
{
    public sealed class ObjNames
    {
        public static readonly ObjNames World = Of("World");
        public static readonly ObjNames MainCamera = Of("Main Camera");
        public String Name { get; }
        private ObjNames(string name)
        {
            Name = name;
        }

        private static ObjNames Of(string name)
        {
            return new ObjNames(name);
        }
    }
}