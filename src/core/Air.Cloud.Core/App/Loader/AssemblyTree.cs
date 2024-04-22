/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using System.Reflection;

namespace Air.Cloud.Core.App.Loader
{
    /// <summary>
    /// <para>zh-cn: 程序集加载树,实验性功能</para>
    /// <para>en-us: Assembly loading tree, experimental function</para>
    /// </summary>
    [Obsolete("实验性功能/Experimental Function")]
    public class AssemblyTree
    {
        /// <summary>
        /// zh-cn:当前节点的程序集信息
        /// en-us: Current node assembly information
        /// </summary>
        public AssemblyName Assembly;
        /// <summary>
        /// zh-cn:当前节点的依赖程序集信息
        /// en-us: Dependency assembly information of the current node
        /// </summary>
        public IList<AssemblyTree> ChildrenAssemblies;
        /// <summary>
        /// zh-cn:依赖当前节点的程序集信息
        /// en-us: Assembly information that depends on the current node
        /// </summary>
        public IList<AssemblyTree> ParentAssemblies;
    }

    /// <summary>
    /// zh-cn: 程序集树扩展
    /// en-us: Assembly tree extension
    /// </summary>
    [Obsolete("实验性功能/Experimental Function")]
    public static class AssemblyTreeExtensions
    {
        /// <summary>
        /// <para>zh-cn: 程序集树排序</para>
        /// <para>en-us: Assembly tree sorting</para>
        /// </summary>
        /// <param name="tree">
        /// zh-cn: 程序集树
        /// en-us: Assembly tree
        /// </param>
        /// <param name="NodeDependency">
        /// <para>zh-cn:程序集依赖程序集信息</para>
        /// <para>en-us:assembly dependent assembly information</para>
        /// </param>
        /// <param name="Used">
        /// zh-cn: 当前程序集是否已经被其他程序集引用,如果引用了则不再向下查找
        /// en-us: Whether the current assembly has been referenced by other assemblies, if it has been referenced, it will not be searched down
        /// </param>
        /// <returns></returns>
        public static AssemblyTree Order(AssemblyTree tree, IDictionary<string, List<AssemblyName>> NodeDependency = null, List<AssemblyName> Used = null)
        {
            Console.WriteLine("current order Assembly information:" + tree.Assembly.Name);
            //一开始为空的 初始化
            if (NodeDependency == null) NodeDependency = new Dictionary<string, List<AssemblyName>>();
            if (Used == null) Used = new List<AssemblyName>();
            //当前程序集名称
            var AssemblyName = tree.Assembly.Name;
            //如果为空 则返回tree
            if (AssemblyName == null) return tree;
            if (Used.Any(s => s.Name == AssemblyName)) return tree;
            //依赖当前程序集的程序集信息
            List<AssemblyName> CurrentChildAssemblies = new List<AssemblyName>();
            if (NodeDependency.Keys.Contains(AssemblyName))
            {
                NodeDependency.TryGetValue(AssemblyName, out CurrentChildAssemblies);
                CurrentChildAssemblies.AddRange(tree.ChildrenAssemblies.Select(s => s.Assembly).ToList());
                Used.AddRange(tree.ChildrenAssemblies.Select(s => s.Assembly));
                NodeDependency[AssemblyName] = CurrentChildAssemblies;
            }
            else
            {
                CurrentChildAssemblies.AddRange(tree.ChildrenAssemblies.Select(s => s.Assembly).ToList());
                Used.AddRange(tree.ChildrenAssemblies.Select(s => s.Assembly));
                NodeDependency.Add(AssemblyName, CurrentChildAssemblies);
            }
            foreach (var item in tree.ChildrenAssemblies)
            {
                if (item.ParentAssemblies == null) item.ParentAssemblies = new List<AssemblyTree>();
                item.ParentAssemblies.Add(tree);
                //A    BCDEF
                Order(item, NodeDependency, Used);
            }
            return tree;
        }


        /// <summary>
        /// <para>zh-cn:加载程序集依赖树信息</para>
        /// <para>en-us:Load assembly dependency tree information</para>
        /// </summary>
        /// <param name="assembly">
        /// <para>zh-cn:父级程序集信息</para>
        /// <para>en-us:Parent assembly information</para>
        /// </param>
        /// <param name="processedAssemblies">
        /// <para>zh-cn:已处理的程序集</para>
        /// <para>en-us:Processed assembly</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:程序集依赖树</para>
        /// <para>en-us:Assembly dependency tree</para>
        /// </returns>
        public static AssemblyTree LoadTree(AssemblyName assembly, HashSet<string> processedAssemblies = null)
        {
            processedAssemblies ??= new HashSet<string>();
            AssemblyTree tree = new AssemblyTree();
            tree.Assembly = assembly;
            tree.ChildrenAssemblies = new List<AssemblyTree>();
            if (processedAssemblies.Contains(assembly.Name)) return null;
            processedAssemblies.Add(assembly.Name);
            //this code maybe has some performance question because we used assembly load method to load assembly,this method will load assembly to memory and will not unload assembly from memory
            var Dependencies = Assembly.Load(assembly.Name).GetReferencedAssemblies().Where(s => AppCore.Assemblies.Select(ss => ss.Name).Contains(s.Name));
            //we will scanning all assembly in this project and get all assembly reference information
            Dependencies.ToList().ForEach(s =>
            {
                var childTree = LoadTree(s, processedAssemblies);
                if (childTree != null)
                {
                    tree.ChildrenAssemblies.Add(childTree);
                    processedAssemblies.Add(childTree.Assembly.Name);
                }
            });
            tree.ParentAssemblies = new List<AssemblyTree>();
            return tree;
        }
        /// <summary>
        /// zh-cn: 调整树的顺序
        /// en-us: Adjust the order of the tree
        /// </summary>
        /// <param name="tree">
        /// <para>zh-cn:待调整</para>
        /// <para>en-us:To be adjusted</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:调整结果</para>
        /// <para>en-us:Adjustment result</para>
        /// </returns>
        public static AssemblyTree OrderTree(this AssemblyTree tree)
        {
            IDictionary<string, List<AssemblyName>> NodeDependency = new Dictionary<string, List<AssemblyName>>();
            List<AssemblyName> UsedDenpendency = new List<AssemblyName>();
            tree = Order(tree, NodeDependency, UsedDenpendency);
            return tree;
        }
    }
}
