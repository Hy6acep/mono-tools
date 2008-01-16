using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Mono.Cecil;
using Gtk;

namespace GuiCompare {

	static class MasterUtils {
		public static void PopulateMethodList (XMLMethods methods, List<CompNamed> method_list)
		{
			foreach (object key in methods.keys.Keys) {
				XMLMethods.SignatureFlags signatureFlags = (methods.signatureFlags != null &&
				                                            methods.signatureFlags.ContainsKey (key) ?
				                                            (XMLMethods.SignatureFlags) methods.signatureFlags [key] :
				                                            XMLMethods.SignatureFlags.None);

				XMLParameters parameters = (methods.parameters == null ? null
				                            : (XMLParameters)methods.parameters[key]);
				XMLGenericMethodConstraints genericConstraints = (methods.genericConstraints == null ? null
				                                                  : (XMLGenericMethodConstraints)methods.genericConstraints[key]);
				XMLAttributes attributes = (methods.attributeMap == null ? null
				                            : (XMLAttributes)methods.attributeMap[key]);
				string returnType = (methods.returnTypes == null ? null
				                     : (string)methods.returnTypes[key]);
				method_list.Add (new MasterMethod ((string)methods.keys[key],
				                                   signatureFlags,
				                                   returnType,
				                                   parameters,
				                                   genericConstraints,
				                                   methods.ConvertToString (Int32.Parse ((string)methods.access[key])),
				                                   attributes));
			}
		}
		                                       
		public static void PopulateMemberLists (XMLClass xml_cls,
		                                        List<CompNamed> interface_list,
		                                        List<CompNamed> constructor_list,
		                                        List<CompNamed> method_list,
		                                        List<CompNamed> property_list,
		                                        List<CompNamed> field_list,
		                                        List<CompNamed> event_list)
		{
			if (interface_list != null && xml_cls.interfaces != null) {
				foreach (object i in xml_cls.interfaces.keys.Keys) {
					interface_list.Add (new MasterInterface ((string)xml_cls.interfaces.keys[i]));
				}
			}
			
			if (constructor_list != null && xml_cls.constructors != null) {
				PopulateMethodList (xml_cls.constructors, constructor_list);
			}
			
			if (method_list != null && xml_cls.methods != null) {
				PopulateMethodList (xml_cls.methods, method_list);
			}
			
			if (property_list != null && xml_cls.properties != null) {
				foreach (object key in xml_cls.properties.keys.Keys) {
					XMLAttributes attributes = (xml_cls.properties.attributeMap == null ? null
					                            : (XMLAttributes)xml_cls.properties.attributeMap[key]);

					property_list.Add (new MasterProperty ((string)key,
					                                       (string)xml_cls.properties.keys[key],
					                                       xml_cls.properties.ConvertToString (Int32.Parse ((string)xml_cls.properties.access[key])),
					                                       (XMLMethods)xml_cls.properties.nameToMethod[key],
					                                       attributes));
				}
			}
			
			if (field_list != null && xml_cls.fields != null) {
				foreach (object key in xml_cls.fields.keys.Keys) {
					string type = (xml_cls.fields.fieldTypes == null || !xml_cls.fields.fieldTypes.ContainsKey(key)) ? null : (string)xml_cls.fields.fieldTypes[key];
					string fvalue = (xml_cls.fields.fieldValues == null || !xml_cls.fields.fieldValues.ContainsKey(key)) ? null : (string)xml_cls.fields.fieldValues[key];
					XMLAttributes attributes = (xml_cls.fields.attributeMap == null ? null
					                            : (XMLAttributes)xml_cls.fields.attributeMap[key]);

					field_list.Add (new MasterField ((string)xml_cls.fields.keys[key],
					                                 type, fvalue,
					                                 xml_cls.fields.ConvertToString(Int32.Parse ((string)xml_cls.fields.access[key])),
					                                 attributes));
				}
			}
			
			if (event_list != null && xml_cls.events != null) {
				foreach (object key in xml_cls.events.keys.Keys) {
					XMLAttributes attributes = (xml_cls.events.attributeMap == null ? null
					                            : (XMLAttributes)xml_cls.events.attributeMap[key]);
					event_list.Add (new MasterEvent ((string)xml_cls.events.keys[key],
					                                 (string)xml_cls.events.eventTypes[key],
					                                 xml_cls.events.ConvertToString (Int32.Parse ((string)xml_cls.events.access[key])),
					                                 attributes));
				}
			}
		}
		

