﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Text.Outlining;
using Steroids.Contracts;
using Steroids.Contracts.UI;
using Steroids.Core;
using Steroids.Core.Diagnostics.Contracts;
using Steroids.Core.Extensions;

namespace Steroids.CodeQuality.UI
{
    public sealed class DiagnosticInfosViewModel : BindableBase, IDisposable
    {
        private readonly IDiagnosticProvider _diagnosticProvider;

        private List<DiagnosticInfo> _lastDiagnostics = new List<DiagnosticInfo>();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticInfosViewModel"/> class.
        /// </summary>
        /// <param name="textView">The <see cref="IQualityTextView"/>.</param>
        /// <param name="diagnosticProvider">The <see cref="IDiagnosticProvider"/>.</param>
        /// <param name="outliningManagerService">THe <see cref="IOutliningManagerService"/> for the <paramref name="textView"/>.</param>
        /// <param name="adornmentSpaceReservation">The <see cref="IAdornmentSpaceReservation"/>.</param>
        public DiagnosticInfosViewModel(
            IQualityTextView textView,
            IDiagnosticProvider diagnosticProvider,
            IOutliningManagerService outliningManagerService,
            IAdornmentSpaceReservation adornmentSpaceReservation)
        {
            _diagnosticProvider = diagnosticProvider ?? throw new ArgumentNullException(nameof(diagnosticProvider));
            TextView = textView ?? throw new ArgumentNullException(nameof(textView));
            AdornmentSpaceReservation = adornmentSpaceReservation ?? throw new ArgumentNullException(nameof(adornmentSpaceReservation));
            OutliningManager = outliningManagerService.GetOutliningManager(TextView.TextView);

            WeakEventManager<IDiagnosticProvider, DiagnosticsChangedEventArgs>.AddHandler(_diagnosticProvider, nameof(IDiagnosticProvider.DiagnosticsChanged), OnDiagnosticsChanged);

            OutliningManager.RegionsExpanded += OnRegionsExpanded;
            OutliningManager.RegionsCollapsed += OnRegionsCollapsed;

            OnDiagnosticsChanged(this, new DiagnosticsChangedEventArgs(_diagnosticProvider.CurrentDiagnostics));
        }

        public IOutliningManager OutliningManager { get; private set; }

        /// <summary>
        /// The list of current <see cref="DiagnosticInfoLine"/>.
        /// </summary>
        public ObservableCollection<DiagnosticInfoLine> DiagnosticInfoLines { get; }
            = new ObservableCollection<DiagnosticInfoLine>();

        /// <summary>
        /// The adornment space reservation to avoid overlapping adornments.
        /// </summary>
        public IAdornmentSpaceReservation AdornmentSpaceReservation { get; }

        /// <summary>
        /// The text view for which the diagnostics are evaluated.
        /// </summary>
        public IQualityTextView TextView { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            OutliningManager.RegionsExpanded -= OnRegionsExpanded;
            OutliningManager.RegionsCollapsed -= OnRegionsCollapsed;

            _disposed = true;
        }

        /// <summary>
        /// Recreates the code hints, when the items in the error list have changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="DiagnosticsChangedEventArgs"/>.</param>
        private void OnDiagnosticsChanged(object sender, DiagnosticsChangedEventArgs args)
        {
            // TODO: only recreate non existing hints and remove resolved hints.
            var fileDiagnostics = TextView
                .ExtractRelatedDiagnostics(args.Diagnostics)
                .Where(x => x.IsActive)
                .ToList();

            _lastDiagnostics = fileDiagnostics;
            UpdateDiagnostics(fileDiagnostics);
        }

        private void UpdateDiagnostics(IReadOnlyCollection<DiagnosticInfo> fileDiagnostics)
        {
            var lineMap = fileDiagnostics
                .Select(x => x.LineNumber)
                .Distinct()
                .ToDictionary(x => x, x => DiagnosticInfoPlacementCalculator.GetRealLineNumber(TextView.TextView, x, OutliningManager));

            foreach (var diagnostic in fileDiagnostics)
            {
                if (!lineMap.ContainsKey(diagnostic.LineNumber))
                {
                    diagnostic.ComputedLineNumber = diagnostic.LineNumber;
                    continue;
                }

                diagnostic.ComputedLineNumber = lineMap[diagnostic.LineNumber];
            }

            // remove unused lines
            foreach (var line in DiagnosticInfoLines.Where(x => !lineMap.Values.Contains(x.LineNumber)).ToList())
            {
                DiagnosticInfoLines.Remove(line);
            }

            // update existing lines or add new ones
            foreach (var lineDiagnostics in fileDiagnostics.GroupBy(x => x.ComputedLineNumber))
            {
                var line = DiagnosticInfoLines.FirstOrDefault(x => x.LineNumber == lineDiagnostics.Key)
                    ?? new DiagnosticInfoLine(lineDiagnostics.Key, lineDiagnostics.OrderBy(x => x).ToList());

                if (!DiagnosticInfoLines.Contains(line))
                {
                    DiagnosticInfoLines.Add(line);
                }
                else
                {
                    line.DiagnosticInfos = lineDiagnostics.OrderBy(x => x).ToList();
                }
            }
        }

        private void OnRegionsCollapsed(object sender, RegionsCollapsedEventArgs e)
        {
            UpdateDiagnostics(_lastDiagnostics);
        }

        private void OnRegionsExpanded(object sender, RegionsExpandedEventArgs e)
        {
            UpdateDiagnostics(_lastDiagnostics);
        }
    }
}
