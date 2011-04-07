﻿using System.Xml;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Services;

namespace Orchard.Core.Common.Drivers {
    public class CommonPartDriver : ContentPartDriver<CommonPart> {
        private readonly IContentManager _contentManager;
        private readonly IAuthenticationService _authenticationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMembershipService _membershipService;
        private readonly IClock _clock;

        public CommonPartDriver(
            IOrchardServices services,
            IContentManager contentManager,
            IAuthenticationService authenticationService,
            IAuthorizationService authorizationService,
            IMembershipService membershipService,
            IClock clock) {
            _contentManager = contentManager;
            _authenticationService = authenticationService;
            _authorizationService = authorizationService;
            _membershipService = membershipService;
            _clock = clock;
            T = NullLocalizer.Instance;
            Services = services;
        }

        public Localizer T { get; set; }
        public IOrchardServices Services { get; set; }

        protected override string Prefix {
            get { return "CommonPart"; }
        }

        protected override DriverResult Display(CommonPart part, string displayType, dynamic shapeHelper) {
            return Combined(
                ContentShape("Parts_Common_Metadata",
                             () => shapeHelper.Parts_Common_Metadata(ContentPart: part)),
                ContentShape("Parts_Common_Metadata_Summary",
                             () => shapeHelper.Parts_Common_Metadata_Summary(ContentPart: part)),
                ContentShape("Parts_Common_Metadata_SummaryAdmin",
                             () => shapeHelper.Parts_Common_Metadata_SummaryAdmin(ContentPart: part))
                );
        }

        protected override DriverResult Editor(CommonPart part, dynamic shapeHelper) {
            return Combined(
                OwnerEditor(part, null, shapeHelper),
                ContainerEditor(part, null, shapeHelper));
        }

        protected override DriverResult Editor(CommonPart part, IUpdateModel updater, dynamic shapeHelper) {
            // this event is hooked so the modified timestamp is changed when an edit-post occurs
            part.ModifiedUtc = _clock.UtcNow;
            part.VersionModifiedUtc = _clock.UtcNow;

            return Combined(
                OwnerEditor(part, updater, shapeHelper),
                ContainerEditor(part, updater, shapeHelper));
        }

        DriverResult OwnerEditor(CommonPart part, IUpdateModel updater, dynamic shapeHelper) {
            var currentUser = _authenticationService.GetAuthenticatedUser();
            if (!_authorizationService.TryCheckAccess(StandardPermissions.SiteOwner, currentUser, part)) {
                return null;
            }

            var model = new OwnerEditorViewModel();
            if (part.Owner != null)
                model.Owner = part.Owner.UserName;

            if (updater != null) {
                var priorOwner = model.Owner;
                updater.TryUpdateModel(model, Prefix, null, null);

                if (model.Owner != null && model.Owner != priorOwner) {
                    var newOwner = _membershipService.GetUser(model.Owner);
                    if (newOwner == null) {
                        updater.AddModelError("CommonPart.Owner", T("Invalid user name"));
                    }
                    else {
                        part.Owner = newOwner;
                    }
                }
            }

            return ContentShape("Parts_Common_Owner_Edit",
                                () => shapeHelper.EditorTemplate(TemplateName: "Parts.Common.Owner", Model: model, Prefix: Prefix));
        }

        DriverResult ContainerEditor(CommonPart part, IUpdateModel updater, dynamic shapeHelper) {
            var currentUser = _authenticationService.GetAuthenticatedUser();
            if (!_authorizationService.TryCheckAccess(StandardPermissions.SiteOwner, currentUser, part)) {
                return null;
            }

            var model = new ContainerEditorViewModel();
            if (part.Container != null)
                model.ContainerId = part.Container.ContentItem.Id;

            if (updater != null) {
                var priorContainerId = model.ContainerId;
                updater.TryUpdateModel(model, Prefix, null, null);

                if (model.ContainerId != null && model.ContainerId != priorContainerId) {
                    var newContainer = _contentManager.Get((int)model.ContainerId, VersionOptions.Latest);
                    if (newContainer == null) {
                        updater.AddModelError("CommonPart.ContainerId", T("Invalid container"));
                    }
                    else {
                        part.Container = newContainer;
                    }
                }
            }

            return ContentShape("Parts_Common_Container_Edit",
                                () => shapeHelper.EditorTemplate(TemplateName: "Parts.Common.Container", Model: model, Prefix: Prefix));
        }

        protected override void Importing(CommonPart part, ImportContentContext context) {
            var owner = context.Attribute(part.PartDefinition.Name, "Owner");
            if (owner != null) {
                var contentIdentity = new ContentIdentity(owner);
                part.Owner = _membershipService.GetUser(contentIdentity.Get("User.UserName"));
            }

            var container = context.Attribute(part.PartDefinition.Name, "Container");
            if (container != null) {
                part.Container = context.GetItemFromSession(container);
            }

            var createdUtc = context.Attribute(part.PartDefinition.Name, "CreatedUtc");
            if (createdUtc != null) {
                part.CreatedUtc = XmlConvert.ToDateTime(createdUtc, XmlDateTimeSerializationMode.Utc);
            }

            var publishedUtc = context.Attribute(part.PartDefinition.Name, "PublishedUtc");
            if (publishedUtc != null) {
                part.PublishedUtc = XmlConvert.ToDateTime(publishedUtc, XmlDateTimeSerializationMode.Utc);
            }

            var modifiedUtc = context.Attribute(part.PartDefinition.Name, "ModifiedUtc");
            if (modifiedUtc != null) {
                part.ModifiedUtc = XmlConvert.ToDateTime(modifiedUtc, XmlDateTimeSerializationMode.Utc);
            }
        }

        protected override void Exporting(CommonPart part, ExportContentContext context) {
            if (part.Owner != null) {
                var ownerIdentity = _contentManager.GetItemMetadata(part.Owner).Identity;
                context.Element(part.PartDefinition.Name).SetAttributeValue("Owner", ownerIdentity.ToString());
            }

            if (part.Container != null) {
                var containerIdentity = _contentManager.GetItemMetadata(part.Container).Identity;
                context.Element(part.PartDefinition.Name).SetAttributeValue("Container", containerIdentity.ToString()); 
            }

            if (part.CreatedUtc != null) {
                context.Element(part.PartDefinition.Name)
                    .SetAttributeValue("CreatedUtc", XmlConvert.ToString(part.CreatedUtc.Value, XmlDateTimeSerializationMode.Utc));
            }
            if (part.PublishedUtc != null) {
                context.Element(part.PartDefinition.Name)
                    .SetAttributeValue("PublishedUtc", XmlConvert.ToString(part.PublishedUtc.Value, XmlDateTimeSerializationMode.Utc));
            }
            if (part.ModifiedUtc != null) {
                context.Element(part.PartDefinition.Name)
                    .SetAttributeValue("ModifiedUtc", XmlConvert.ToString(part.ModifiedUtc.Value, XmlDateTimeSerializationMode.Utc));
            }
        }
    }
}