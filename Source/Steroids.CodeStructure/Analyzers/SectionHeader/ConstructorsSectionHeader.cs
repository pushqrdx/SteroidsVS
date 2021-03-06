﻿using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Steroids.CodeStructure.Analyzers.SectionHeader
{
    public class ConstructorsSectionHeader : SectionHeaderBase
    {
        public override bool IsMetaNode => true;

        public override int SectionTypeOrderIndex => 6;

        protected override ImageMoniker GetMoniker()
        {
            return (ImageMoniker)typeof(KnownMonikers).GetProperty($"Method").GetValue(null);
        }

        protected override string GetName()
        {
            return "Constructors";
        }
    }
}