// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	public readonly struct ReflectionMarker
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;
		readonly bool _enabled;

		public ReflectionMarker (LinkContext context, MarkStep markStep, bool enabled)
		{
			_context = context;
			_markStep = markStep;
			_enabled = enabled;
		}

		internal void MarkTypeForDynamicallyAccessedMembers (in MessageOrigin origin, TypeDefinition typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, DependencyKind dependencyKind, bool declaredOnly = false)
		{
			if (!_enabled)
				return;

			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (_context, requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case MethodDefinition method:
					MarkMethod (origin, method, dependencyKind);
					break;
				case FieldDefinition field:
					MarkField (origin, field, dependencyKind);
					break;
				case TypeDefinition nestedType:
					MarkType (origin, nestedType, dependencyKind);
					break;
				case PropertyDefinition property:
					MarkProperty (origin, property, dependencyKind);
					break;
				case EventDefinition @event:
					MarkEvent (origin, @event, dependencyKind);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (origin, interfaceImplementation, dependencyKind);
					break;
				}
			}
		}

		internal bool TryResolveTypeNameAndMark (string typeName, in DiagnosticContext diagnosticContext, bool needsAssemblyName, [NotNullWhen (true)] out TypeDefinition? type)
		{
			if (!_context.TypeNameResolver.TryResolveTypeName (typeName, diagnosticContext, out TypeReference? typeRef, out AssemblyDefinition? typeAssembly, needsAssemblyName)
				|| typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition foundType) {
				type = default;
				return false;
			}

			if (_enabled) {
				var origin = diagnosticContext.Origin;
				_markStep.MarkTypeVisibleToReflection (typeRef, foundType, new DependencyInfo (DependencyKind.AccessedViaReflection, origin.Provider), origin);
				_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, foundType), origin);
			}

			type = foundType;
			return true;
		}

		internal void MarkType (in MessageOrigin origin, TypeDefinition type, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkTypeVisibleToReflection (type, type, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		internal void MarkMethod (in MessageOrigin origin, MethodDefinition method, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkMethodVisibleToReflection (method, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkField (in MessageOrigin origin, FieldDefinition field, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkFieldVisibleToReflection (field, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		internal void MarkProperty (in MessageOrigin origin, PropertyDefinition property, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkPropertyVisibleToReflection (property, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkEvent (in MessageOrigin origin, EventDefinition @event, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkEventVisibleToReflection (@event, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkInterfaceImplementation (in MessageOrigin origin, InterfaceImplementation interfaceImplementation, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (!_enabled)
				return;

			_markStep.MarkInterfaceImplementation (interfaceImplementation, null, new DependencyInfo (dependencyKind, origin.Provider));
		}

		internal void MarkConstructorsOnType (in MessageOrigin origin, TypeDefinition type, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			if (!_enabled)
				return;

			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (origin, ctor);
		}

		internal void MarkFieldsOnTypeHierarchy (in MessageOrigin origin, TypeDefinition type, Func<FieldDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			if (!_enabled)
				return;

			foreach (var field in type.GetFieldsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkField (origin, field);
		}

		internal void MarkPropertiesOnTypeHierarchy (in MessageOrigin origin, TypeDefinition type, Func<PropertyDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			if (!_enabled)
				return;

			foreach (var property in type.GetPropertiesOnTypeHierarchy (_context, filter, bindingFlags))
				MarkProperty (origin, property);
		}

		internal void MarkEventsOnTypeHierarchy (in MessageOrigin origin, TypeDefinition type, Func<EventDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			if (!_enabled)
				return;

			foreach (var @event in type.GetEventsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkEvent (origin, @event);
		}

		internal void MarkStaticConstructor (in MessageOrigin origin, TypeDefinition type)
		{
			if (!_enabled)
				return;

			_markStep.MarkStaticConstructorVisibleToReflection (type, new DependencyInfo (DependencyKind.AccessedViaReflection, origin.Provider), origin);
		}
	}
}