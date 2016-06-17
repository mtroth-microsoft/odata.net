﻿//---------------------------------------------------------------------
// <copyright file="ReaderValidator.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData
{
    using System;
    using System.Diagnostics;
    using Microsoft.OData.Edm;
    using Microsoft.OData.JsonLight;
    using Microsoft.OData.Metadata;

    /// <summary>
    /// Reader validator that binds to an ODataMessageReaderSettings instance.
    /// </summary>
    internal class ReaderValidator : IReaderValidator
    {
        /// <summary>
        /// References the bound ODataMessageReaderSettings instance.
        /// </summary>
        private readonly ODataMessageReaderSettings settings;

        /// <summary>
        /// Constructs a ReaderValidator instance that binds to settings.
        /// </summary>
        /// <param name="settings">The ODataMessageReaderSettings instance to bind to.</param>
        internal ReaderValidator(ODataMessageReaderSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Validates that the specified <paramref name="resource"/> is a valid resource as per the specified type.
        /// </summary>
        /// <param name="resource">The resource to validate.</param>
        /// <param name="resourceType">Optional entity type to validate the resource against.</param>
        /// <param name="model">Model containing the entity type.</param>
        /// <remarks>If the <paramref name="resourceType"/> is available only resource-level tests are performed,
        /// properties and such are not validated.</remarks>
        public virtual void ValidateMediaResource(ODataResource resource, IEdmEntityType resourceType, IEdmModel model)
        {
            if (settings.BasicValidation)
            {
                ValidationUtils.ValidateMediaResource(resource, resourceType, model);
            }
        }

        /// <summary>
        /// Creates a DuplicatePropertyNamesChecker instance.
        /// </summary>
        /// <param name="readResponse">true if reading a response; false otherwise.</param>
        /// <returns>The created instance.</returns>
        public DuplicatePropertyNamesChecker CreateDuplicatePropertyNamesChecker(bool readResponse)
        {
            return new DuplicatePropertyNamesChecker(!settings.ThrowOnDuplicatePropertyNames, readResponse);
        }

        /// <summary>
        /// Validate a null value.
        /// </summary>
        /// <param name="expectedTypeReference">The expected type of the null value.</param>
        /// <param name="validateNullValue">true to validate the null value; false to only check whether the type is
        /// supported.</param>
        /// <param name="propertyName">The name of the property whose value is being read, if applicable
        /// (used for error reporting).</param>
        /// <param name="isDynamicProperty">Indicates whether the property is dynamic or unknown.</param>
        public void ValidateNullValue(IEdmTypeReference expectedTypeReference,
                                      bool validateNullValue, string propertyName,
                                      bool? isDynamicProperty)
        {
            ReaderValidationUtils.ValidateNullValue(expectedTypeReference, settings.DisablePrimitiveTypeConversion,
                                                    validateNullValue, propertyName, isDynamicProperty);
        }

        /// <summary>
        /// Resolves and validates the payload type against the expected type and returns the target type.
        /// </summary>
        /// <param name="expectedTypeKind">The expected type kind for the value.</param>
        /// <param name="defaultPrimitivePayloadType">The default payload type if none is specified in the payload;
        /// for ATOM this is Edm.String, for JSON it is null since there is no payload type name for primitive types in the payload.</param>
        /// <param name="expectedTypeReference">The expected type reference, or null if no expected type is available.</param>
        /// <param name="payloadTypeName">The payload type name, or null if no payload type was specified.</param>
        /// <param name="model">The model to use.</param>
        /// <param name="typeKindFromPayloadFunc">A func to compute the type kind from the payload shape if it could not be determined from the expected type or the payload type.</param>
        /// <param name="targetTypeKind">The target type kind to be used to read the payload.</param>
        /// <param name="serializationTypeNameAnnotation">Potentially non-null instance of an annotation to put on the value reported from the reader.</param>
        /// <returns>
        /// The target type reference to use for parsing the value.
        /// If there is no user specified model, this will return null.
        /// If there is a user specified model, this method never returns null.
        /// </returns>
        /// <remarks>
        /// This method cannot be used for primitive type resolution. Primitive type resolution is format dependent and format specific methods should be used instead.
        /// </remarks>
        public IEdmTypeReference ResolvePayloadTypeNameAndComputeTargetType(
            EdmTypeKind expectedTypeKind,
            IEdmType defaultPrimitivePayloadType,
            IEdmTypeReference expectedTypeReference,
            string payloadTypeName,
            IEdmModel model,
            Func<EdmTypeKind> typeKindFromPayloadFunc,
            out EdmTypeKind targetTypeKind,
            out SerializationTypeNameAnnotation serializationTypeNameAnnotation)
        {
            return ReaderValidationUtils.ResolvePayloadTypeNameAndComputeTargetType(
                expectedTypeKind, defaultPrimitivePayloadType, expectedTypeReference, payloadTypeName, model,
                settings.ClientCustomTypeResolver, settings.StrictMetadataValidation,
                settings.DisablePrimitiveTypeConversion,
                typeKindFromPayloadFunc, out targetTypeKind, out serializationTypeNameAnnotation);
        }

        /// <summary>
        /// Validates that a property with the specified name exists on a given structured type.
        /// The structured type can be null if no metadata is available.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate.</param>
        /// <param name="owningStructuredType">The owning type of the property with name <paramref name="propertyName"/>
        /// or null if no metadata is available.</param>
        /// <returns>The <see cref="IEdmProperty"/> instance representing the property with name
        /// <paramref name="propertyName"/> or null if no metadata is available.</returns>
        public IEdmProperty ValidateLinkPropertyDefined(string propertyName, IEdmStructuredType owningStructuredType)
        {
            return ReaderValidationUtils.ValidateLinkPropertyDefined(
                propertyName, owningStructuredType, settings.ThrowOnUndeclaredLinkProperty);
        }

        /// <summary>
        /// Validates that a navigation property with the specified name exists on a given entity type.
        /// The entity type can be null if no metadata is available.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate.</param>
        /// <param name="owningEntityType">The owning entity type or null if no metadata is available.</param>
        /// <returns>The <see cref="IEdmNavigationProperty"/> instance representing the navigation property with name
        /// <paramref name="propertyName"/> or null if no metadata is available.</returns>
        public IEdmNavigationProperty ValidateNavigationPropertyDefined(
            string propertyName, IEdmEntityType owningEntityType)
        {
            return ReaderValidationUtils.ValidateNavigationPropertyDefined(propertyName, owningEntityType,
                                                                           settings.ThrowOnUndeclaredLinkProperty);
        }

        /// <summary>
        /// Validates a stream reference property.
        /// </summary>
        /// <param name="streamProperty">The stream property to check.</param>
        /// <param name="structuredType">The owning type of the stream property or null if no metadata is available.</param>
        /// <param name="streamEdmProperty">The stream property defined by the model.</param>
        public void ValidateStreamReferenceProperty(ODataProperty streamProperty,
                                                    IEdmStructuredType structuredType,
                                                    IEdmProperty streamEdmProperty)
        {
            ReaderValidationUtils.ValidateStreamReferenceProperty(
                streamProperty, structuredType, streamEdmProperty, settings.ThrowOnUndeclaredLinkProperty);
        }
    }
}