﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Steroids.CodeStructure.Analyzers.NodeContainer;

namespace Steroids.CodeStructure.Analyzers.SectionHeader
{
    public abstract class SectionHeaderBase : NodeContainerBase<CSharpSyntaxNode>, ICodeStructureSectionHeader
    {
        private bool _isCollapsed;

        /// <summary>
        /// Gets the <see cref="ICodeStructureSectionHeader.AllTreeItems"/>.
        /// </summary>
        public List<ICodeStructureNodeContainer> AllTreeItems
        {
            get
            {
                Items.RemoveAll(x => x.AnalyzeId != AnalyzeId);

                return Items.Concat(Items.OfType<ICodeStructureSectionHeader>().SelectMany(x => x.AllTreeItems)).ToList();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ICodeStructureSectionHeader.IsCollapsed"/> is set.
        /// </summary>
        public bool IsCollapsed
        {
            get { return _isCollapsed; }
            set { Set(ref _isCollapsed, value); }
        }

        public abstract bool IsMetaNode { get; }

        /// <summary>
        /// Gets the <see cref="ICodeStructureSectionHeader.Items"/>.
        /// </summary>
        public List<ICodeStructureNodeContainer> Items { get; } = new List<ICodeStructureNodeContainer>();

        /// <summary>
        /// Gets the <see cref="ICodeStructureSectionHeader.LastAbsoluteIndex"/>.
        /// </summary>
        public override int LastAbsoluteIndex
        {
            get
            {
                return Items.Count == 0 ? AbsoluteIndex : Items.Max(x => x.LastAbsoluteIndex);
            }
        }

        /// <summary>
        /// Gets the <see cref="ICodeStructureSectionHeader.SectionTypeOrderIndex"/>.
        /// </summary>
        public abstract int SectionTypeOrderIndex { get; }

        /// <summary>
        /// Gets or sets the <see cref="ICodeStructureSectionHeader.SyntaxWalkerId"/>.
        /// </summary>
        public Guid SyntaxWalkerId { get; set; }

        /// <summary>
        /// <see cref="ICodeStructureSectionHeader.RefreshIndexes"/>.
        /// </summary>
        public void RefreshIndexes()
        {
            var currentIndex = AbsoluteIndex + 1;

            foreach (var section in Items.OfType<ICodeStructureSectionHeader>().OrderBy(x => x.SectionTypeOrderIndex).ThenBy(x => x.Name).ToList())
            {
                if (section.Items.Count == 0 && section.IsMetaNode)
                {
                    section.Parent = null;
                    Items.Remove(section);
                    continue;
                }

                section.AbsoluteIndex = currentIndex++;
                section.RefreshIndexes();
                currentIndex = section.LastAbsoluteIndex + 1;
            }

            foreach (var node in Items.OfType<ICodeStructureLeaf>().OrderBy(x => x.Name))
            {
                node.AbsoluteIndex = currentIndex++;
            }
        }

        /// <summary>
        /// Gets the <see cref="ICodeStructureNodeContainer.AccessModifier"/>.
        /// </summary>
        /// <returns>The access modifier text.</returns>
        protected override string GetAccessModifier()
        {
            return string.Empty;
        }

        protected override string GetParameters()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets the <see cref="ICodeStructureNodeContainer.Type"/>.
        /// </summary>
        /// <returns>The return type.</returns>
        protected override string GetReturnType()
        {
            return string.Empty;
        }
    }
}