		public static void PopulateTypeLists (XMLClass fromDef,
		                                      List<CompNamed> class_list,
		                                      List<CompNamed> enum_list,
		                                      List<CompNamed> delegate_list,
		                                      List<CompNamed> interface_list,
		                                      List<CompNamed> struct_list)
		{
			if (fromDef.nested == null)
				return;
			
			foreach (XMLClass cls in fromDef.nested) {
				if (cls.type == "class")
					class_list.Add (new MasterClass (cls, CompType.Class));
				else if (cls.type == "enum")
					enum_list.Add (new MasterEnum (cls));
				else if (cls.type == "delegate")
					delegate_list.Add (new MasterDelegate (cls));
				else if (cls.type == "interface")
					interface_list.Add (new MasterInterface (cls));
				else if (cls.type == "struct")
					struct_list.Add (new MasterClass (cls, CompType.Struct));
			}
		}
		
		public static bool ShouldSkipAttribute (string name)
		{
			if (name.StartsWith ("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"))
				return true;
			
			return false;
		}
		
		public static List<CompNamed> GetAttributes (XMLAttributes attributes)
		{
			List<CompNamed> rv = new List<CompNamed>();
			if (attributes != null) {
				foreach (object key in attributes.keys.Keys) {
					if (ShouldSkipAttribute ((string)key))
						continue;
					rv.Add (new MasterAttribute ((string)attributes.keys[key]));
				}
			}
			return rv;
		}

	}
	
	public class MasterAssembly : CompAssembly {
		public MasterAssembly (string path)
			: base (path)
		{
			masterinfo = XMLAssembly.CreateFromFile (path);
		}

		public override List<CompNamed> GetNamespaces ()
		{
			List<CompNamed> namespaces = new List<CompNamed>();
			if (masterinfo != null && masterinfo.namespaces != null) {
				foreach (XMLNamespace ns in masterinfo.namespaces)
					namespaces.Add (new MasterNamespace (ns));
			}

			return namespaces;
		}

		XMLAssembly masterinfo;
	}

	public class MasterNamespace : CompNamespace {
		public MasterNamespace (XMLNamespace ns)
			: base (ns.name)
		{
			this.ns = ns;

			delegate_list = new List<CompNamed>();
			enum_list = new List<CompNamed>();
			class_list = new List<CompNamed>();
			struct_list = new List<CompNamed>();
			interface_list = new List<CompNamed>();

			foreach (XMLClass cls in ns.types) {
				if (cls.type == "class")
					class_list.Add (new MasterClass (cls, CompType.Class));
				else if (cls.type == "enum")
					enum_list.Add (new MasterEnum (cls));
				else if (cls.type == "delegate")
					delegate_list.Add (new MasterDelegate (cls));
				else if (cls.type == "interface")
					interface_list.Add (new MasterInterface (cls));
				else if (cls.type == "struct")
					struct_list.Add (new MasterClass (cls, CompType.Struct));
			}
		}

		public override List<CompNamed> GetNestedClasses ()
		{
			return class_list;
		}

		public override List<CompNamed> GetNestedInterfaces ()
		{
			return interface_list;
		}

		public override List<CompNamed> GetNestedStructs ()
		{
			return struct_list;
		}

		public override List<CompNamed> GetNestedEnums ()
		{
			return enum_list;
		}

		public override List<CompNamed> GetNestedDelegates ()
		{
			return delegate_list;
		}

		XMLNamespace ns;
		List<CompNamed> delegate_list;
		List<CompNamed> enum_list;
		List<CompNamed> class_list;
		List<CompNamed> struct_list;
		List<CompNamed> interface_list;
	}

	public class MasterInterface : CompInterface {
		public MasterInterface (XMLClass xml_cls)
			: base (xml_cls.name)
		{
			this.xml_cls = xml_cls;
			
			interfaces = new List<CompNamed>();
			constructors = new List<CompNamed>();
			methods = new List<CompNamed>();
			properties = new List<CompNamed>();
			fields = new List<CompNamed>();
			events = new List<CompNamed>();
			
			MasterUtils.PopulateMemberLists (xml_cls,
			                                 interfaces,
			                                 constructors,
			                                 methods,
			                                 properties,
			                                 fields,
			                                 events);
			
			attributes = MasterUtils.GetAttributes (xml_cls.attributes);
		}
		
