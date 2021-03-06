﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Steroids.CodeStructure.Analyzers.NodeContainer;
using Steroids.CodeStructure.Analyzers.SectionHeader;
using Steroids.Contracts.Core;

namespace Steroids.CodeStructure.Analyzers
{
    public sealed class TypeGroupedSyntaxAnalyzer : CSharpSyntaxWalker, ICodeStructureSyntaxAnalyzer, IDisposable
    {
        private readonly List<TypeGroupedSyntaxAnalyzer> _subWalkers = new List<TypeGroupedSyntaxAnalyzer>();
        private readonly FieldsSectionHeader _fields = new FieldsSectionHeader();
        private readonly ConstructorsSectionHeader _constructors = new ConstructorsSectionHeader();
        private readonly MethodsSectionHeader _methods = new MethodsSectionHeader();
        private readonly PropertiesSectionHeader _properties = new PropertiesSectionHeader();
        private readonly EventsSectionHeader _events = new EventsSectionHeader();
        private readonly IDispatcherService _dispatcherService;
        private readonly ICollection<ICodeStructureNodeContainer> _nodeList;
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        private CancellationToken _token;
        private Guid _analyzeId;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeGroupedSyntaxAnalyzer"/> class.
        /// </summary>
        /// <param name="dispatcherService">The <see cref="IDispatcherService"/>.</param>
        public TypeGroupedSyntaxAnalyzer(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            RootNode = new FileSectionHeader
            {
                AbsoluteIndex = -1,
            };

            _nodeList = new List<ICodeStructureNodeContainer>();
            NodeList = _nodeList.OrderBy(x => x.AbsoluteIndex);
            TreeId = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeGroupedSyntaxAnalyzer"/> class.
        /// </summary>
        /// <param name="dispatcherService">The <see cref="IDispatcherService"/>.</param>
        /// <param name="analyzeId">An id to identify current analysis run.</param>
        /// <param name="nodeList">The node list of the root syntax walker.</param>
        /// <param name="rootNode">The <see cref="ICodeStructureNodeContainer">root node</see> for this walker.</param>
        private TypeGroupedSyntaxAnalyzer(
            IDispatcherService dispatcherService,
            Guid analyzeId,
            ICollection<ICodeStructureNodeContainer> nodeList,
            ICodeStructureSectionHeader rootNode)
        {
            _dispatcherService = dispatcherService ?? throw new ArgumentNullException(nameof(dispatcherService));

            _nodeList = nodeList;
            _analyzeId = analyzeId;
            RootNode = rootNode;

            if (!RootNode.Items.Contains(_fields))
            {
                RootNode.Items.Add(_fields);
            }

            if (!RootNode.Items.Contains(_constructors))
            {
                RootNode.Items.Add(_constructors);
            }

            if (!RootNode.Items.Contains(_events))
            {
                RootNode.Items.Add(_events);
            }

            if (!RootNode.Items.Contains(_properties))
            {
                RootNode.Items.Add(_properties);
            }

            if (!RootNode.Items.Contains(_methods))
            {
                RootNode.Items.Add(_methods);
            }
        }

        /// <summary>
        /// Gets an unique id to identify this instance.
        /// </summary>
        public Guid TreeId
        {
            get;
        }

        /// <summary>
        /// Gets the node list.
        /// </summary>
        public IEnumerable<ICodeStructureNodeContainer> NodeList
        {
            get;
        }

        /// <summary>
        /// Gets or sets the <see cref="ICodeStructureNodeContainer">root node</see> of this instance.
        /// </summary>
        private ICodeStructureSectionHeader RootNode
        {
            get;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _locker.Dispose();
        }

        /// <summary>
        /// Starts analysis of syntax tree.
        /// </summary>
        /// <param name="node">The root node of the syntax tree.</param>
        /// <param name="token">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Analyze(SyntaxNode node, CancellationToken token)
        {
            _token = token;
            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (!await _locker.WaitAsync(TimeSpan.FromSeconds(0.5)))
                {
                    return;
                }

                _analyzeId = Guid.NewGuid();

                await AnalyzeCore(node, token);
                RootNode.RefreshIndexes();
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <summary>
        /// Called when the visitor visits a ClassDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var sectionHeader = new ClassSectionHeader
            {
                Node = node,
                Parent = RootNode,
                AnalyzeId = _analyzeId
            };

            VisitSectionDeclaration(node, sectionHeader);
        }

        /// <summary>
        /// Called when the visitor visits a InterfaceDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var sectionHeader = new InterfaceSectionHeader
            {
                Node = node,
                Parent = RootNode,
                AnalyzeId = _analyzeId
            };

            VisitSectionDeclaration(node, sectionHeader);
        }

        /// <summary>
        /// Called when the visitor visits a StructDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var sectionHeader = new StructSectionHeader
            {
                Node = node,
                Parent = RootNode,
                AnalyzeId = _analyzeId
            };

            VisitSectionDeclaration(node, sectionHeader);
        }

        /// <summary>
        /// Called when the visitor visits a EnumDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var sectionHeader = new EnumSectionHeader
            {
                Node = node,
                Parent = RootNode,
                AnalyzeId = _analyzeId
            };

