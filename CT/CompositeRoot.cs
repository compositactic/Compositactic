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

using CT.Hosting;
using CT.Hosting.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace CT
{
    [DataContract]
    [KeyProperty(nameof(Id))]
    [ParentProperty(nameof(ActiveCompositeRoots))]
    public abstract class CompositeRoot : Composite, IDisposable
    {
        private static readonly BindingFlags _constructorBindingFlags = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        protected CompositeRoot()
        {
            Id = Guid.NewGuid().ToString();
            var assemblies = GetServiceAssemblyNames().Select(ca => Assembly.Load(ca.FullName));
            InitializeServices(assemblies);
        }

        protected CompositeRoot(IEnumerable<Assembly> serviceAssemblies)
        {
            Id = Guid.NewGuid().ToString();
            InitializeServices(serviceAssemblies);
        }

        protected CompositeRoot(params IService[] services)
        {
            Id = Guid.NewGuid().ToString();
            _services = new Collection<IService>(services.ToList());
            SetCompositeRoots();
        }

        protected CompositeRoot(CompositeRootConfiguration configuration)
        {
            Id = Guid.NewGuid().ToString();
            Configuration = configuration;
            var assemblies = GetServiceAssemblyNames().Select(ca => Assembly.Load(ca.FullName));
            InitializeServices(assemblies);
        }

        protected CompositeRoot(CompositeRootConfiguration configuration, IEnumerable<Assembly> serviceAssemblies)
        {
            Id = Guid.NewGuid().ToString();
            Configuration = configuration;
            InitializeServices(serviceAssemblies);
        }

        protected CompositeRoot(CompositeRootConfiguration configuration, params IService[] services)
        {
            Id = Guid.NewGuid().ToString();
            Configuration = configuration;
            _services = new Collection<IService>(services.ToList());
            SetCompositeRoots();
        }

        internal static CompositeRoot Create(CompositeRootContainer activeCompositeRoots, CompositeRootConfiguration configuration, EventHandler eventHandler, IEnumerable<Assembly> serviceAssemblies)
        {
            var compositeRoot = serviceAssemblies == null ? (CompositeRoot)Activator.CreateInstance(configuration.CompositeRootType, _constructorBindingFlags, null, new object[] { configuration }, CultureInfo.InvariantCulture) : 
                                                            (CompositeRoot)Activator.CreateInstance(configuration.CompositeRootType, _constructorBindingFlags, null, new object[] { configuration, serviceAssemblies }, CultureInfo.InvariantCulture);
            compositeRoot.Authenticator = Activator.CreateInstance(configuration.CompositeRootAuthenticatorType, _constructorBindingFlags, null, null, CultureInfo.InvariantCulture) as CompositeRootAuthenticator;
            compositeRoot.Configuration = configuration;
            compositeRoot.EventAdded += eventHandler;

            return compositeRoot;
        }

        internal static CompositeRoot Create(CompositeRootContainer activeCompositeRoots, CompositeRootConfiguration configuration, EventHandler eventHandler, IEnumerable<IService> services)
        {
            var compositeRoot = services == null ? (CompositeRoot)Activator.CreateInstance(configuration.CompositeRootType, _constructorBindingFlags, null, new object[] { configuration }, CultureInfo.InvariantCulture) :
                                                    (CompositeRoot)Activator.CreateInstance(configuration.CompositeRootType, _constructorBindingFlags, null, new object[] { configuration, services }, CultureInfo.InvariantCulture);
            compositeRoot.Authenticator = Activator.CreateInstance(configuration.CompositeRootAuthenticatorType) as CompositeRootAuthenticator;
            compositeRoot.Configuration = configuration;
            compositeRoot.EventAdded += eventHandler;

            return compositeRoot;
        }
        
        [DataMember]
        public string Id { get; internal set; }

        public CompositeRootConfiguration Configuration { get; internal set; }
        public CompositeRootAuthenticator Authenticator { get; internal set; }
        public CompositeRootSession CompositeRootSession { get; internal set; }

        public CompositeRootContainer ActiveCompositeRoots { get; internal set; }

        private static IEnumerable<AssemblyName> GetServiceAssemblyNames()
        {
            var serviceAssemblyNames = new Collection<AssemblyName>();
            var dirInfo = new DirectoryInfo(Environment.CurrentDirectory);
            foreach (var file in dirInfo.EnumerateFiles().Where(f => f.Extension.ToUpperInvariant() == ".DLL"))
            {
                try
                {
                    serviceAssemblyNames.Add(AssemblyName.GetAssemblyName(file.FullName));
                }
                catch (BadImageFormatException)
                {
                    continue;
                }
            }

            return serviceAssemblyNames;
        }

        private Collection<IService> _services;
        protected void InitializeServices(IEnumerable<Assembly> assemblies)
        {
            _services = new Collection<IService>(
                    assemblies
                        .SelectMany(assembly => assembly.GetTypes()
                        .Where(t => t.GetInterface(nameof(IService)) != null && t.IsClass && !t.IsAbstract))
                            .Select(serviceType => (IService)serviceType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null).Invoke(null))
                            .ToList());

            SetCompositeRoots();
        }

        private void SetCompositeRoots()
        {
            foreach (var service in _services)
                service.CompositeRoot = this;
        }

        public TService GetService<TService>() where TService : IService
        {
            return (TService)_services.Single(service => service is TService);
        }

        public virtual void DoCompositeRootLogOffTask()
        {
            DoAddEvent(new CompositeEvent(CompositeEventType.LogOff, string.Empty, CompositeRoot?.Configuration?.Id.ToString(), CompositeRoot?.CompositeRootSession?.Token,
                CompositeRoot != null && CompositeRoot.CompositeRootSession != null ?
                                            CompositeRoot.CompositeRootSession.Mode :
                                            CompositeRootMode.None));
        }

        private EventHandler _eventAdded;
        public event EventHandler EventAdded
        {
            add { _eventAdded += value; }
            remove { _eventAdded -= value; }
        }

        internal void DoAddEvent(CompositeEvent newEvent)
        {
            if (newEvent == null)
                throw new ArgumentNullException(nameof(newEvent));

            _eventAdded?.Invoke(newEvent, new EventArgs());
        }

        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
            }

            disposed = true;
        }

        ~CompositeRoot()
        {
            Dispose(false);
        }
    }
}

