﻿using Coevery.ContentManagement;
using Coevery.ContentManagement.Drivers;
using Coevery.ContentManagement.Handlers;
using Coevery.Core.Title.Models;
using Coevery.Localization;

namespace Coevery.Core.Title.Drivers {
    public class TitlePartDriver : ContentPartDriver<TitlePart> {

        private const string TemplateName = "Parts.Title.TitlePart";

        public Localizer T { get; set; }

        protected override string Prefix {
            get { return "Title"; }
        }

        protected override DriverResult Display(TitlePart part, string displayType, dynamic shapeHelper) {
            return Combined(
                ContentShape("Parts_Title",
                    () => shapeHelper.Parts_Title(Title: part.Title)),
                ContentShape("Parts_Title_Summary",
                    () => shapeHelper.Parts_Title_Summary(Title: part.Title)),
                ContentShape("Parts_Title_SummaryAdmin",
                    () => shapeHelper.Parts_Title_SummaryAdmin(Title: part.Title))
                );
        }

        protected override DriverResult Editor(TitlePart part, dynamic shapeHelper) {

            return ContentShape("Parts_Title_Edit",
                () => shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: part, Prefix: Prefix));
        }

        protected override DriverResult Editor(TitlePart part, IUpdateModel updater, dynamic shapeHelper) {
            updater.TryUpdateModel(part, Prefix, null, null);

            return Editor(part, shapeHelper);
        }

        protected override void Importing(TitlePart part, ImportContentContext context) {
            var title = context.Attribute(part.PartDefinition.Name, "Title");
            if (title != null) {
                part.Title = title;
            }
        }

        protected override void Exporting(TitlePart part, ExportContentContext context) {
            context.Element(part.PartDefinition.Name).SetAttributeValue("Title", part.Title);
        }
    }
}