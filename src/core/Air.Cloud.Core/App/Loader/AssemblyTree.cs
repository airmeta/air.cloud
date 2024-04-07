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
    public class AssemblyTree
    {
        /// <summary>
        /// 当前节点的程序集信息
        /// </summary>
        public AssemblyName Assembly;
        /// <summary>
        /// 当前节点的依赖程序集信息
        /// </summary>
        public IList<AssemblyTree> ChildrenAssemblies;
        /// <summary>
        /// 依赖当前节点的程序集信息
        /// </summary>
        public IList<AssemblyTree> ParentAssemblies;
    }

    public static class AssemblyTreeExtensions
    {
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
        /// 加载程序集依赖树信息
        /// </summary>
        /// <param name="assembly">父程序集信息</param>
        /// <returns></returns>
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

        public static AssemblyTree OrderTree(this AssemblyTree tree)
        {
            //这个tree 就已经是依赖过的程序集信息
            //数据处理流程解释:
            //开始向下遍历检查 检查该程序集的依赖项是否被其他类库依赖
            //这里最子节点一定是Air.Cloud.Core类库
            //也就是说 所有的模组,增强,插件都是依赖于Air.Cloud.Core
            //这个时候 可以剔除掉非模组,增强,插件,用户业务的程序集,然后进行倒转过来
            //取到的结果 都是直接依赖或者间接依赖Air.Cloud.Core的程序集 
            //这个时候的依赖可能还是多条重复的线  这个时候 需要对树进行节点优化 保证顺序 平级从左到右分别调用
            /*          1
             *       2    a
             *     a  b  3  4
             *     
             */
            IDictionary<string, List<AssemblyName>> NodeDependency = new Dictionary<string, List<AssemblyName>>();
            List<AssemblyName> UsedDenpendency = new List<AssemblyName>();
            tree = Order(tree, NodeDependency, UsedDenpendency);
            return tree;
        }

        public static AssemblyTree Reverse(this AssemblyTree tree)
        {
            var AssemblyOrder = new List<AssemblyName>();
            //第一个没有再被依赖了 即使有依赖也解除依赖
            tree.ParentAssemblies = new List<AssemblyTree>();
            //加载主应用程序(保证用户的配置为第一位)
            AssemblyOrder.Add(tree.Assembly);
            //加载依赖应用程序 要排除根类库
            var BaseAssembly = Assembly.GetExecutingAssembly();
            //倒转后的tree
            var ReverseTree = new AssemblyTree();
            return ReverseTree;
        }
    }
}
