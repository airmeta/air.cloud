using System.Reflection;

namespace Air.Cloud.Core.Modules.AppAssembly.Builder
{
    public class AppAssemblyBuilderFactory
    {
        public static IAppAssemblyBuilder Create(string AssemblyFilePath)
        {
            var BuilderNew = new AppAssemblyBuilder();
            BuilderNew.AssemblyFilePath = AssemblyFilePath;
            BuilderNew.InitializeAssemblyLoadContext();
            IAppAssemblyBuilder.AppAssemblyBuilderPool.Set(BuilderNew);
            return BuilderNew;
        }
        public static IAppAssemblyBuilder Get(AssemblyName Name)
        {
            var Builder= IAppAssemblyBuilder.AppAssemblyBuilderPool.Get(Name.Name);
            if (Builder == null)
            {
                Builder = new AppAssemblyBuilder();
                Builder.MainAssemblyName = Name;
                IAppAssemblyBuilder.AppAssemblyBuilderPool.Set(Builder);
            }
            return Builder;
        }
    }
}
