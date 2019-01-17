// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using CT.Hosting;
using CT.Properties;
using System.Globalization;
using System.Collections.Concurrent;

namespace CT
{
    [DataContract]
    public abstract class Composite : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler _propertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }
        
        public CompositeState State { get; set; } = CompositeState.Unchanged;

        private readonly ConcurrentDictionary<string, object> _modifiedProperties = new ConcurrentDictionary<string, object>();
        public IReadOnlyDictionary<string, object> ModifiedProperties { get { return _modifiedProperties; } }

        public void ResetState()
        {
            _modifiedProperties.Clear();
            State = CompositeState.Unchanged;
        }

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var property = GetType().GetProperty(propertyName);
            if (property == null)
                throw new ArgumentException(propertyName);

            var propertyValue = property.GetValue(this);

            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            CompositeRoot?.DoAddEvent(new CompositeEvent(CompositeEventType.Change, this.GetPropertyPath(property),
                                        propertyValue,
                                        CompositeRoot?.CompositeRootSession?.Token,
                                        CompositeRoot != null && CompositeRoot.CompositeRootSession != null ?
                                            CompositeRoot.CompositeRootSession.Mode :
                                            CompositeRootMode.None));

            if (_modifiedProperties.ContainsKey(propertyName))
                _modifiedProperties[propertyName] = propertyValue;
            else
                _modifiedProperties.TryAdd(propertyName, propertyValue);

            State = CompositeState.Modified;
        }

        protected internal void AddEvent(CompositeEventType eventType, string path, object eventData)
        {
            CompositeRoot?.DoAddEvent(new CompositeEvent(eventType, path, eventData, CompositeRoot?.CompositeRootSession?.Token,
                CompositeRoot != null && CompositeRoot.CompositeRootSession != null ?
                                            CompositeRoot.CompositeRootSession.Mode :
                                            CompositeRootMode.None));
        }

        [DataMember]
        [Help(typeof(Resources), nameof(Resources.CompositePathToComposite))]
        public string Path { get { return this.GetPath(); } }

        protected CompositeRoot CompositeRoot
        {
            get { return GetParentComposite(this, null, null) as CompositeRoot; }
        }

        protected Composite GetParentComposite(Type parentCompositeType, string parentPropertyName)
        {
            if (parentCompositeType == null && !string.IsNullOrEmpty(parentPropertyName) ||
                parentCompositeType != null && string.IsNullOrEmpty(parentPropertyName))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustSpecifyBothNonNullOrBothNull, nameof(parentCompositeType), nameof(parentPropertyName)));

            return GetParentComposite(this, parentCompositeType, parentPropertyName);
        }

        private Composite GetParentComposite(Composite composite, Type parentCompositeType, string parentPropertyName)
        {
            if (composite == null)
                return null;

            if (composite is CompositeRoot)
                return composite as CompositeRoot;
            else
            {
                var parentPropertyAttribute = composite.GetType().FindCustomAttribute<ParentPropertyAttribute>();
                if (parentPropertyAttribute == null)
                    return null;

                var parentPropertyInfo = composite.GetType().GetProperty(parentPropertyAttribute.ParentPropertyName);
                var parentComposite = parentPropertyInfo.GetValue(composite) as Composite;
                if (parentPropertyAttribute.ParentPropertyName == parentPropertyName && parentPropertyInfo.PropertyType == parentCompositeType)
                    return parentComposite;

                return GetParentComposite(parentComposite, parentCompositeType, parentPropertyName);
            }
        }

        private readonly BindingFlags _flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.InvokeMethod;

        internal CompositeMemberInfo GetCompositeMemberInfo()
        {
            var compositeMemberInfos = 
                GetType()
                .GetMembers(_flags)
                .Where(mi => mi.GetCustomAttribute<DataMemberAttribute>() != null || mi.GetCustomAttribute<CommandAttribute>() != null);

            var compositePropertyInfos = new List<CompositePropertyInfo>();

            compositeMemberInfos
                .Where(mi => mi.MemberType == MemberTypes.Property)
                .Cast<PropertyInfo>().ToList()
                .ForEach(pi => compositePropertyInfos.Add(new CompositePropertyInfo(pi.Name, pi.PropertyType, pi.GetSetMethod(false) == null, pi.GetCustomAttribute<HelpAttribute>()?.Text)));

            var compositeCommandInfos = new List<CompositeCommandInfo>();
            var compositeCommandParameterInfos = new List<CompositeCommandParameterInfo>();
            foreach(var cmi in compositeMemberInfos.Where(mi => mi.MemberType == MemberTypes.Method).Cast<MethodInfo>())
            {
                foreach (var parameterInfo in cmi.GetParameters().Where(pi => pi.ParameterType != typeof(CompositeRootHttpContext)))
                    compositeCommandParameterInfos.Add(new CompositeCommandParameterInfo(parameterInfo.Name, parameterInfo.ParameterType, parameterInfo.GetCustomAttribute<HelpAttribute>()?.Text, parameterInfo.ParameterType.GetTypeEnumValues()));

                var returnValueHelpText = cmi.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(HelpAttribute), true).Cast<HelpAttribute>().FirstOrDefault()?.Text;

                compositeCommandInfos.Add(new CompositeCommandInfo(cmi.Name, cmi.GetCustomAttribute<HelpAttribute>()?.Text, compositeCommandParameterInfos, cmi.ReturnType, returnValueHelpText));
            }

            return new CompositeMemberInfo(compositePropertyInfos, compositeCommandInfos);
        }
    }
}
