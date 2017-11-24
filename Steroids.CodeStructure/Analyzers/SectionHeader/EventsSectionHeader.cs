﻿namespace Steroids.CodeStructure.Analyzers.SectionHeader
{
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Imaging.Interop;

    public class EventsSectionHeader : SectionHeaderBase
    {
        public override bool IsMetaNode => true;

        public override int SectionTypeOrderIndex => 7;

        protected override ImageMoniker GetMoniker()
        {
            return (ImageMoniker)typeof(KnownMonikers).GetProperty($"Event").GetValue(null);
        }

        protected override string GetName()
        {
            return "Events";
        }
    }
}