            VisitSectionDeclaration(node, sectionHeader);
        }

        /// <summary>
        /// Called when the visitor visits a MethodDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            VisitMemberDeclaration<MethodNodeContainer>(node, _methods);
        }

        /// <summary>
        /// Called when the visitor visits a ConstructorDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            VisitMemberDeclaration<ConstructorNodeContainer>(node, _constructors);
        }

        /// <summary>
        /// Called when the visitor visits a PropertyDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            VisitMemberDeclaration<PropertyNodeContainer>(node, _properties);
        }

        /// <summary>
        /// Called when the visitor visits a EventFieldDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            foreach (var eventDeclaration in node.Declaration.Variables)
            {
                VisitMemberDeclaration<EventNodeContainer>(eventDeclaration, _events);
            }
        }

        /// <summary>
        /// Called when the visitor visits a FieldDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var field in node.Declaration.Variables)
            {
                VisitMemberDeclaration<FieldNodeContainer>(field, _fields);
            }
        }

        /// <summary>
        /// Called when the visitor visits a EnumMemberDeclarationSyntax node.
        /// </summary>
        /// <param name="node">The visited node.</param>
        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            VisitMemberDeclaration<EnumMemberNodeContainer>(node, RootNode as EnumSectionHeader);
        }

        /// <summary>
        /// Analyzes a single section in separate walker.
        /// </summary>
        /// <param name="node">The node. </param>
        /// <param name="rootOfSubtree">The <see cref="ICodeStructureSectionHeader"/>.</param>
        private async void VisitSectionDeclaration(SyntaxNode node, ICodeStructureSectionHeader rootOfSubtree)
        {
            if (!RootNode.Items.Any(x => x.Id == rootOfSubtree.Id))
            {
                RootNode.Items.Add(rootOfSubtree);
                rootOfSubtree.Parent = RootNode;
            }
            else
            {
                RootNode.Items.First(x => x.Id == rootOfSubtree.Id).Node = node;
            }

            // We assume that in most cases the main structure doesn't change that much,
            // so the sub-walkers should always be visited in same order
            // otherwise we need some smarter matching to reuse the same walker for same tree parts.
            var subWalker = _subWalkers.FirstOrDefault(x => x.RootNode.Id == rootOfSubtree.Id);
            if (subWalker == null)
            {
                subWalker = new TypeGroupedSyntaxAnalyzer(_dispatcherService, _analyzeId, _nodeList, rootOfSubtree);
                _subWalkers.Add(subWalker);
            }

            rootOfSubtree.AnalyzeId = _analyzeId;
            subWalker._analyzeId = _analyzeId;
            await subWalker.AnalyzeCore(node, _token, true);
        }

        /// <summary>
        /// Internal generic implementation of VisitDeclaration overrides.
        /// </summary>
        /// <typeparam name="T">The type of the node container.</typeparam>
        /// <param name="node">The <see cref="SyntaxNode"/>.</param>
        /// <param name="section">The <see cref="SectionHeaderBase"/>.</param>
        private void VisitMemberDeclaration<T>(SyntaxNode node, SectionHeaderBase section)
            where T : class, ICodeStructureNodeContainer, new()
        {
            var newNode = new T() { Node = node };
            var reusableNode = section.Items.LastOrDefault(x => x.Id == newNode.Id);
            if (reusableNode != null)
            {
                reusableNode.Node = node;
            }
            else
            {
                reusableNode = newNode;
                section.Items.Add(reusableNode);
            }

            section.AnalyzeId = _analyzeId;
            reusableNode.AnalyzeId = _analyzeId;

            if (section.Parent == null)
            {
                section.Parent = RootNode;
            }

            if (!((SectionHeaderBase)section.Parent).Items.Contains(section))
            {
                ((SectionHeaderBase)section.Parent).Items.Add(section);
            }

            if (reusableNode.Parent != section)
            {
                reusableNode.Parent = section;
            }
        }

        /// <summary>
        /// Core logic of syntax analyzer.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="token">The token.</param>
        /// <param name="onlySubNodes">if set to <c>true</c> [only sub nodes].</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task AnalyzeCore(SyntaxNode node, CancellationToken token, bool onlySubNodes = false)
        {
            RootNode.AnalyzeId = _analyzeId;

            // visit all members
            if (!onlySubNodes)
            {
                Visit(node);
            }
            else
            {
                foreach (var subNode in node.ChildNodes())
                {
                    if (token.IsCancellationRequested)
                    {
                        return Task.FromResult(0);
                    }

                    Visit(subNode);
                }
            }

            // remove all sub-walkers which where not touched in analysis
            _subWalkers.RemoveAll(x => x._analyzeId != _analyzeId);
            if (onlySubNodes)
            {
                return Task.FromResult(0);
            }

            _dispatcherService.Dispatch(() =>
            {
                var tree = RootNode.AllTreeItems.ToList();
                foreach (var unused in NodeList.Where(x => !tree.Contains(x)).ToList())
                {
                    _nodeList.Remove(unused);
                }

                foreach (var added in tree.Where(x => !NodeList.Contains(x)).ToList())
                {
                    _nodeList.Add(added);
                }
            });

            return Task.FromResult(0);
        }
    }
}
