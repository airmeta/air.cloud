using Air.Cloud.Core.Extensions.Aspect;
using Air.Cloud.Core.Extensions.IEnumerables;
using Air.Cloud.Core.Modules.AppAspect.Attributes;

using System.Reflection;

public static class AssemblyExtensions
{
    //[Aspect(typeof(IfException))]
    public static Type[] LoadTypes(this Assembly assembly,Func<Type,bool> CheckTypeAction)
    {
        return assembly.GetTypes().Where(CheckTypeAction).ToArray();
    }

}