		public MasterInterface (string name)
			: base (name)
		{
			interfaces = new List<CompNamed>();
			constructors = new List<CompNamed>();
			methods = new List<CompNamed>();
			properties = new List<CompNamed>();
			fields = new List<CompNamed>();
			events = new List<CompNamed>();
			attributes = new List<CompNamed>();
		}

		public override string GetBaseType()
		{
			return xml_cls == null ? null : xml_cls.baseName;
		}
		
		public override List<CompNamed> GetInterfaces ()
		{
			return interfaces;
		}

		public override List<CompNamed> GetMethods ()
		{
			return methods;
		}

		public override List<CompNamed> GetConstructors ()
		{
			return constructors;
		}

 		public override List<CompNamed> GetProperties()
		{
			return properties;
		}

 		public override List<CompNamed> GetFields()
		{
			return fields;
		}

 		public override List<CompNamed> GetEvents()
		{
			return events;
		}

		public override List<CompNamed> GetAttributes ()
		{
			return attributes;
		}
		
		XMLClass xml_cls;
		List<CompNamed> interfaces;
		List<CompNamed> constructors;
		List<CompNamed> methods;
		List<CompNamed> properties;
		List<CompNamed> fields;
		List<CompNamed> events;
		List<CompNamed> attributes;
	}

	public class MasterDelegate : CompDelegate {
		public MasterDelegate (XMLClass cls)
			: base (cls.name)
		{
			xml_cls = cls;
		}

		public override string GetBaseType ()
		{
			return xml_cls.baseName;
		}
		
		XMLClass xml_cls;
	}

	public class MasterEnum : CompEnum {
		public MasterEnum (XMLClass cls)
			: base (cls.name)
		{
			xml_cls = cls;
			
			fields = new List<CompNamed>();

			MasterUtils.PopulateMemberLists (xml_cls,
			                                 null,
			                                 null,
			                                 null,
			                                 null,
			                                 fields,
			                                 null);

		}

		public override string GetBaseType()
		{
			return xml_cls.baseName;
		}
		
 		public override List<CompNamed> GetFields()
		{
			return fields;
		}

		public override List<CompNamed> GetAttributes ()
		{
			return MasterUtils.GetAttributes (xml_cls.attributes);
		}

		List<CompNamed> fields;
		XMLClass xml_cls;
	}

	public class MasterClass : CompClass {
		public MasterClass (XMLClass cls, CompType type)
			: base (cls.name, type)
		{
			xml_cls = cls;

			interfaces = new List<CompNamed>();
			constructors = new List<CompNamed>();
			methods = new List<CompNamed>();
			properties = new List<CompNamed>();
			fields = new List<CompNamed>();
			events = new List<CompNamed>();

			MasterUtils.PopulateMemberLists (xml_cls,
			                                 interfaces,
			                                 constructors,
			                                 methods,
			                                 properties,
			                                 fields,
			                                 events);
			
			delegate_list = new List<CompNamed>();
			enum_list = new List<CompNamed>();
			class_list = new List<CompNamed>();
			struct_list = new List<CompNamed>();
			interface_list = new List<CompNamed>();

			MasterUtils.PopulateTypeLists (xml_cls,
			                               class_list,
			                               enum_list,
			                               delegate_list,
			                               interface_list,
			                               struct_list);
		}

		public override string GetBaseType ()
		{
			return xml_cls.baseName;
		}
		
		public override List<CompNamed> GetInterfaces ()
		{
			return interfaces;
		}

		public override List<CompNamed> GetMethods()
		{
			return methods;
		}

		public override List<CompNamed> GetConstructors()
		{
			return constructors;
		}

 		public override List<CompNamed> GetProperties()
		{
			return properties;
		}

 		public override List<CompNamed> GetFields()
		{
			return fields;
		}

 		public override List<CompNamed> GetEvents()
		{
			return events;
		}

		public override List<CompNamed> GetAttributes ()
		{
			return MasterUtils.GetAttributes (xml_cls.attributes);
		}

		public override List<CompNamed> GetNestedClasses()
		{
			return class_list;
		}

		public override List<CompNamed> GetNestedInterfaces ()
		{
			return interface_list;
		}

		public override List<CompNamed> GetNestedStructs ()
		{
			return struct_list;
		}

