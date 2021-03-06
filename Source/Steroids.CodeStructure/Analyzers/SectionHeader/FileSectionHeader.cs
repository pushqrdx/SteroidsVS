﻿using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Steroids.CodeStructure.Analyzers.SectionHeader
{
    public class FileSectionHeader : SectionHeaderBase
    {
        public override bool IsMetaNode => true;

        public override int SectionTypeOrderIndex => 0;

        protected override ImageMoniker GetMoniker()
        {
            return (ImageMoniker)typeof(KnownMonikers).GetProperty($"CSFileNode").GetValue(null);
        }

        protected override string GetName()
        {
            return "File";
        }
    }
}