using Air.Cloud.Core.Extensions.IEnumerables;

using System.Reflection;

public static class AssemblyExtensions
{
    //[Aspect(typeof(IfException))]
    public static Type[] LoadTypes(this Assembly assembly,Func<Type,bool> CheckTypeAction)
    {
        return assembly.GetTypes().Where(CheckTypeAction).ToArray();
    }

}