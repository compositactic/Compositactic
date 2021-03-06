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
using CT.Properties;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace CT
{
    public static class ExtensionMethods
    {
        public static void Load<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TValue value, Func<TValue, TKey> keyPropertyValueGenerationFunc)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (keyPropertyValueGenerationFunc == null)
                throw new ArgumentNullException(nameof(keyPropertyValueGenerationFunc));

            var keyProperty = value.GetType().GetProperty(value.GetType().FindCustomAttribute<KeyPropertyAttribute>().PropertyName);

            TKey keyValue;
            var totalCount = dictionary.LongCount();
            var loadTries = 0L;
            var loaded = false;

            while(loadTries <= totalCount)
            {
                keyValue = keyPropertyValueGenerationFunc(value);

                if(!dictionary.ContainsKey(keyValue))
                {
                    keyProperty.SetMethod.Invoke(value, new object[] { keyValue });
                    dictionary.Add(keyValue, value);
                    loaded = true;
                    break;
                }

                loadTries++;
            }

            if (loaded)
                return;
            else
                throw new ArgumentException(Resources.MustReturnAUniqueKeyValue);
        }

        public static void RestoreParentReferences(this object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            RestoreParentReferences(value, value.GetType(), null);
        }

        private const BindingFlags _privateFields = BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance;
        private static void RestoreParentReferences(object @object, Type objectType, object parentObject)
        {
            if (parentObject != null)
            {
                var parentPropertyName = objectType.FindCustomAttribute<ParentPropertyAttribute>()?.ParentPropertyName;
                if (parentPropertyName != null)
                    objectType.GetProperty(parentPropertyName).SetValue(@object, parentObject);
                else
                {
                    if (@object is IDictionary dictionary)
                        foreach (var e in dictionary.Values)
                            RestoreParentReferences(e, e.GetType(), parentObject);
                }
            }
            
            foreach (var field in objectType.GetMembers(_privateFields).Where(f => f.GetCustomAttributes<DataMemberAttribute>(false).Any()))
            {
                var fieldInfo = objectType.GetField(field.Name, _privateFields);
                RestoreParentReferences(fieldInfo.GetValue(@object), fieldInfo.FieldType, @object);
            }
        }

        public static IEnumerable<CompositeRootCommandResponse> Execute(this CompositeRoot compositeRoot, IEnumerable<CompositeRootCommandRequest> commandRequests, CompositeRootHttpContext compositeRootHttpContext, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return ExecuteCommands(compositeRoot, commandRequests, null, compositeRootHttpContext, userName, sessionToken, uploadedFiles);
        }

        public static IEnumerable<CompositeRootCommandResponse> Execute(this CompositeRoot compositeRoot, IEnumerable<CompositeRootCommandRequest> commandRequests, HttpListenerContext context, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return ExecuteCommands(compositeRoot, commandRequests, context, null, userName, sessionToken, uploadedFiles);
        }

        private static IEnumerable<CompositeRootCommandResponse> ExecuteCommands(CompositeRoot compositeRoot, IEnumerable<CompositeRootCommandRequest> commandRequests, HttpListenerContext context, CompositeRootHttpContext compositeRootHttpContext, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            var commandResponses = new List<CompositeRootCommandResponse>();
            var returnValue = new object();
            CompositeRootHttpContext ctContext = null;

            foreach (var commandRequest in commandRequests)
            {
                var commandResponseReturnValuePlaceholderMatches = Regex.Matches(commandRequest.CommandPath, @"{(?'commandId'\d+)/?(?'path'.+?)?}").Cast<Match>();

                foreach (var commandResponseReturnValuePlaceholderMatch in commandResponseReturnValuePlaceholderMatches)
                {
                    var commandResponseReturnValue = commandResponses.Single(cr => cr.Id == int.Parse(commandResponseReturnValuePlaceholderMatch.Groups["commandId"].Value, CultureInfo.InvariantCulture)).ReturnValue;
                    var commandResponseReturnValuePath = commandResponseReturnValuePlaceholderMatch.Groups["path"].Value;
                    var commandResponseReturnValueComposite = commandResponseReturnValue as Composite;
                    var returnValuePlaceholder = commandRequest.CommandPath.Substring(commandResponseReturnValuePlaceholderMatch.Index, commandResponseReturnValuePlaceholderMatch.Length);
                    if (commandResponseReturnValueComposite != null && !string.IsNullOrEmpty(commandResponseReturnValuePath))
                    {
                        var valueForPlaceholder = commandResponseReturnValueComposite.Execute(commandResponseReturnValuePath, context, null, string.Empty, sessionToken, uploadedFiles);
                        if (!valueForPlaceholder.ReturnValue.GetType().IsConvertable())
                            throw new ArgumentException(
                                string.Format(CultureInfo.CurrentCulture, Resources.PlaceholderValueConversionError,
                                                        valueForPlaceholder.ReturnValue.GetType().FullName,
                                                        nameof(TypeConverter),
                                                        nameof(String)));

                        commandRequest.CommandPath = commandRequest.CommandPath.Replace(returnValuePlaceholder, valueForPlaceholder.ReturnValue.ToString());
                    }
                    else if (commandResponseReturnValueComposite != null && string.IsNullOrEmpty(commandResponseReturnValuePath))
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.PlaceholderPathRequiredError, commandResponseReturnValueComposite.GetType().FullName));
                    else
                        commandRequest.CommandPath = commandRequest.CommandPath.Replace(returnValuePlaceholder, commandResponseReturnValue.ToString());
                }

                var commandResponse = context != null ? compositeRoot.Execute(commandRequest.CommandPath, context, userName, sessionToken, uploadedFiles) :
                                                        compositeRoot.Execute(commandRequest.CommandPath, compositeRootHttpContext, userName, sessionToken, uploadedFiles);

                ctContext = commandResponse.Context;
                returnValue = commandResponse.ReturnValue;
                commandResponses.Add(new CompositeRootCommandResponse { Id = commandRequest.Id, Success = true, ReturnValue = returnValue, ReturnValueContentType = ctContext?.Response.ContentType });
            }

            return commandResponses;
        }

        public static CommandResponse Execute(this CompositeRoot compositeRoot, string commandPath, HttpListenerContext context, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return Execute(compositeRoot, commandPath, context, null, userName, sessionToken, uploadedFiles);
        }

        public static CommandResponse Execute(this CompositeRoot compositeRoot, string commandPath, CompositeRootHttpContext compositeRootHttpContext, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return Execute(compositeRoot, commandPath, null, compositeRootHttpContext, userName, sessionToken, uploadedFiles);
        }

        public static CommandResponse Execute(this Composite composite, string commandPath, HttpListenerContext context, CompositeRootHttpContext compositeRootHttpContext,  string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return Execute(composite, new CompositePath(commandPath), 1, context, compositeRootHttpContext, userName, sessionToken, uploadedFiles);
        }

        private static CommandResponse Execute(object composite, CompositePath compositePath, int commandPathSegmentIndex, HttpListenerContext context, CompositeRootHttpContext compositeRootHttpContext, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            if (composite == null && commandPathSegmentIndex == 1)
                throw new ArgumentNullException(nameof(composite));

            if(composite == null && commandPathSegmentIndex == compositePath.Segments.Length)
                return new CommandResponse
                {
                    ReturnValue = null,
                    Context = compositeRootHttpContext != null ? compositeRootHttpContext : (context == null ? null : new CompositeRootHttpContext(context, uploadedFiles, userName, sessionToken))
                };

            if (composite == null && commandPathSegmentIndex < compositePath.Segments.Length)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidPropertyOrCommand, compositePath.Segments[commandPathSegmentIndex]));

            var compositeType = composite.GetType();
            var isDictionary = compositeType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

            string segment;
            MemberInfo member;

            if (commandPathSegmentIndex == compositePath.Segments.Length)
            {
                segment = UnEscape(compositePath.Segments[commandPathSegmentIndex - 1].Trim('/', '\\'));
                member = compositeType.GetMember(segment, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                Match badUrlMatch;

                if (composite is Composite lastComposite && compositePath.PathAndQuery.EndsWith("/?", StringComparison.OrdinalIgnoreCase))
                {
                    return new CommandResponse
                    {
                        ReturnValue = lastComposite.GetCompositeMemberInfo(),
                        Context = context == null ? compositeRootHttpContext : new CompositeRootHttpContext(context, uploadedFiles, userName, sessionToken)
                    };
                }
                else if ((badUrlMatch = Regex.Match(compositePath.PathAndQuery, @"\?.*$")).Success)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidPropertyOrCommand, badUrlMatch.Value));
                }
                else return new CommandResponse
                {
                    ReturnValue = isDictionary ? (compositeType.GetProperty("Values").GetValue(composite) as IEnumerable<object>).ToList() : composite,
                    Context = context == null ? compositeRootHttpContext : new CompositeRootHttpContext(context, uploadedFiles, userName, sessionToken)
                };
            }

            segment = UnEscape(compositePath.Segments[commandPathSegmentIndex].Trim('/', '\\'));

            if (isDictionary)
                composite = ExecuteGetCompositeDictionaryElement(composite, compositeType, segment, context, compositeRootHttpContext);
            else
            {
                member = compositeType.GetMember(segment, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();

                if (member == null)
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidPropertyOrCommand, segment));

                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        var executePropertyResult = ExecuteProperty(ref composite, compositePath, commandPathSegmentIndex, context, compositeRootHttpContext, userName, sessionToken, uploadedFiles, member);
                        if (executePropertyResult != null)
                            return executePropertyResult;
                        break;
                    case MemberTypes.Method:
                        return ExecuteMethod(composite, compositePath, commandPathSegmentIndex, context, compositeRootHttpContext, userName, sessionToken, uploadedFiles, member);
                    default:
                        break;
                }
            }

            return Execute(composite, compositePath, ++commandPathSegmentIndex, context, compositeRootHttpContext, userName, sessionToken, uploadedFiles);
        }

        private static CommandResponse ExecuteProperty(ref object composite, CompositePath compositePath, int commandPathSegmentIndex, HttpListenerContext context, CompositeRootHttpContext compositeRootHttpContext,  string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles, MemberInfo member)
        {
            var memberPropertyInfo = ((PropertyInfo)member);
            if (memberPropertyInfo.PropertyType.IsConvertable() && (commandPathSegmentIndex == compositePath.Segments.Length - 1))
            {
                var propertyValueIsNull = Regex.IsMatch(compositePath.PathAndQuery, @"[^/]\?$");
                if (!string.IsNullOrEmpty(compositePath.Query) || propertyValueIsNull)
                {
                    if (memberPropertyInfo.SetMethod != null && !memberPropertyInfo.SetMethod.IsPublic)
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.PropertyReadOnly, memberPropertyInfo.Name));

                    Match badUrlMatch;
                    if ((badUrlMatch = Regex.Match(compositePath.PathAndQuery, @"/\?.*$")).Success)
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidPropertyOrCommand, badUrlMatch.Value));

                    memberPropertyInfo.SetValue(composite, propertyValueIsNull ? null : TypeDescriptor.GetConverter(memberPropertyInfo.PropertyType).ConvertFrom(null, GetCultureInfo(context != null ? context.Request.UserLanguages : compositeRootHttpContext.Request.UserLanguages.ToArray()), UnEscape(compositePath.Query.Substring(1))));
                    return new CommandResponse
                    {
                        ReturnValue = null,
                        Context = context == null ? compositeRootHttpContext : new CompositeRootHttpContext(context, uploadedFiles, userName, sessionToken)
                    };
                }
                else
                    composite = memberPropertyInfo.GetValue(composite);
            }
            else
                composite = memberPropertyInfo.GetValue(composite);

            return null;
        }

        private static CommandResponse ExecuteMethod(object composite, CompositePath compositePath, int commandPathSegmentIndex, HttpListenerContext context, CompositeRootHttpContext compositeRootHttpContext, string userName, string sessionToken, IEnumerable<CompositeUploadedFile> uploadedFiles, MemberInfo member)
        {
            if (commandPathSegmentIndex != compositePath.Segments.Length - 1)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidPropertyOrCommand, compositePath.Segments[commandPathSegmentIndex + 1]));

            var queryStringParameterNames = !string.IsNullOrEmpty(compositePath.Query) ? compositePath.Query.Substring(1).Split('&').Select(p => p.Split('=')[0].ToLowerInvariant()) : new string[] { };
            var overloadMethod = composite.GetType().GetMethods().Where(m => m.Name == member.Name &&
                                                                            m.GetBaseDefinition().GetCustomAttributes(true).Cast<Attribute>().Any(a => a is CommandAttribute) &&
                                                                            new HashSet<string>(queryStringParameterNames).SetEquals(m.GetParameters().Select(p => p.Name.ToLowerInvariant()))).FirstOrDefault();
            var memberMethodInfo = member.Name == nameof(CompositeRootAuthenticator.LogOn) ? (MethodInfo)member : (overloadMethod ?? (MethodInfo)member);

            if (!memberMethodInfo.GetBaseDefinition().GetCustomAttributes(true).Cast<Attribute>().Any(a => a is CommandAttribute))
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.MissingCommandAttribute, memberMethodInfo.Name));

            if (!memberMethodInfo.IsPublic)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.CommandNotPublic, memberMethodInfo.Name));

            var methodParameters = memberMethodInfo.GetParameters();

            object[] parameterValues = new object[methodParameters.Count()];
            int parameterValuesIndex = 0;

            CompositeRootHttpContext ctContext = null;

            foreach (var methodParameter in methodParameters)
            {
                if (methodParameter.ParameterType == typeof(CompositeRootHttpContext))
                {
                    ctContext = context == null ? compositeRootHttpContext : new CompositeRootHttpContext(context, uploadedFiles, userName, sessionToken);
                    parameterValues[parameterValuesIndex] = ctContext;
                }
                else
                {
                    var parameters = !string.IsNullOrEmpty(compositePath.Query) ? compositePath.Query.Substring(1).Split('&') : new string[] { };

                    if (methodParameter.ParameterType.IsArray)
                    {
                        var parameterArrayType = methodParameter.ParameterType.GetElementType();
                        var parameterArray = new ArrayList();
                        foreach (var parameter in parameters.Where(p => p.StartsWith(methodParameter.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (parameter.Split('=').Length < 2)
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.MissingParameterValue, parameter));

                            var parameterValue = parameter.Split('=')[1];
                            parameterValue = string.IsNullOrEmpty(parameterValue) ? null : UnEscape(parameterValue);
                            var parameterValueToAdd = parameterValue != null ? TypeDescriptor.GetConverter(parameterArrayType).ConvertFrom(null, GetCultureInfo(context != null ? context.Request.UserLanguages : compositeRootHttpContext.Request.UserLanguages.ToArray()), parameterValue) : null;
                            parameterArray.Add(parameterValueToAdd);
                        }
                        parameterValues[parameterValuesIndex] = parameterArray.ToArray(parameterArrayType);
                    }
                    else
                    {
                        var parameter = parameters.SingleOrDefault(p => p.StartsWith(methodParameter.Name, StringComparison.OrdinalIgnoreCase));
                        if (parameter == null)
                            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.MissingParameter, methodParameter.Name));

                        if (parameter.Split('=').Length < 2)
                            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.MissingParameterValue, parameter));

                        var parameterValue = parameter.Split('=')[1];
                        parameterValue = string.IsNullOrEmpty(parameterValue) ? null : UnEscape(parameterValue);
                        parameterValues[parameterValuesIndex] = parameterValue != null ? TypeDescriptor.GetConverter(methodParameter.ParameterType).ConvertFrom(null, GetCultureInfo(context != null ? context.Request.UserLanguages : compositeRootHttpContext.Request.UserLanguages.ToArray()), parameterValue) : null;
                    }
                }

                parameterValuesIndex++;
            }

            return new CommandResponse { ReturnValue = memberMethodInfo.Invoke(composite, parameterValues), Context = ctContext };
        }

        internal static CultureInfo GetCultureInfo(this string[] acceptLanguages)
        {
            if(acceptLanguages != null && acceptLanguages.Length > 0)
                try
                {
                    return new CultureInfo(acceptLanguages[0]);
                }
                catch(CultureNotFoundException)
                {
                    return CultureInfo.CurrentCulture;
                }

            return CultureInfo.CurrentCulture;
        }

        private static object ExecuteGetCompositeDictionaryElement(object composite, Type type, string segment, HttpListenerContext context, CompositeRootHttpContext compositeRootHttpContext)
        {
            Match keyMatch;
            if ((keyMatch = Regex.Match(segment, @"^\[(?'key'.*?)\]$")).Success)
            {
                var key = TypeDescriptor.GetConverter(type.GetGenericArguments()[0]).ConvertFrom(null, GetCultureInfo(context != null ? context.Request.UserLanguages : compositeRootHttpContext.Request.UserLanguages.ToArray()), Regex.Replace(Regex.Replace(keyMatch.Groups["key"].Value, @"\[{2}", @"["), @"\]{2}", @"]"));
                if ((bool)type.GetMethod("ContainsKey").Invoke(composite, new[] { key }))
                    composite = type.GetProperty("Item").GetValue(composite, new[] { key });
                else
                    throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.KeyNotFound, key));
            }
            else if ((keyMatch = Regex.Match(segment, @"^(?'index'\d+)$")).Success)
            {
                var index = int.Parse(keyMatch.Groups["index"].Value, CultureInfo.InvariantCulture);
                var elements = type.GetProperty("Values").GetValue(composite) as IEnumerable<object>;
                composite = elements.ElementAt(index);
            }
            else
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidParameter, segment));

            return composite;
        }

        internal static bool IsConvertable(this Type type)
        {
            return TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string)) ||
                (Nullable.GetUnderlyingType(type) != null ? TypeDescriptor.GetConverter(Nullable.GetUnderlyingType(type)).CanConvertFrom(typeof(string)) : false);
        }

        internal static string[] GetTypeEnumValues(this Type parameterType)
        {
            if (parameterType.IsEnum)
                return Enum.GetNames(parameterType);
            else if (parameterType.GenericTypeArguments.Length == 1 && parameterType.GenericTypeArguments[0].IsEnum)
                return Enum.GetNames(parameterType.GenericTypeArguments[0]);
            else
                return null;
        }

        private static string UnEscape(this string value)
        {
            if (value == "%00")
                return string.Empty;

            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(value)) != value)
                value = newUrl;
            return newUrl;
        }

        public static void InitializeCompositeContainer<TKey, TComposite>(this Composite compositeContainer, out CompositeDictionary<TKey, TComposite> compositeContainerDictionary, Composite parentComposite) where TComposite : Composite
        {
            var compositeType = compositeContainer.GetType();

            ParentPropertyAttribute parentPropertyAttribute = null;
            CompositeContainerAttribute compositeDictionaryPropertyAttribute;
            CompositeModelAttribute compositeModelAttribute;

            if ((compositeDictionaryPropertyAttribute = compositeType.FindCustomAttribute<CompositeContainerAttribute>()) == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustSupplyValidCompositeDictionaryPropertyAttribute, compositeContainer.GetType().Name));

            if (parentComposite != null && (parentPropertyAttribute = compositeType.FindCustomAttribute<ParentPropertyAttribute>()) == null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustSupplyValidParentPropertyAttribute, compositeContainer.GetType().Name));

            compositeType
                .GetProperty(parentPropertyAttribute.ParentPropertyName)
                .SetValue(compositeContainer, parentComposite);

            compositeContainerDictionary = new CompositeDictionary<TKey, TComposite>();

            compositeType
                .GetProperty(compositeDictionaryPropertyAttribute.CompositeContainerDictionaryPropertyName)
                .SetValue(compositeContainer, new ReadOnlyCompositeDictionary<TKey, TComposite>(compositeContainerDictionary));

            if(!string.IsNullOrEmpty(compositeDictionaryPropertyAttribute.ModelDictionaryPropertyName))
            {
                var parentCompositeType = parentComposite.GetType();
                if ((compositeModelAttribute = parentCompositeType.FindCustomAttribute<CompositeModelAttribute>()) == null)
                    throw new InvalidOperationException();

                FieldInfo modelFieldInfo;
                if ((modelFieldInfo = parentCompositeType.GetField(compositeModelAttribute.ModelFieldName, BindingFlags.NonPublic | BindingFlags.Instance)) == null)
                    throw new InvalidOperationException();

                object parentModel;
                if ((parentModel = modelFieldInfo.GetValue(parentComposite)) == null)
                    throw new InvalidOperationException();

                PropertyInfo modelDictionaryPropertyInfo;

                if ((modelDictionaryPropertyInfo = parentModel.GetType().GetProperty(compositeDictionaryPropertyAttribute.ModelDictionaryPropertyName)) == null)
                    throw new InvalidOperationException();

                object modelDictionary;

                if ((modelDictionary = modelDictionaryPropertyInfo.GetValue(parentModel)) == null)
                    throw new InvalidOperationException();

                var models = modelDictionary.GetType().GetProperty("Values").GetValue(modelDictionary) as IEnumerable;

                KeyPropertyAttribute modelKeyPropertyAttribute = null;
                PropertyInfo modelKeyPropertyInfo = null;
                Type modelType = null;

                foreach (var model in models)
                {
                    modelType = modelType ?? model.GetType();

                    if (modelKeyPropertyAttribute == null && (modelKeyPropertyAttribute = modelType.FindCustomAttribute<KeyPropertyAttribute>()) == null)
                        throw new InvalidOperationException();

                    if (modelKeyPropertyInfo == null && (modelKeyPropertyInfo = modelType.GetProperty(modelKeyPropertyAttribute.PropertyName)) == null)
                        throw new InvalidOperationException();

                    var idValue = modelKeyPropertyInfo.GetValue(model);
                    compositeContainerDictionary.Add((TKey)idValue, Activator.CreateInstance(typeof(TComposite), new object[] { model, compositeContainer }) as TComposite);
                }
            }
        }

        public static string GetPath(this Composite composite)
        {
            if (composite == null)
                throw new ArgumentNullException(nameof(composite));

            var propertyPathBuilder = new StringBuilder();
            GetPropertyPath(null, null, composite, propertyPathBuilder);
            return propertyPathBuilder.ToString();
        }

        internal static string GetPropertyPath(this Composite composite, PropertyInfo propertyInfo)
        {
            var propertyPathBuilder = new StringBuilder();
            GetPropertyPath(propertyInfo, propertyInfo, composite, propertyPathBuilder);

            return propertyPathBuilder.ToString();
        }

        private static void GetPropertyPath(PropertyInfo startingProperty, PropertyInfo property, Composite composite, StringBuilder propertyPathBuilder)
        {
            var compositeType = composite.GetType();
            if (compositeType.IsSubclassOf(typeof(CompositeRoot)))
            {
                if(startingProperty != null && !propertyPathBuilder.ToString().EndsWith("/" + startingProperty.Name))
                {
                    if (propertyPathBuilder.Length != 0)
                        propertyPathBuilder.Append("/");

                    propertyPathBuilder.Append(startingProperty.Name);
                }

                return;
            }

            var parentPropertyAttribute = compositeType.FindCustomAttribute<ParentPropertyAttribute>();
            if (parentPropertyAttribute == null)
                return;

            var parentProperty = composite.GetType().GetProperty(parentPropertyAttribute.ParentPropertyName);

            var dictionaryPropertyAttribute = compositeType.FindCustomAttribute<CompositeContainerAttribute>();
            var keyPropertyAttribute = compositeType.FindCustomAttribute<KeyPropertyAttribute>();

            if (keyPropertyAttribute != null && parentPropertyAttribute != null && dictionaryPropertyAttribute == null)
            {
                var keyPropertyValue = compositeType.GetProperty(keyPropertyAttribute.PropertyName).GetValue(composite);
                propertyPathBuilder.Insert(0, "/[" + keyPropertyValue + "]" + (property != null && property.PropertyType.IsConvertable() ? "/" + property.Name : string.Empty));
            }
            else if (dictionaryPropertyAttribute != null)
                propertyPathBuilder.Insert(0, "/" + (property != null ?
                                                    property.Name :
                                                    compositeType.GetProperty(dictionaryPropertyAttribute.CompositeContainerDictionaryPropertyName).PropertyType.GenericTypeArguments[1].FindCustomAttribute<ParentPropertyAttribute>().ParentPropertyName) + 
                                                    (propertyPathBuilder.Length > 0 ? "/" + dictionaryPropertyAttribute.CompositeContainerDictionaryPropertyName : string.Empty));
            else
                propertyPathBuilder.Insert(0, "/" + parentPropertyAttribute.ParentCompositePropertyName);

            if (parentPropertyAttribute != null)
            {
                var parentComposite = parentProperty.GetValue(composite) as Composite;
                GetPropertyPath(startingProperty, parentProperty, parentComposite, propertyPathBuilder);
            }
        }

        internal static bool IsRequestForEvents(this Uri url, CompositeRootConfiguration compositeRootConfiguration)
        {
            return Regex.IsMatch(url.ToString(), "^" + compositeRootConfiguration.Endpoint + @"[^/]+/event", RegexOptions.IgnoreCase);
        }

        internal static bool IsRequestForPublicFile(this Uri url, CompositeRootConfiguration compositeRootConfiguration, ref string filePath)
        {
            return Regex.IsMatch(url.ToString(), "^" + compositeRootConfiguration.Endpoint + "*?" + compositeRootConfiguration.EndpointPublicDirectory, RegexOptions.IgnoreCase) &&
                !string.IsNullOrEmpty(filePath = Path.Combine(Environment.CurrentDirectory, compositeRootConfiguration.EndpointPublicDirectory, string.Join("/", url.Segments.SkipWhile(s => s.ToUpperInvariant() != compositeRootConfiguration.EndpointPublicDirectory.ToUpperInvariant() + "/").Skip(1).Select(s => s.Trim('/')))));
        }

        internal static bool IsRequestForPrivateFile(this Uri url, CompositeRootConfiguration compositeRootConfiguration, CompositeRoot compositeRoot, ref string filePath)
        {
            return Regex.IsMatch(url.ToString(), "^" + compositeRootConfiguration.Endpoint + @"[^/]+/~/") && compositeRoot != null &&
                !string.IsNullOrEmpty(filePath = Path.Combine(Environment.CurrentDirectory, compositeRootConfiguration.EndpointPrivateDirectory, string.Join(" / ", url.Segments.SkipWhile(s => s != "~/").Skip(1).Select(s => s.Trim('/')))));
        }

        internal static bool IsRequestForLogin(this Uri url, CompositeRootConfiguration compositeRootConfiguration)
        {
            return Regex.IsMatch(url.ToString(), "^" + compositeRootConfiguration.Endpoint + nameof(CompositeRootAuthenticator.LogOn), RegexOptions.IgnoreCase);
        }

        internal static string ToStringClean(this Uri uri)
        {
            return uri.Scheme + "://" + uri.Host + (uri.Port == 80 || uri.Port == 443 ? "" : ":" + uri.Port.ToString()) + "/" + string.Join(string.Empty, uri.Segments.Where(s => s != "/"));
        }

        public static TAttribute FindCustomAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            TAttribute attribute;

            if (type == null)
                return default;

            if ((attribute = type.GetCustomAttribute<TAttribute>()) != null)
                return attribute;
            else
                return FindCustomAttribute<TAttribute>(type.BaseType);
        }

        public static string RunTemplate(this string template, string rootObjectName, object rootObjectValue, string expressionBeginToken, string expressionEndToken)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentNullException(nameof(template));

            if(string.IsNullOrWhiteSpace(rootObjectName))
                throw new ArgumentNullException(nameof(rootObjectName));

            if (rootObjectValue == null)
                throw new ArgumentNullException(nameof(rootObjectValue));

            if (string.IsNullOrWhiteSpace(expressionBeginToken))
                throw new ArgumentNullException(nameof(expressionBeginToken));

            if (string.IsNullOrWhiteSpace(expressionEndToken))
                throw new ArgumentNullException(nameof(expressionEndToken));

            return Regex.Replace(template, expressionBeginToken + @"\s*(?'exp'.+?)" + expressionEndToken, match =>
            {
                try
                {
                    return DynamicExpressionParser
                            .ParseLambda(new[] { Expression.Parameter(rootObjectValue.GetType(), rootObjectName) }, null, match.Groups["exp"].Value)
                            .Compile()
                            .DynamicInvoke(rootObjectValue)
                            .ToString();
                }
                catch (Exception e)
                {
                    return match.Value + " : " + e.Message;
                }
            });
        }

        public static string RunTemplate(this string template, object rootObjectValue, string expressionBeginToken, string expressionEndToken, IReadOnlyDictionary<string, Delegate> compiledTemplateDelegates)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentNullException(nameof(template));

            if (rootObjectValue == null)
                throw new ArgumentNullException(nameof(rootObjectValue));

            if (string.IsNullOrWhiteSpace(expressionBeginToken))
                throw new ArgumentNullException(nameof(expressionBeginToken));

            if (string.IsNullOrWhiteSpace(expressionEndToken))
                throw new ArgumentNullException(nameof(expressionEndToken));

            return Regex.Replace(template, expressionBeginToken + @"\s*(?'exp'.+?)" + expressionEndToken, match =>
            {
                try
                {
                    return compiledTemplateDelegates[match.Groups["exp"].Value].DynamicInvoke(rootObjectValue).ToString();
                }
                catch (Exception e)
                {
                    return match.Value + " : " + e.Message;
                }
            });
        }

        public static IReadOnlyDictionary<string, Delegate> CompileTemplate(this string template, string rootObjectName, Type rootObjectType, string expressionBeginToken, string expressionEndToken)
        {
            if (string.IsNullOrWhiteSpace(template))
                throw new ArgumentNullException(nameof(template));

            if (string.IsNullOrWhiteSpace(rootObjectName))
                throw new ArgumentNullException(nameof(rootObjectName));

            if (string.IsNullOrWhiteSpace(expressionBeginToken))
                throw new ArgumentNullException(nameof(expressionBeginToken));

            if (string.IsNullOrWhiteSpace(expressionEndToken))
                throw new ArgumentNullException(nameof(expressionEndToken));

            var delegates = new Dictionary<string, Delegate>();

            foreach(Match expressionMatch in Regex.Matches(template, expressionBeginToken + @"\s*(?'exp'.+?)" + expressionEndToken))
            {
                var expression = expressionMatch.Groups["exp"].Value;
                if(!delegates.ContainsKey(expression))
                    delegates.Add(expression, DynamicExpressionParser
                                .ParseLambda(new[] { Expression.Parameter(rootObjectType, rootObjectName) }, null, expression)
                                .Compile());
            }

            return delegates;
        }

        public static void TraverseDepthFirst(this Composite composite, Action<Composite> action)
        {
            action(composite);

            foreach(var compositePropertyInfo in composite.GetType().GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null))
            {
                var compositePropertyType = compositePropertyInfo.PropertyType;
                var compositePropertyGenericType = compositePropertyType.IsGenericType ? compositePropertyType.GetGenericTypeDefinition() : null;

                if (compositePropertyType.IsSubclassOf(typeof(Composite)))
                    TraverseDepthFirst(composite.GetType().GetProperty(compositePropertyInfo.Name).GetValue(composite) as Composite, action);

                if(compositePropertyGenericType == typeof(ReadOnlyCompositeDictionary<,>))
                {
                    var compositeDictionary = compositePropertyInfo.GetValue(composite) as dynamic;
                    foreach (var c in compositeDictionary.Values as IEnumerable<Composite>)
                        TraverseDepthFirst(c, action);
                }
            }
        }

        public static DataTable ToDataTable(this IEnumerable<Composite> composites)
        {
            if (composites == null)
                throw new ArgumentNullException(nameof(composites));

            CompositeModelAttribute compositeModelAttribute = null;
            DataTable dataTable = null;
            FieldInfo modelFieldInfo = null;
            IEnumerable<PropertyInfo> modelProperties = null;
            Type compositeType = null;

            foreach(var composite in composites)
            {
                if (compositeType == null)
                    compositeType = composite.GetType();
                else
                    if (composite.GetType() != compositeType)
                        throw new InvalidOperationException(Resources.MustAllBeSameCompositeType);

                if(compositeModelAttribute == null)
                {
                    compositeModelAttribute = compositeType.FindCustomAttribute<CompositeModelAttribute>();
                    if (compositeModelAttribute == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveCompositeModelAttribute, compositeType.Name));
                }

                if (modelFieldInfo == null)
                    modelFieldInfo = compositeType.GetField(compositeModelAttribute.ModelFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (modelFieldInfo == null)
                    throw new MemberAccessException(Resources.CannotFindCompositeModelProperty);

                KeyPropertyAttribute keyPropertyAttribute = null;
                if ((keyPropertyAttribute = modelFieldInfo.FieldType.GetCustomAttribute<KeyPropertyAttribute>()) == null)
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveKeyPropertyAttribute, modelFieldInfo.FieldType));

                var modelKeyPropertyName = keyPropertyAttribute.PropertyName;

                var model = modelFieldInfo.GetValue(composite);

                if (modelProperties == null)
                    modelProperties = model.GetType().GetProperties().Where(p => p.GetCustomAttributes<DataMemberAttribute>().Any());

                if(dataTable == null)
                {
                    DataContractAttribute dataContractAttribute = null;

                    if ((dataContractAttribute = modelFieldInfo.FieldType.GetCustomAttribute<DataContractAttribute>()) == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveDataContractAttribute, modelFieldInfo.FieldType));

                    var dataTableName = dataContractAttribute.Name ?? modelFieldInfo.FieldType.Name;

                    if (!Regex.IsMatch(dataTableName, @"^[A-Za-z0-9_]+$"))
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidTableName, dataTableName));

                    dataTable = new DataTable(dataTableName);

                    Type columnType = null;
                    foreach(var modelProperty in modelProperties)
                    {
                        var columnName = modelProperty.GetCustomAttribute<DataMemberAttribute>()?.Name ?? modelProperty.Name;

                        if ((columnType = Nullable.GetUnderlyingType(modelProperty.PropertyType)) != null)
                            dataTable.Columns.Add(new DataColumn(columnName, columnType) { AllowDBNull = true });
                        else
                            dataTable.Columns.Add(new DataColumn(columnName, modelProperty.PropertyType));
                    }

                    dataTable.Columns.Add("__model", model.GetType());
                    dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns[modelKeyPropertyName] };
                }

                var dataRow = dataTable.NewRow();
                
                foreach (var modelProperty in modelProperties)
                {
                    var columnName = modelProperty.GetCustomAttribute<DataMemberAttribute>()?.Name ?? modelProperty.Name;
                    dataRow[columnName] = modelProperty.GetValue(model) ?? DBNull.Value;
                }

                dataRow["__model"] = model;
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        public static T ToModel<T>(this IDataRecord record) where T : new()
        {
            var modelType = typeof(T);
            var model = new T();
            var modelProperties = modelType.GetProperties();
            PropertyInfo propertyInfo = null;

            for (int columnIndex = 0; columnIndex < record.FieldCount; columnIndex++)
            {
                var columnName = record.GetName(columnIndex);

                if ((propertyInfo = modelProperties.SingleOrDefault(p => (p.GetCustomAttribute<DataMemberAttribute>()?.Name ?? p.Name).ToLowerInvariant() == columnName.ToLowerInvariant())) == null)
                    continue;

                propertyInfo.SetValue(model, record.IsDBNull(columnIndex) ? null : record[columnIndex]);
            }

            return model;
        }

        public static string GetRequest(this Stream stream, Encoding contentEncoding, string contentType, string urlQueryString, CultureInfo cultureInfo, out IEnumerable<CompositeUploadedFile> uploadedFiles, out IEnumerable<CompositeRootCommandRequest> multipleCommandRequest)
        {
            multipleCommandRequest = null;
            var uploadedFilesList = new List<CompositeUploadedFile>();
          

            var requestBody = string.Empty;
            if (stream != Stream.Null)
            {
                byte[] requestContent;
                string requestContentType;
                Encoding requestEncoding;

                using (var requestStream = new MemoryStream())
                {
                    stream.CopyTo(requestStream);
                    requestEncoding = contentEncoding;
                    requestContentType = string.IsNullOrEmpty(contentType) ? "application/x-www-form-urlencoded" : contentType;
                    requestContent = requestStream.ToArray();
                }

                Match matchedBoundary;

                if ((matchedBoundary = Regex.Match(requestContentType, @"^multipart/form-data;\s+boundary=(?'boundary'.+)$")).Success)
                    requestBody = GetMultiPartFormDataRequest(uploadedFilesList, requestContent, requestEncoding, matchedBoundary.Groups["boundary"].Value);
                else if (Regex.IsMatch(requestContentType, @"application/x-www-form-urlencoded|application/json"))
                {
                    requestBody = requestEncoding.GetString(requestContent);
                    try
                    {
                        multipleCommandRequest = JsonConvert.DeserializeObject<IEnumerable<CompositeRootCommandRequest>>(requestBody, new JsonSerializerSettings { Culture = cultureInfo });
                    }
                    catch (JsonReaderException)
                    {
                    }
                }
                else
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.RequestContentTypeNotSupported, contentType));
            }
            else if (!string.IsNullOrEmpty(urlQueryString))
                requestBody = urlQueryString.Substring(1);

            uploadedFiles = uploadedFilesList;

            return requestBody;
        }

        private static string GetMultiPartFormDataRequest(List<CompositeUploadedFile> uploadedFilesList, byte[] requestContent, Encoding requestEncoding, string boundry)
        {
            var headerEndBytes = Encoding.ASCII.GetBytes("\r\n\r\n");
            var boundaryBytes = requestEncoding.GetBytes("--" + boundry);
            var boundaryIndexes = FindBytes(requestContent, boundaryBytes);

            var requestBodyBuilder = new StringBuilder();

            for (var i = 0; i < boundaryIndexes.Count; i++)
            {
                var startIndex = boundaryIndexes[i] + boundaryBytes.Length;
                var endIndex = i + 1 == boundaryIndexes.Count ? requestContent.Length - 1 : boundaryIndexes[i + 1] - 1;

                var blockBytes = requestContent.Skip(startIndex).Take((endIndex - startIndex) - 1).ToArray();
                var contentBeginIndex = FindBytes(blockBytes, headerEndBytes).FirstOrDefault();

                if (contentBeginIndex == 0)
                    continue;

                var headerText = requestEncoding.GetString(blockBytes.Take(contentBeginIndex).ToArray());

                Match nameMatch;
                Match contentTypeMatch;

                if ((nameMatch = Regex.Match(headerText, @"Content-Disposition: form-data; name=\x22(?'name'\w+)\x22;?\s*(?:filename=\x22(?'filename'[^\x22]+)\x22)?")).Success)
                {
                    if ((contentTypeMatch = Regex.Match(headerText, @"Content-Type:\s+(?'contentType'\S+\w)")).Success)
                        uploadedFilesList.Add(new CompositeUploadedFile(nameMatch.Groups["name"].Value, nameMatch.Groups["filename"].Value, blockBytes.Skip(contentBeginIndex + 4).ToArray(), contentTypeMatch.Groups["contentType"].Value));
                    else
                    {
                        var formDataValue = requestEncoding.GetString(blockBytes.Skip(contentBeginIndex + 4).ToArray());
                        requestBodyBuilder.AppendFormat(CultureInfo.CurrentCulture, "{0}={1}&", nameMatch.Groups["name"].Value, formDataValue == "%00" ? "%00" : Uri.EscapeDataString(formDataValue));
                    }
                }
            }

            return requestBodyBuilder.ToString();
        }
        private static IReadOnlyList<int> FindBytes(byte[] buffer, byte[] pattern)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var positions = new List<int>();

            if (buffer.Length < pattern.Length)
                return positions;

            for (var bufferIndex = 0; bufferIndex < buffer.Length - pattern.Length + 1; bufferIndex++)
                if (!pattern.Where((data, index) => !buffer[bufferIndex + index].Equals(data)).Any())
                    positions.Add(bufferIndex);

            return positions;
        }
    }
}
