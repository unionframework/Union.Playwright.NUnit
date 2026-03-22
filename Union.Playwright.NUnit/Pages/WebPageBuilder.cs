using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Union.Playwright.NUnit.Attributes;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Pages
{
    public static class WebPageBuilder
    {
        public static void InitPage(UnionPage page)
        {
            ArgumentNullException.ThrowIfNull(page);
            InitComponents(page, page);
        }

        /// <summary>
        /// Initializes [UnionInit] fields on a component that was created outside of page activation,
        /// such as items created by ListBase.
        /// </summary>
        public static void InitComponent(IUnionPage page, object component)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (component == null) return;
            InitComponents(page, component);
        }

        private static void InitComponents(IUnionPage page, object componentContainer)
        {
            var type = componentContainer.GetType();
            var members = GetInitializableMembers(type);

            foreach (var (member, attribute) in members)
            {
                var componentType = GetMemberType(member);
                var component = CreateComponent(page, componentContainer, componentType, attribute);

                SetMemberValue(componentContainer, member, component);

                if (component is ComponentBase cb)
                {
                    cb.ComponentName = attribute.ComponentName ?? member.Name;
                    cb.FrameXcss = attribute.FrameXcss;
                }

                page.RegisterComponent((IComponent)component);

                InitComponents(page, component);
            }
        }

        public static object CreateComponent(IUnionPage page, object componentContainer, Type type, UnionInit attribute)
        {
            var args = typeof(ItemBase).IsAssignableFrom(type)
                ? new List<object> { componentContainer }
                : new List<object> { page };

            if (attribute.Args is { Length: > 0 })
            {
                var container = componentContainer as IContainer;
                var processedArgs = new object[attribute.Args.Length];
                Array.Copy(attribute.Args, processedArgs, attribute.Args.Length);

                if (container != null)
                {
                    for (var i = 0; i < processedArgs.Length; i++)
                    {
                        processedArgs[i] = CreateInnerSelector(container, processedArgs[i]);
                    }
                }

                args.AddRange(processedArgs);
            }

            return Activator.CreateInstance(type, args.ToArray());
        }

        private static object CreateInnerSelector(IContainer container, object argument)
        {
            if (argument is string str && str.StartsWith("root:"))
            {
                return container.InnerScss(str.Replace("root:", string.Empty));
            }
            return argument;
        }

        public static List<T> CreateItems<T>(IContainer container, IEnumerable<string> ids)
            where T : ItemBase
        {
            return ids.Select(id => (T)Activator.CreateInstance(typeof(T), container, id)).ToList();
        }

        public static T CreateComponent<T>(IContainer container, params object[] additionalArgs)
            where T : IComponent
        {
            return CreateComponent<T>(container.ParentPage, container, additionalArgs);
        }

        public static T CreateComponent<T>(IUnionPage page, params object[] additionalArgs)
            where T : IComponent
        {
            return CreateComponent<T>(page, null, additionalArgs);
        }

        public static T CreateComponent<T>(IUnionPage page, object componentContainer, params object[] additionalArgs)
            where T : IComponent
        {
            var attr = new UnionInit(additionalArgs);
            var component = (T)CreateComponent(page, componentContainer ?? page, typeof(T), attr);
            InitComponents(page, component);
            return component;
        }

        public static IComponent CreateComponent(IUnionPage page, Type type, params object[] additionalArgs)
        {
            var attr = new UnionInit(additionalArgs);
            var component = (IComponent)CreateComponent(page, page, type, attr);
            InitComponents(page, component);
            return component;
        }

        internal static T CreateComponent<T>(UnionPage pageBase, object[] args) where T : IComponent
        {
            return CreateComponent<T>((IUnionPage)pageBase, args);
        }

        private static List<(MemberInfo Member, UnionInit Attribute)> GetInitializableMembers(Type type)
        {
            var result = new List<(MemberInfo, UnionInit)>();

            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<UnionInit>();
                if (attr != null)
                {
                    result.Add((field, attr));
                }
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<UnionInit>();
                if (attr != null)
                {
                    result.Add((prop, attr));
                }
            }

            return result;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo fi => fi.FieldType,
                PropertyInfo pi => pi.PropertyType,
                _ => throw new NotSupportedException($"Unsupported member type: {member.GetType().Name}")
            };
        }

        private static void SetMemberValue(object target, MemberInfo member, object value)
        {
            switch (member)
            {
                case FieldInfo fi:
                    fi.SetValue(target, value);
                    break;
                case PropertyInfo pi:
                    pi.SetValue(target, value);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member type: {member.GetType().Name}");
            }
        }
    }
}
