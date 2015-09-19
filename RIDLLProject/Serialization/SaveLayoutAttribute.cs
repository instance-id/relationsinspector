using System;

namespace RelationsInspector
{
    [AttributeUsage(AttributeTargets.Class)]
    class SaveLayoutAttribute : Attribute
    {
        public bool doSave;

        public SaveLayoutAttribute()
        {
            this.doSave = true;
        }

        public SaveLayoutAttribute(bool doSave)
        {
            this.doSave = doSave;
        }
    }
}