		public override List<CompNamed> GetNestedEnums ()
		{
			return enum_list;
		}

		public override List<CompNamed> GetNestedDelegates ()
		{
			return delegate_list;
		}

		XMLClass xml_cls;

		List<CompNamed> interfaces;
		List<CompNamed> constructors;
		List<CompNamed> methods;
		List<CompNamed> properties;
		List<CompNamed> fields;
		List<CompNamed> events;
		
		List<CompNamed> delegate_list;
		List<CompNamed> enum_list;
		List<CompNamed> class_list;
		List<CompNamed> struct_list;
		List<CompNamed> interface_list;
}

	public class MasterEvent : CompEvent {
		public MasterEvent (string name,
		                    string eventType,
		                    string eventAccess,
		                    XMLAttributes attributes)
			: base (name)
		{
			this.eventType = eventType;
			this.eventAccess = eventAccess;
			this.attributes = attributes;
		}

		public override string GetMemberType ()
		{
			return eventType;
		}

		public override string GetMemberAccess ()
		{
			return eventAccess;
		}
		
		public override List<CompNamed> GetAttributes ()
		{
			return MasterUtils.GetAttributes (attributes);
		}
		
		string eventType;
		string eventAccess;
		XMLAttributes attributes;
	}
	

	public class MasterField : CompField {
		public MasterField (string name,
		                    string fieldType,
		                    string fieldValue,
		                    string fieldAccess,
		                    XMLAttributes attributes)
			: base (name)
		{
			this.fieldType = fieldType;
			this.fieldValue = fieldValue;
			this.fieldAccess = fieldAccess;
			this.attributes = attributes;
		}

		public override string GetMemberType ()
		{
			return fieldType;
		}
		
		public override string GetMemberAccess ()
		{
			return fieldAccess;
		}
		
		public override List<CompNamed> GetAttributes ()
		{
			return MasterUtils.GetAttributes (attributes);
		}
		
		public override string GetLiteralValue ()
		{
			return fieldValue;
		}

		string fieldType;
		string fieldValue;
		string fieldAccess;
		XMLAttributes attributes;
	}
	
	public class MasterProperty : CompProperty {
		public MasterProperty (string key, string name, string propertyAccess, XMLMethods xml_methods, XMLAttributes attributes)
			: base (name)
		{
			string[] keyparts = key.Split(new char[] {':'}, 3);
			
			this.propertyType = keyparts[1];
			this.propertyAccess = propertyAccess;
			
			methods = new List<CompNamed>();			
			
			MasterUtils.PopulateMethodList (xml_methods, methods);
			this.attributes = attributes;
		}

		public override List<CompNamed> GetAttributes ()
		{
			return MasterUtils.GetAttributes (attributes);
		}
		
		public override string GetMemberType()
		{
			return propertyType;
		}
		
		public override string GetMemberAccess()
		{
			return propertyAccess;
		}
		
		public override List<CompNamed> GetMethods()
		{
			return methods;
		}

		List<CompNamed> methods;
		XMLAttributes attributes;
		string propertyType;
		string propertyAccess;
	}
	
	public class MasterMethod : CompMethod {
		public MasterMethod (string name,
		                     XMLMethods.SignatureFlags signatureFlags,
		                     string returnType,
		                     XMLParameters parameters,
		                     XMLGenericMethodConstraints genericConstraints,
		                     string methodAccess,
		                     XMLAttributes attributes)
			: base (String.Format ("{0} {1}", returnType, name))
		{
			this.signatureFlags = signatureFlags;
			this.returnType = returnType;
			this.parameters = parameters;
			this.genericConstraints = genericConstraints;
			this.methodAccess = methodAccess;
			this.attributes = attributes;
		}

		public override string GetMemberType()
		{
			return returnType;
		}

		public override bool ThrowsNotImplementedException ()
		{
			return false;
		}
		
		public override string GetMemberAccess()
		{
			return methodAccess;
		}
		
		public override List<CompNamed> GetAttributes ()
		{
			return MasterUtils.GetAttributes (attributes);
		}

		XMLMethods.SignatureFlags signatureFlags;
		string returnType;
		XMLParameters parameters;
		XMLGenericMethodConstraints genericConstraints;
		string methodAccess;
		XMLAttributes attributes;
	}
			         
	public class MasterAttribute : CompAttribute {
		public MasterAttribute (string name)
			: base (name)
		{
		}
	}
}
